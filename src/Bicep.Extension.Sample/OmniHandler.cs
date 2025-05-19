using Bicep.Extension.Host;
using Bicep.Extension.Sample.Models;
using Bicep.Local.Extension.Protocol;
using CommandLine;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bicep.Extension.Sample.Handlers
{
    public class OmniHandler
       : TypedResourceHandler<StronglyTypedResource>
    {
        private readonly IBackendService backendService;

        public OmniHandler(IBackendService backendService)
        {
            this.backendService = backendService;
        }

        public override async Task<LocalExtensibilityOperationResponse> CreateOrUpdate(StronglyTypedResource resource, ResourceSpecification? resourceSpecification, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(resource);

            await this.backendService.CreateOrUpdate(json);

            return Success(
            resource.GetType().Name
            , new()
            {
                ["name"] = resourceSpecification.Properties["name"],
                ["type"] = resourceSpecification.Type,
                ["apiVersion"] = resourceSpecification.ApiVersion,
                ["properties"] = new JsonObject()
                {
                    ["status"] = "Created"
                }
            });
        }

        public override Task<LocalExtensibilityOperationResponse> Delete(ResourceReference resourceReference, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task<LocalExtensibilityOperationResponse> Get(ResourceReference resourceReference, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task<LocalExtensibilityOperationResponse> Preview(StronglyTypedResource resource, ResourceSpecification resourceSpecification, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
