using System.CodeDom.Compiler;
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
    public abstract class BicepTypedExtensionBase
    {
        [GeneratedCode("grpc_csharp_plugin", null)]
        public virtual Task<string> Describe(Rpc.Empty request, ServerCallContext context)
            => throw new RpcException(new Status(StatusCode.Unimplemented, ""));

        [GeneratedCode("grpc_csharp_plugin", null)]
        public virtual Task<LocalExtensibilityOperationResponse> CreateOrUpdate(ResourceSpecification request, ServerCallContext context)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, ""));
        }

        [GeneratedCode("grpc_csharp_plugin", null)]
        public virtual Task<LocalExtensibilityOperationResponse> Preview(ResourceSpecification request, ServerCallContext context)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, ""));
        }

        [GeneratedCode("grpc_csharp_plugin", null)]
        public virtual Task<LocalExtensibilityOperationResponse> Get(ResourceReference request, ServerCallContext context)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, ""));
        }

        [GeneratedCode("grpc_csharp_plugin", null)]
        public virtual Task<LocalExtensibilityOperationResponse> Delete(ResourceReference request, ServerCallContext context)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, ""));
        }

        [GeneratedCode("grpc_csharp_plugin", null)]
        public virtual Task<Rpc.Empty> Ping(Rpc.Empty request, ServerCallContext context)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, ""));
        }
    }

}
