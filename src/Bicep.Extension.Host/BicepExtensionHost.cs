using Bicep.Local.Extension.Protocol;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace Bicep.Extension.Host
{
    public static class BicepExtensionHost
    {
        private static bool IsTracingEnabled
            => bool.TryParse(Environment.GetEnvironmentVariable("BICEP_TRACING_ENABLED"), out var value) && value;

        public static IConfigurationBuilder AddBicepCommandLineArguments(this ConfigurationManager configuration, string[] args)
        {
            if (IsTracingEnabled)
            {
                Trace.Listeners.Add(new TextWriterTraceListener(Console.Error));
            }

            var mappings = new Dictionary<string, string>
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

            configuration.AddCommandLine(args, mappings);

            return configuration;
        }

        public static IServiceCollection AddBicepServices(this IServiceCollection services)
        {
            services.AddSingleton<BicepResourceHandlerMap>();
            services.AddGrpc();
            services.AddGrpcReflection();
            return services;
        }

        public static WebApplication UseBicepDispatcher(this WebApplication app)
        {
            app.MapGrpcService<BicepResourceRequestDispatcher>();

            var env = app.Environment;
            if(env.IsDevelopment())
            {
                app.MapGrpcReflectionService();
            }

            return app;
        }

        public static WebApplicationBuilder AddBicepExtensionHost(this WebApplicationBuilder builder, string[] args)
        {
            builder.Configuration.AddBicepCommandLineArguments(args);

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
                    case { Http: { } }:                        
                    default:
                        options.ListenLocalhost(connectionOptions.Http, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                        break;
                }
            });

            return builder;
        }

        public static IServiceCollection AddBicepGenericResourceHandler<T>(this IServiceCollection services)
            where T : class, IGenericResourceHandler
        {
            var genericHandlers = services.Count(service => service.ServiceType is IGenericResourceHandler);
            if (genericHandlers > 0)
            {
                throw new InvalidOperationException("Generic resource handler has already been added.");
            }

            services.AddSingleton<IGenericResourceHandler, T>();

            return services;
        }

        public static IServiceCollection AddBicepResourceHandler<T>(this IServiceCollection services)
            where T : class, IResourceHandler
            => services.AddSingleton<IResourceHandler, T>();
    }
}
