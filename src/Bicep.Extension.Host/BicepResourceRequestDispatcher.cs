using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using Bicep.Host.Types;
using Bicep.Local.Extension.Protocol;
using Google.Protobuf.Collections;
using Grpc.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

using Rpc = Bicep.Local.Extension.Rpc;

namespace Bicep.Extension.Host
{

    public class BicepResourceRequestDispatcher
        : Rpc.BicepExtension.BicepExtensionBase
    {
        private readonly ILogger<BicepResourceRequestDispatcher> logger;
        private readonly BicepResourceHandlerMap resourceHandlerMap;
        private readonly ITypeSpecGenerator typeSpecGenerator;

        public BicepResourceRequestDispatcher(BicepResourceHandlerMap resourceHandlerMap, ITypeSpecGenerator typeSepcGenerator, ILogger<BicepResourceRequestDispatcher> logger)
        {
            this.logger = logger;
            this.typeSpecGenerator = typeSepcGenerator ?? throw new ArgumentNullException(nameof(typeSepcGenerator));   
            this.resourceHandlerMap = resourceHandlerMap ?? throw new ArgumentNullException(nameof(resourceHandlerMap));
        }

        public override Task<Rpc.LocalExtensibilityOperationResponse> CreateOrUpdate(Rpc.ResourceSpecification request, ServerCallContext context)
        {
            var (resource, resourceSpecification) = TypeConvert(request);
            return WrapExceptions(async () => Convert(await resourceHandlerMap.GetResourceHandler(request.Type).CreateOrUpdate(resource, resourceSpecification, context.CancellationToken)));
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
            => WrapExceptions(async () => Convert(await resourceHandlerMap.GetResourceHandler(request.Type).Get(Convert(request), context.CancellationToken)));

        public override Task<Rpc.LocalExtensibilityOperationResponse> Delete(Rpc.ResourceReference request, ServerCallContext context)
            => WrapExceptions(async () => Convert(await resourceHandlerMap.GetResourceHandler(request.Type).Delete(Convert(request), context.CancellationToken)));

        public override Task<Rpc.Empty> Ping(Rpc.Empty request, ServerCallContext context)
            => Task.FromResult(new Rpc.Empty());

        private (object Resource, ResourceSpecification Request) TypeConvert(Rpc.ResourceSpecification request)
        {
            var resourceType = request.Type;

            if (!resourceHandlerMap.TryGetResourceType(resourceType, out Type? type))
            {
                throw new ArgumentException($"No resource handler found for type {resourceType}");
            }
            
            var resourceJson = ToJsonObject(request.Properties, "Parsing resource properties failed. Please ensure is non-null or empty and is a valid JSON object.");

            var resource =  JsonSerializer.Deserialize(resourceJson.ToJsonString(), type);
            
            if(resource is null)
            {
                throw new ArgumentNullException("Parsing resource properties failed. Please ensure is non-null or empty and is a valid JSON object.");
            }

            ResourceSpecification resourceSpecification = Convert(request);

            return (resource, resourceSpecification);
        }

        private ResourceSpecification Convert(Rpc.ResourceSpecification request)
        {
            JsonObject? config = GetExtensionConfig(request.Config);
            var properties = ToJsonObject(request.Properties, "Parsing resource properties failed. Please ensure is non-null or empty and is a valid JSON object.");

            return new(request.Type, request.ApiVersion, properties, config);
        }

        private ResourceReference Convert(Rpc.ResourceReference request)
        {
            JsonObject identifiers = ToJsonObject(request.Identifiers, "Parsing resource identifiers failed. Please ensure is non-null or empty and is a valid JSON object.");
            JsonObject? config = GetExtensionConfig(request.Config);

            return new(request.Type, request.ApiVersion, identifiers, config);
        }

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

        private Rpc.Resource? Convert(Resource? response)
            => response is null ? null :
                new()
                {
                    Identifiers = response.Identifiers.ToJsonString(),
                    Properties = response.Properties.ToJsonString(),
                    Status = response.Status,
                    Type = response.Type,
                    ApiVersion = response.ApiVersion,
                };

        private Rpc.ErrorData? Convert(ErrorData? response)
        {
            if (response is null)
            {
                return null;
            }

            var errorData = new Rpc.ErrorData()
            {
                Error = new Rpc.Error()
                {
                    Code = response.Error.Code,
                    Message = response.Error.Message,
                    InnerError = response.Error.InnerError?.ToJsonString(),
                    Target = response.Error.Target,
                }
            };

            var errorDetails = Convert(response.Error.Details);
            if (errorDetails is not null)
            {
                errorData.Error.Details.AddRange(errorDetails);
            }
            return errorData;
        }

        private RepeatedField<Rpc.ErrorDetail>? Convert(ErrorDetail[]? response)
        {
            if (response is null)
            {
                return null;
            }

            var list = new RepeatedField<Rpc.ErrorDetail>();
            foreach (var item in response)
            {
                list.Add(Convert(item));
            }
            return list;
        }

        private Rpc.ErrorDetail Convert(ErrorDetail response)
            => new()
            {
                Code = response.Code,
                Message = response.Message,
                Target = response.Target
            };


        private Rpc.LocalExtensibilityOperationResponse Convert(LocalExtensibilityOperationResponse response)
            => new()
            {
                ErrorData = Convert(response.ErrorData),
                Resource = Convert(response.Resource)
            };

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