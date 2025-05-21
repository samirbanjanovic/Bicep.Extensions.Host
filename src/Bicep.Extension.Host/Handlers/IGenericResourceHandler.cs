using Bicep.Local.Extension.Protocol;
using System.Text.Json.Nodes;

namespace Bicep.Extension.Host.Handlers;

public interface IGenericResourceHandler
{
    Task<LocalExtensibilityOperationResponse> CreateOrUpdate(
        dynamic resource,
        ResourceSpecification? resourceSpecification,
        CancellationToken cancellationToken);

    Task<LocalExtensibilityOperationResponse> Preview(
        dynamic resource,
        ResourceSpecification resourceSpecification,
        CancellationToken cancellationToken);

    Task<LocalExtensibilityOperationResponse> Get(
        ResourceReference resourceReference,
        CancellationToken cancellationToken);

    Task<LocalExtensibilityOperationResponse> Delete(
        ResourceReference resourceReference,
        CancellationToken cancellationToken);
}
