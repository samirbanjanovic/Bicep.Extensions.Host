namespace Bicep.Extension.Host.Handlers
{
    public interface IResourceHandlerMap
    {
        TypedHandlerMap GetResourceHandler(string resourceType);
        TypedHandlerMap GetResourceHandler(Type resourceType);
        TypedHandlerMap GetResourceHandler<T>() where T : IGenericResourceHandler;
    }
}