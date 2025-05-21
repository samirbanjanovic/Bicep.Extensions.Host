using Azure.Bicep.Types;
using Azure.Bicep.Types.Index;
using Bicep.Extension.Host;
using Bicep.Extension.Sample.Handlers;
using Bicep.Extension.Sample.Models;
using Microsoft.AspNetCore.Builder;
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
                   .AddBicepExtensionServices((factory, configurationType) => new TypeSettings(
                       name: "ExtensionSample",
                       version: "0.0.1",
                       isSingleton: true,
                       configurationType: new CrossFileTypeReference("types.json", factory.GetIndex(configurationType))))                   
                   .AddBicepResourceHandler<OmniHandler>()
                   .AddBicepResourceHandler<StronglyTypedHandler>()
                   .AddSingleton<IBackendService, LocalOutputService>();

            var app = builder.Build();
            app.UseBicepDispatcher();

            await app.RunAsync();
        }
    }
}
