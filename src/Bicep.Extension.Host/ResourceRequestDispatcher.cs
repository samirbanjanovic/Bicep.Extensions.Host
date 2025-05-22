using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Bicep.Extension.Host.Handlers;
using Bicep.Extension.Host.TypeBuilder;
using Bicep.Local.Extension.Protocol;
using Google.Protobuf.Collections;
using Grpc.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

using Rpc = Bicep.Local.Extension.Rpc;

namespace Bicep.Extension.Host
{

    public class ResourceRequestDispatcher
        : Rpc.BicepExtension.BicepExtensionBase
    {
        private readonly ILogger<ResourceRequestDispatcher> logger;
        private readonly IResourceHandlerFactory resourceHandlerFactory;
        private readonly ITypeSpecGenerator typeSpecGenerator;

        public ResourceRequestDispatcher(IResourceHandlerFactory resourceHandlerFactory, ITypeSpecGenerator typeSepcGenerator, ILogger<ResourceRequestDispatcher> logger)
        {
            this.logger = logger;
            this.typeSpecGenerator = typeSepcGenerator ?? throw new ArgumentNullException(nameof(typeSepcGenerator));
            this.resourceHandlerFactory = resourceHandlerFactory ?? throw new ArgumentNullException(nameof(resourceHandlerFactory));
        }

        public override Task<Rpc.LocalExtensibilityOperationResponse> CreateOrUpdate(Rpc.ResourceSpecification request, ServerCallContext context)
        {
            var handlerRequest = GenerateHandlerRequest(request);
            return WrapExceptions(async () => ToLocalOperationResponse(await resourceHandlerFactory.GetResourceHandler(request.Type).Handler.CreateOrUpdate(handlerRequest, context.CancellationToken)));
        }

        public override Task<Rpc.LocalExtensibilityOperationResponse> Preview(Rpc.ResourceSpecification request, ServerCallContext context)
        {
            var spec = typeSpecGenerator.GenerateBicepResourceTypes();
            Rpc.LocalExtensibilityOperationResponse opResponse = new();
            opResponse.Resource = new Rpc.Resource()
            {
                Identifiers = spec.TypesJson,
                Properties = spec.IndexJson,
                Status = "Succeeded",
                Type = "types.json",
                ApiVersion = "1.0.0"
            };

            return Task.FromResult(opResponse);
        }

        public override Task<Rpc.LocalExtensibilityOperationResponse> Get(Rpc.ResourceReference request, ServerCallContext context)
            => WrapExceptions(async () => ToLocalOperationResponse(await resourceHandlerFactory.GetResourceHandler(request.Type).Handler.Get(ToHandlerRequest(request), context.CancellationToken)));

        public override Task<Rpc.LocalExtensibilityOperationResponse> Delete(Rpc.ResourceReference request, ServerCallContext context)
            => WrapExceptions(async () => ToLocalOperationResponse(await resourceHandlerFactory.GetResourceHandler(request.Type).Handler.Delete(ToHandlerRequest(request), context.CancellationToken)));

        public override Task<Rpc.Empty> Ping(Rpc.Empty request, ServerCallContext context)
            => Task.FromResult(new Rpc.Empty());

        private HandlerRequest? GenerateHandlerRequest(Rpc.ResourceSpecification request)
        {
            var handlerMap = resourceHandlerFactory.GetResourceHandler(request.Type);
            var resourceJson = ToJsonObject(request.Properties, "Parsing requested resource properties failed.");
            var extensionSettings = GetExtensionConfig(request.Config);

            if (handlerMap.Type == typeof(EmptyGeneric))
            {
                return new HandlerRequest(request.Type, request.ApiVersion, extensionSettings, resourceJson);
            }

            var resource = DeserializeJson(request.Type, resourceJson, handlerMap);
            var resourceType = typeof(HandlerRequest<>).MakeGenericType(handlerMap.Type);

            if (resourceType is null)
            {
                throw new InvalidOperationException($"Failed to generate request for {request.Type}");
            }

            var handlerRequest = Activator.CreateInstance(resourceType, resource, request.ApiVersion, extensionSettings, resourceJson) as HandlerRequest;

            return handlerRequest
                ?? throw new InvalidOperationException($"Failed to process strongly typed request for {request.Type}");

        }

        private HandlerRequest ToHandlerRequest(Rpc.ResourceReference resourceReference)
        {
            var extensionSettings = GetExtensionConfig(resourceReference.Config);

            return new HandlerRequest(resourceReference.Type, resourceReference.HasApiVersion ? resourceReference.ApiVersion : "0.0.0");
        }

        private static object? DeserializeJson(string bicepType, JsonObject? resourceJson, TypedHandlerMap handlerMap)
        {
            if (resourceJson is null)
            {
                throw new ArgumentNullException($"No type mapping exists for resource `{resourceJson}`");
            }

            var jsonSerializerSettings = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
            };

            var resource = JsonSerializer.Deserialize(resourceJson.ToJsonString(), handlerMap.Type, options: jsonSerializerSettings);

            if (resource is null)
            {
                throw new ArgumentNullException($"No type mapping exists for resource `{bicepType}`");
            }

            return resource;
        }

        private Rpc.LocalExtensibilityOperationResponse ToLocalOperationResponse(HandlerResponse handlerResponse)
            => new Rpc.LocalExtensibilityOperationResponse()
            {
                ErrorData = handlerResponse.Status == HandlerResponseStatus.Failed && handlerResponse.Error is not null ?
                              new Rpc.ErrorData
                              {
                                  Error = new Rpc.Error()
                                  {
                                      Code = handlerResponse.Error.Code,
                                      Message = handlerResponse.Message,
                                      InnerError = handlerResponse.Error.Message,
                                      Target = handlerResponse.Error.Target,
                                  }
                              } : null,
                Resource = handlerResponse.Status != HandlerResponseStatus.Failed ?
                              new Rpc.Resource()
                              {
                                  Status = handlerResponse.Status.ToString(),
                                  Type = handlerResponse.Type,
                                  ApiVersion = handlerResponse.Version,
                                  Properties = handlerResponse.Properties.ToJsonString(),
                                  Identifiers = string.Empty
                              } : null
            };


        private JsonObject? GetExtensionConfig(string extensionConfig)
        {
            JsonObject? config = null;
            if (!string.IsNullOrEmpty(extensionConfig))
            {
                config = ToJsonObject(extensionConfig, "Parsing extension config failed. Please ensure is a valid JSON object.");
            }
            return config;
        }

        private JsonObject ToJsonObject(string json, string errorMessage)
            => JsonNode.Parse(json)?.AsObject() ?? throw new ArgumentNullException(errorMessage);


        private static async Task<Rpc.LocalExtensibilityOperationResponse> WrapExceptions(Func<Task<Rpc.LocalExtensibilityOperationResponse>> func)
        {
            try
            {
                return await func();
            }
            catch (Exception ex)
            {
                var response = new Rpc.LocalExtensibilityOperationResponse
                {
                    Resource = null,
                    ErrorData = new Rpc.ErrorData
                    {
                        Error = new Rpc.Error
                        {
                            Message = $"Rpc request failed: {ex}",
                            Code = "RpcException",
                            Target = ""
                        }
                    }
                };

                return response;
            }
        }

    }
}