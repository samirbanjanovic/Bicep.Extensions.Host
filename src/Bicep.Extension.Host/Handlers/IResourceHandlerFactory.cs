namespace Bicep.Extension.Host.Handlers;
public record TypedHandlerMap(Type Type, IResourceHandler Handler);

public interface IResourceHandlerFactory
{
    IEnumerable<TypedHandlerMap> GetAllResourceHandlers();
    TypedHandlerMap GetResourceHandler(string resourceType);
    TypedHandlerMap GetResourceHandler(Type resourceType);
}