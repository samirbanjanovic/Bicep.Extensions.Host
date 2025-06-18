using Azure.Bicep.Types;
using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Index;
using Bicep.Extension.Host.Handlers;
using Bicep.Extension.Host.TypeBuilder;
using Bicep.Local.Extension.Rpc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bicep.Extension.Host.Extensions;

public static class IServiceCollectionExtensions
{
    private static Dictionary<string, string> ArgumentMappings => new Dictionary<string, string>
            {
                { "-d", "describe" },
                { "--describe", "describe" },

                { "-s", "socket" },
                { "--socket", "socket" },

                { "-t", "http" },
                { "--http", "http" },

                { "-p", "pipe" },
                { "--pipe", "pipe" }
            };


    private static bool IsTracingEnabled
        => bool.TryParse(Environment.GetEnvironmentVariable("BICEP_TRACING_ENABLED"), out var isEnabled) && isEnabled;

    public static IServiceCollection AddBicepExtensionServices(this IServiceCollection services
                                                                , string name, string version, bool isSingleton
                                                                , Action<Dictionary<string, ObjectTypeProperty>>? typeConfiguration = null)
    {
        var typeFactory = new TypeFactory([]);
        var configuration = new Dictionary<string, ObjectTypeProperty>();

        if (typeConfiguration is not null)
        {
            typeConfiguration(configuration);
        }

        ObjectType configurationType = typeFactory.Create(() => new ObjectType("configuration", configuration, null));

        var typeSettings = new TypeSettings
            (
                name,
                version,
                isSingleton,
                new CrossFileTypeReference("types.json", typeFactory.GetIndex(configurationType))
            );

        services.AddSingleton(typeSettings)
            .AddSingleton(typeFactory)
            .AddSingleton<IResourceHandlerFactory, ResourceHandlerFactory>()
            .AddSingleton<ITypeDefinitionBuilder, TypeDefinitionBuilder>()
            .AddSingleton(sp => new Dictionary<Type, Func<TypeBase>>
                            {
                                { typeof(string), () => new StringType() },
                                { typeof(bool), () => new BooleanType() },
                                { typeof(int), () => new IntegerType() }
                            }.ToImmutableDictionary());

        services.AddGrpc();
        services.AddGrpcReflection();
        return services;
    }

    public static async Task RunBicepExtensionAsync(this WebApplication? app)
    {
        if (app is null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        if (app.Configuration.GetValue<bool>("describe"))
        {
            var typeSpecGenerator = app.Services.GetRequiredService<ITypeDefinitionBuilder>();
            var spec = typeSpecGenerator.GenerateBicepResourceTypes();

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters =
                    {
                        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                    }
            };

            Console.WriteLine($"types =\r\n{spec.TypesJson}\r\nindex =\r\n{spec.IndexJson}");
        }
        else
        {
            await app.RunAsync();
        }
    }

    public static WebApplicationBuilder AddBicepExtensionHost(this WebApplicationBuilder builder, string[] args)
    {
        if (IsTracingEnabled)
        {
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Error));
        }

        builder.Configuration.AddCommandLine(args, ArgumentMappings);

        builder.WebHost.ConfigureKestrel((context, options) =>
        {
            (string? Socket, string? Pipe, int Http) connectionOptions = (context.Configuration.GetValue<string>("socket"),
                                                                          context.Configuration.GetValue<string>("pipe"),
                                                                          context.Configuration.GetValue("http", 5000));

            switch (connectionOptions)
            {
                case { Socket: { }, Pipe: null }:
                    options.ListenUnixSocket(connectionOptions.Socket, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                    break;
                case { Socket: null, Pipe: { } }:
                    options.ListenNamedPipe(connectionOptions.Pipe, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                    break;
                default:
                    options.ListenLocalhost(connectionOptions.Http, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                    break;
            }
        });

        return builder;
    }

    public static WebApplication MapBicepDispatcher<TDispatcher>(this WebApplication app)
        where TDispatcher : BicepExtension.BicepExtensionBase
    {        
        app.MapGrpcService<TDispatcher>();

        var env = app.Environment;
        if (env.IsDevelopment())
        {
            app.MapGrpcReflectionService();
        }

        return app;
    }

    public static IServiceCollection AddBicepResourceHandler<T>(this IServiceCollection services)
        where T : class, IResourceHandler
    {
        var resourceHandler = typeof(T);

        // check that only one handler is registered for the given resource type
        if (resourceHandler.TryGetTypedResourceHandlerInterface(out var baseInterface))
        {
            var resourceType = baseInterface.GetGenericArguments()[0];

            var existingHandler = services
                .Where(st => st.ServiceType.IsAssignableFrom(typeof(IResourceHandler<>)))
                .Select(t =>
                {
                    var implementationType = t.IsKeyedService ? t.KeyedImplementationType : t.ImplementationType;

                    if (implementationType?.TryGetTypedResourceHandlerInterface(out Type? typedInterface) == true)
                    {
                        var genericType = typedInterface.GetGenericArguments()[0];
                        return genericType;
                    }

                    return null;
                })
                .OfType<Type>()
                .FirstOrDefault(et => et == resourceType);

            if (existingHandler is not null)
            {
                throw new InvalidOperationException($"A handler [`{existingHandler.FullName}`] is already registered for type [`{resourceType.FullName}`]");
            }

        }

        services.AddSingleton<IResourceHandler, T>();

        return services;
    }

}

