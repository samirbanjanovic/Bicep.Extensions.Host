using Bicep.Local.Extension;
using Bicep.Local.Extension.Protocol;
using Bicep.Local.Extension.Rpc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Bicep.Extension.Tricep
{
    internal class TricepKestrelProviderExtension
        : ProviderExtension
    {
        private readonly Action<IServiceCollection>? services;

        public TricepKestrelProviderExtension(Action<IServiceCollection>? services)
            : base()
        {
            this.services = services;
        }

        protected override async Task RunServer(ConnectionOptions connectionOptions, ResourceDispatcher dispatcher, CancellationToken cancellationToken)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.ConfigureKestrel(options =>
            {
                switch (connectionOptions)
                {
                    case { Socket: { }, Pipe: null }:
                        options.ListenUnixSocket(connectionOptions.Socket, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                        break;
                    case { Socket: null, Pipe: { } }:
                        options.ListenNamedPipe(connectionOptions.Pipe, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                        break;
                    default:
                        throw new InvalidOperationException("Either socketPath or pipeName must be specified.");
                }
            });

            if (services is not null)
            {
                // Add any additional services to the DI container
                services(builder.Services);
            }

            builder.Services.AddGrpc();
            builder.Services.AddSingleton(dispatcher);

            var app = builder.Build();
            app.MapGrpcService<BicepExtensionImpl>();

            await app.RunAsync();
        }
    }
}

