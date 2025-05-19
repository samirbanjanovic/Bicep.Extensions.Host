using Bicep.Local.Extension.Protocol;
using System.Text.Json.Nodes;
//using Bicep.Local.Extension.Rpc;

namespace Bicep.Extension.Host;
public abstract class GenericTypedResourceHandler
    : ITypedResourceHandler
{
    public virtual Task<LocalExtensibilityOperationResponse> CreateOrUpdate(object resource, ResourceSpecification? resourceSpecification, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public virtual Task<LocalExtensibilityOperationResponse> Delete(ResourceReference resourceReference, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public virtual Task<LocalExtensibilityOperationResponse> Get(ResourceReference resourceReference, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public virtual Task<LocalExtensibilityOperationResponse> Preview(object resource, ResourceSpecification resourceSpecification, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public LocalExtensibilityOperationResponse Success(
        string resourceType,
        JsonObject? properties)
        => new
            (
                new
                (
                    resourceType,
                    "1.0.0",
                    "Succeeded",
                    new(),
                    new(),
                    properties ?? new JsonObject().AsObject()
                ),
                null
            );

    public LocalExtensibilityOperationResponse Error(
         string resourceType,
        JsonObject? properties)
        => new
            (
                new
                (
                    resourceType,
                    "1.0.0",
                    "Error",
                    new(),
                    new(),
                    properties ?? new JsonObject().AsObject()
                ),
                null
    );
}

public abstract class TypedResourceHandler<T>
    : GenericTypedResourceHandler, ITypedResourceHandler<T>
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

    public LocalExtensibilityOperationResponse Success(JsonObject? properties)
        => Success(typeof(T).Name, properties);

    public LocalExtensibilityOperationResponse Error(JsonObject? properties)
        => Error(typeof(T).Name, properties);
}

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

public interface ITypedResourceHandler<T>
    : ITypedResourceHandler
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

    Task<LocalExtensibilityOperationResponse> ITypedResourceHandler.CreateOrUpdate(object resource, ResourceSpecification? resourceSpecification, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    Task<LocalExtensibilityOperationResponse> ITypedResourceHandler.Preview(object resource, ResourceSpecification resourceSpecification, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

}

