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
    public interface ITypedResourceHandler
    {
        Task<LocalExtensibilityOperationResponse> CreateOrUpdate(
            object resource,
            ResourceSpecification? resourceSpecification,
            CancellationToken cancellationToken);

        Task<LocalExtensibilityOperationResponse> Preview(
            object resource,
            ResourceSpecification resourceSpecification,
            CancellationToken cancellationToken);

        Task<LocalExtensibilityOperationResponse> Get(
            ResourceReference resourceReference,
            CancellationToken cancellationToken);

        Task<LocalExtensibilityOperationResponse> Delete(
            ResourceReference resourceReference,
            CancellationToken cancellationToken);
    }

    public abstract class TypedResourceHandler<T>
        : ITypedResourceHandler
        where T : class
    {
        public abstract Task<LocalExtensibilityOperationResponse> CreateOrUpdate(
            T resource,
            ResourceSpecification? resourceSpecification,
            CancellationToken cancellationToken);

        public abstract Task<LocalExtensibilityOperationResponse> Preview(
            T resource,
            ResourceSpecification resourceSpecification,
            CancellationToken cancellationToken);

        public abstract Task<LocalExtensibilityOperationResponse> Get(
            ResourceReference resourceReference,
            CancellationToken cancellationToken);

        public abstract Task<LocalExtensibilityOperationResponse> Delete(
            ResourceReference resourceReference,
            CancellationToken cancellationToken);

        Task<LocalExtensibilityOperationResponse> ITypedResourceHandler.CreateOrUpdate(object resource, ResourceSpecification? resourceSpecification, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<LocalExtensibilityOperationResponse> ITypedResourceHandler.Preview(object resource, ResourceSpecification resourceSpecification, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<LocalExtensibilityOperationResponse> ITypedResourceHandler.Get(ResourceReference resourceReference, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<LocalExtensibilityOperationResponse> ITypedResourceHandler.Delete(ResourceReference resourceReference, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
