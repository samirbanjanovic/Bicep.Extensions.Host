using Bicep.Extension.Host.Handlers;
using Bicep.Extension.Sample.Models;
using Bicep.Local.Extension.Protocol;
using CommandLine;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bicep.Extension.Sample.Handlers
{
    public class OmniHandler
       : Host.Handlers.IResourceHandler
    {
        private readonly IBackendService backendService;

        public OmniHandler(IBackendService backendService)
        {
            this.backendService = backendService;
        }

        public async Task<LocalExtensibilityOperationResponse> CreateOrUpdate(object resource, ResourceSpecification? resourceSpecification, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(resource);

            await this.backendService.CreateOrUpdate(json);

            return new
            (
                new
                (
                    resourceSpecification.Type,
                    "1.0.0",
                    "Succeeded",
                    new(),
                    new(),
                    resourceSpecification?.Properties ?? new JsonObject().AsObject()
                ),
                null
            );
        }

        public Task<LocalExtensibilityOperationResponse> Delete(ResourceReference resourceReference, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<LocalExtensibilityOperationResponse> Get(ResourceReference resourceReference, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<LocalExtensibilityOperationResponse> Preview(object resource, ResourceSpecification resourceSpecification, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
