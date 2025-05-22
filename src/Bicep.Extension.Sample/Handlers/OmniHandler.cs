using Bicep.Extension.Host.Handlers;
using Bicep.Local.Extension.Protocol;
using System.Text.Json;

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

        public async Task<HandlerResponse> CreateOrUpdate(HandlerRequest request, CancellationToken cancellationToken)
        {
            await this.backendService.CreateOrUpdate(request.ResourceJson.ToJsonString());

            return HandlerResponse.Success(
                        request.Type,
                        "0.0.1",
                        new());                        
        }

        public Task<HandlerResponse> Delete(HandlerRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<HandlerResponse> Get(HandlerRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<HandlerResponse> Preview(HandlerRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
