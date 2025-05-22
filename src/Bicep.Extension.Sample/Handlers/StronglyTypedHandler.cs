using Bicep.Extension.Host.Handlers;
using Bicep.Extension.Sample.Models;
using Bicep.Local.Extension.Protocol;
using CommandLine;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Bicep.Extension.Sample.Handlers
{
    public class StronglyTypedHandler
       : IResourceHandler<StronglyTypedResource>
    {
        private readonly IBackendService backendService;

        public StronglyTypedHandler(IBackendService backendService)
        {
            this.backendService = backendService;
        }

        public async Task<HandlerResponse> CreateOrUpdate(HandlerRequest<StronglyTypedResource> request, CancellationToken cancellationToken)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
            };

            var json = JsonSerializer.Serialize(request.Resource, jsonOptions);

            await this.backendService.CreateOrUpdate(json);

            return HandlerResponse.Success(
                        request.Type,
                        "0.0.1",
                        JsonObject.Parse(json)?.AsObject() ?? new());
        }

        public Task<HandlerResponse> Delete(HandlerRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<HandlerResponse> Get(HandlerRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<HandlerResponse> Preview(HandlerRequest<StronglyTypedResource> request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
