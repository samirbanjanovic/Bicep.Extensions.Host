using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Index;
using Bicep.Extension.Host.Handlers;
using Bicep.Extension.Host.TypeBuilder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bicep.Extension.Host
{
    public static class BicepExtensionHost
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
                                                                    , Func<TypeFactory, ObjectType, TypeSettings> typeSettings
                                                                    , Action<TypeFactory, Dictionary<string, ObjectTypeProperty>>? typeConfiguration = null)
        {
            var typeFactory = new TypeFactory([]);
            var configuration = new Dictionary<string, ObjectTypeProperty>();

            if (typeSettings is null)
            {
                throw new ArgumentNullException(nameof(typeSettings));
            }

            if (typeConfiguration is not null)
            {
                typeConfiguration(typeFactory, configuration);
            }

            ObjectType configurationType = typeFactory.Create(() => new ObjectType("configuration", configuration, null));

            TypeSettings settings = typeSettings(typeFactory, configurationType);

            services.AddSingleton(settings);
            services.AddSingleton(typeFactory);
            services.AddSingleton<IResourceHandlerFactory, ResourceHandlerFactory>();
            services.AddBicepTypeGenerator<StandardTypeSpecGenerator>();
            services.AddGrpc();
            services.AddGrpcReflection();
            return services;
        }

        public static async Task RunBicepExtensionAsync(this WebApplication? app)
        {
            if(app is null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (app.Configuration.GetValue<bool>("describe"))
            {
                var typeSpecGenerator = app.Services.GetRequiredService<ITypeSpecGenerator>();
                var spec = typeSpecGenerator.GenerateBicepResourceTypes();

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters =
                    {
                        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                    }
                };

                Console.WriteLine(spec.TypesJson);
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
                                                                              context.Configuration.GetValue<int>("http", 5000));

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

        public static IServiceCollection AddBicepTypeGenerator<T>(this IServiceCollection services)
            where T : class, ITypeSpecGenerator
        {
            // remove previously registered BicepTypeGenerator
            var serviceDescriptors = services.Where(services => services.ServiceType == typeof(ITypeSpecGenerator));
            if (serviceDescriptors.Any())
            {
                foreach (var sd in serviceDescriptors)
                {
                    services.Remove(sd);
                }
            }

            services.AddSingleton<ITypeSpecGenerator, T>();
            return services;
        }

        public static WebApplication UseBicepDispatcher(this WebApplication app)
        {
            app.MapGrpcService<ResourceRequestDispatcher>();

            var env = app.Environment;
            if (env.IsDevelopment())
            {
                app.MapGrpcReflectionService();
            }

            return app;
        }

        public static IServiceCollection AddBicepResourceHandler<T>(this IServiceCollection services)
            where T : class, Handlers.IResourceHandler
        {
            var resourceHandler = typeof(T);

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
            else if (resourceHandler.IsGenericTypedResourceHandler())
            {
                var genericHandler = services.Select(service => service.ImplementationType)
                                .OfType<Type>()
                                .FirstOrDefault(x => x.IsGenericTypedResourceHandler());

                if (genericHandler is not null)
                {
                    throw new InvalidOperationException($"A generic resource handler [`{genericHandler.FullName}`] has already been added.");
                }
            }
            else
            {
                throw new InvalidOperationException($"Failed to register resource handler");
            }

            services.AddSingleton<Handlers.IResourceHandler, T>();

            return services;
        }

    }
}
