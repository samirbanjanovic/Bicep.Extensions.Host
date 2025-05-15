using Bicep.Local.Extension.Protocol;

namespace Bicep.Extension.Sample.Handlers
{
    public class OmniHandler
       : IGenericResourceHandler
    {
        private readonly IBackendService backendService;

        public OmniHandler(IBackendService backendService)
        {
            this.backendService = backendService;
        }

        public async Task<LocalExtensibilityOperationResponse> CreateOrUpdate(ResourceSpecification request, CancellationToken cancellationToken)
        {                        
            var properties = request.Properties;
            var type = request.Type;

            await this.backendService.CreateOrUpdate(properties);

            return new(
                new(request.Type, request.ApiVersion, "Succeeded", new(), request.Config, new()),
                null);
        }

        public Task<LocalExtensibilityOperationResponse> Delete(ResourceReference request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<LocalExtensibilityOperationResponse> Get(ResourceReference request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<LocalExtensibilityOperationResponse> Preview(ResourceSpecification request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
