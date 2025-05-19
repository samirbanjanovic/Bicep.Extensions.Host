using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Index;
using Bicep.Host.Types;
using Bicep.Local.Extension.Protocol;
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
            => bool.TryParse(Environment.GetEnvironmentVariable("BICEP_TRACING_ENABLED"), out var value) && value;

        public static IServiceCollection AddBicepTypeGenerator<T>(this IServiceCollection services)
            where T : class, ITypeSpecGenerator
        {
            // remove previously registered BicepTypeGenerator
            var serviceDescriptor = services.FirstOrDefault(services => services.ServiceType == typeof(ITypeSpecGenerator));
            if (serviceDescriptor is not null)
            {
                services.Remove(serviceDescriptor);
            }

            services.AddSingleton<ITypeSpecGenerator, T>();
            return services;
        }

        public static IServiceCollection AddBicepServices(this IServiceCollection services)
        {

            services.AddSingleton<TypeFactory>(sp => new TypeFactory([]));
            services.AddSingleton<BicepResourceHandlerMap>();
            services.AddSingleton(sp => new ExtensionSpec("test-ext", "0.0.1"));
            services.AddSingleton(sp => new TypeConfiguration(new Dictionary<string, ObjectTypeProperty>()));
            services.AddBicepTypeGenerator<StandardTypeSpecGenerator>();
            services.AddGrpc();
            services.AddGrpcReflection();
            return services;
        }

        public static WebApplication UseBicepDispatcher(this WebApplication app)
        {
            app.MapGrpcService<BicepResourceRequestDispatcher>();

            var env = app.Environment;
            if (env.IsDevelopment())
            {
                app.MapGrpcReflectionService();
            }

            return app;
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

        public static IServiceCollection AddGenericBicepResourceHandler<T>(this IServiceCollection services)
            where T : GenericTypedResourceHandler, ITypedResourceHandler
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

            services.AddSingleton<ITypedResourceHandler, T>();

            return services;
        }

        public static IServiceCollection AddTypedBicepResourceHandler<T>(this IServiceCollection services)
            where T : GenericTypedResourceHandler, ITypedResourceHandler
        {
            if (!typeof(T).TryGetTypedResourceHandlerInterface(out var baseInterface))
            {
                throw new InvalidOperationException($"To registere a generic resource handler use {nameof(AddGenericBicepResourceHandler)}");
            }

            var resourceType = baseInterface.GetGenericArguments()[0];

            // get the generic type definition of the class being added, T
            var resourceTypeHasHandler = services
                .Where(st => st.ServiceType.IsAssignableFrom(typeof(ITypedResourceHandler<>)))
                .Select(t =>
                {
                    if (t.ImplementationType.TryGetTypedResourceHandlerInterface(out var typedInterface))
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

            services.AddSingleton<ITypedResourceHandler, T>();

            return services;
        }

    }
}
