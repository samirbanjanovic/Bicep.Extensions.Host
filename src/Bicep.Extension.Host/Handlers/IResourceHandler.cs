using Bicep.Local.Extension.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bicep.Extension.Host.Handlers;

public interface IResourceHandler
{
    Task<LocalExtensibilityOperationResponse> CreateOrUpdate(
       object resource,
       ResourceSpecification? resourceSpecification,
       CancellationToken cancellationToken);

    Task<LocalExtensibilityOperationResponse> Preview(
        object resource,
        ResourceSpecification resourceSpecification,
        CancellationToken cancellationToken);

    Task<LocalExtensibilityOperationResponse> Delete(
        ResourceReference resourceReference,
        CancellationToken cancellationToken);

    Task<LocalExtensibilityOperationResponse> Get(
        ResourceReference resourceReference,
        CancellationToken cancellationToken);
}

public interface IResourceHandler<T>
    : IResourceHandler
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

    Task<LocalExtensibilityOperationResponse> IResourceHandler.CreateOrUpdate(object resource, ResourceSpecification? resourceSpecification, CancellationToken cancellationToken)
    => this.CreateOrUpdate(resource as T ?? throw new ArgumentNullException(nameof(resource)), resourceSpecification, cancellationToken);

    Task<LocalExtensibilityOperationResponse> IResourceHandler.Preview(object resource, ResourceSpecification resourceSpecification, CancellationToken cancellationToken)
        => this.Preview(resource as T ?? throw new ArgumentNullException(nameof(resource)), resourceSpecification, cancellationToken);


}

