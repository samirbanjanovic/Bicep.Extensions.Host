using Bicep.Extension.Host;
using Bicep.Extension.Sample.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bicep.Extension.Sample
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebApplication
                                .CreateBuilder()
                                .AddBicepExtensionHost(args);

            builder.Services
                   .AddBicepServices()
                   .AddTypedBicepResourceHandler<OmniHandler>()
                   .AddSingleton<IBackendService, LocalOutputService>();

            var app = builder.Build();
            app.UseBicepDispatcher();

            await app.RunAsync();
        }
    }
}
