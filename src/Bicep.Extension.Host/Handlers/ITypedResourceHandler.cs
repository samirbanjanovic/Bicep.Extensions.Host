using Bicep.Local.Extension.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bicep.Extension.Host.Handlers;


public interface ITypedResourceHandler<T>
    : IGenericResourceHandler
    where T : class
{
    Task<LocalExtensibilityOperationResponse> CreateOrUpdate(
       T resource,
       ResourceSpecification? resourceSpecification,
       CancellationToken cancellationToken);

    Task<LocalExtensibilityOperationResponse> Preview(
        T resource,
        ResourceSpecification resourceSpecification,
        CancellationToken cancellationToken);

    Task<LocalExtensibilityOperationResponse> IGenericResourceHandler.CreateOrUpdate(object resource, ResourceSpecification? resourceSpecification, CancellationToken cancellationToken)
        => this.CreateOrUpdate(resource as T ?? throw new ArgumentNullException(nameof(resource)), resourceSpecification, cancellationToken);

    Task<LocalExtensibilityOperationResponse> IGenericResourceHandler.Preview(object resource, ResourceSpecification resourceSpecification, CancellationToken cancellationToken)
        => this.Preview(resource as T ?? throw new ArgumentNullException(nameof(resource)), resourceSpecification, cancellationToken);
}

