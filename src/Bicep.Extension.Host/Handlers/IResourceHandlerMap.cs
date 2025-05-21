namespace Bicep.Extension.Host.Handlers
{
    public interface IResourceHandlerFactory
    {
        TypedHandlerMap GetResourceHandler(string resourceType);
        TypedHandlerMap GetResourceHandler(Type resourceType);
    }
}