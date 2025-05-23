using Bicep.Extension.Host.Handlers;

namespace Bicep.Extension.Host.TypeBuilder;

public static class TypeExtensions
{

    public static bool IsGenericTypedResourceHandler(this Type type)
        => type.GetInterfaces().Any(i => !i.IsGenericType && i == typeof(IResourceHandler));


    public static bool TryGetTypedResourceHandlerInterface(this Type type, out Type? resourceHandlerInterface)
    {
        resourceHandlerInterface = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IResourceHandler<>));

        return resourceHandlerInterface is not null;
    }
}

