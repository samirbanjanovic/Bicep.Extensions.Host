using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Index;
using Bicep.Extension.Host.Handlers;
using Bicep.Extension.Host.TypeBuilder;
using Bicep.Local.Extension.Protocol;
using Google.Protobuf.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Threading;

namespace Bicep.Extension.Host
{
    public static class BicepExtensionHost
    {
        private static Dictionary<string, string> ArgumentMappings => new Dictionary<string, string>
            {
                { "--socket", "socket" },
                { "--http", "http" },
                { "--pipe", "pipe" },
                { "--wait-for-debugger", "waitForDebugger" },
                { "-s", "socket" },
                { "-t", "http" },
                { "-p", "pipe" },
                { "-w", "waitForDebugger" }
            };


        private static bool IsTracingEnabled
            => bool.TryParse(Environment.GetEnvironmentVariable("BICEP_TRACING_ENABLED"), out var isEnabled) && isEnabled;

        public static IServiceCollection AddBicepExtensionServices(this IServiceCollection services
                                                                    , Func<TypeFactory, ObjectType, TypeSettings> typeSettings
                                                                    , Action<TypeFactory, Dictionary<string, ObjectTypeProperty>>? typeConfiguration = null)
        {
            var typeFactory = new TypeFactory([]);
            var configuration = new Dictionary<string, ObjectTypeProperty>();

            if(typeSettings is null)
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
            services.AddSingleton<IResourceHandlerMap, ResourceHandlerMap>();
            services.AddBicepTypeGenerator<StandardTypeSpecGenerator>();
            services.AddGrpc();
            services.AddGrpcReflection();
            return services;
        }

        public static WebApplicationBuilder AddBicepExtensionHost(this WebApplicationBuilder builder, string[] args)
        {
            if (IsTracingEnabled)
            {
                Trace.Listeners.Add(new TextWriterTraceListener(Console.Error));
            }

            builder.Configuration.AddCommandLine(args, ArgumentMappings);

            if (builder.Configuration.GetValue<bool>("waitForDebugger"))
            {
                var cancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(5)).Token;

                while (!Debugger.IsAttached && !cancellationToken.IsCancellationRequested)
                {
                    Thread.Sleep(100);
                }

                Debugger.Break();
            }


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
            if (serviceDescriptors is not null)
            {
                foreach(var sd in serviceDescriptors)
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

        public static IServiceCollection AddGenericBicepResourceHandler<T>(this IServiceCollection services)
            where T : class, Handlers.IGenericResourceHandler
        {
            var interfaces = typeof(T).GetInterfaces();
            if (typeof(T).TryGetTypedResourceHandlerInterface(out var _))
            {
                throw new InvalidOperationException($"To register a strongly typed resource hander use {nameof(AddTypedBicepResourceHandler)}");
            }

            var hasGenericHandler = services.Select(service => service.ImplementationType)
                                            .OfType<Type>()
                                            .Any(x => x.IsGenericTypedResourceHandler());
            if (hasGenericHandler)
            {
                throw new InvalidOperationException("A generic resource handler has already been added.");
            }

            services.AddSingleton<Handlers.IGenericResourceHandler, T>();

            return services;
        }

        public static IServiceCollection AddTypedBicepResourceHandler<T>(this IServiceCollection services)
            where T : class, Handlers.IGenericResourceHandler
        {
            if (!typeof(T).TryGetTypedResourceHandlerInterface(out var baseInterface))
            {
                throw new InvalidOperationException($"To register a generic resource handler use {nameof(AddGenericBicepResourceHandler)}");
            }

            var resourceType = baseInterface.GetGenericArguments()[0];

            // get the generic type definition of the class being added, T
            var resourceTypeHasHandler = services
                .Where(st => st.ServiceType.IsAssignableFrom(typeof(ITypedResourceHandler<>)))
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
                .Any(x => x.GetType() == resourceType.GetType());


            if(resourceTypeHasHandler)
            {
                throw new InvalidOperationException($"A resource handler for {resourceType.Name} has already been registered.");
            }

            services.AddSingleton<Handlers.IGenericResourceHandler, T>();

            return services;
        }

    }
}
