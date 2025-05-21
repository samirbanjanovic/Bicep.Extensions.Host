using Bicep.Extension.Host.Handlers;
using Bicep.Extension.Sample.Models;
using Bicep.Local.Extension.Protocol;
using CommandLine;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bicep.Extension.Sample.Handlers
{
    public class StronglyTypedHandler
       : ITypedResourceHandler<StronglyTypedResource>
    {
        private readonly IBackendService backendService;

        public StronglyTypedHandler(IBackendService backendService)
        {
            this.backendService = backendService;
        }

        public async Task<LocalExtensibilityOperationResponse> CreateOrUpdate(StronglyTypedResource resource, ResourceSpecification? resourceSpecification, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(resource);

            await this.backendService.CreateOrUpdate(json);

            return new
            (
                new
                (
                    typeof(StronglyTypedResource).Name,
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

        public Task<LocalExtensibilityOperationResponse> Preview(StronglyTypedResource resource, ResourceSpecification resourceSpecification, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
