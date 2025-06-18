using Azure.Bicep.Types;
using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Index;
using Bicep.Extension.Host;
using Bicep.Extension.Host.Extensions;
using Bicep.Extension.Sample.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;


namespace Bicep.Extension.Sample;
public class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebApplication
                            .CreateBuilder()
                            .AddBicepExtensionHost(args);

        builder.Services
               .AddBicepExtensionServices(name: "sample-ext", version: "0.0.1", isSingleton: true)
               .AddBicepResourceHandler<OmniHandler>()
               .AddBicepResourceHandler<StronglyTypedHandler>()
               .AddSingleton<IBackendService, LocalOutputService>();

        await builder.Build()
                     .MapBicepDispatcher<ResourceRequestDispatcher>()
                     .RunBicepExtensionAsync();
    }
}