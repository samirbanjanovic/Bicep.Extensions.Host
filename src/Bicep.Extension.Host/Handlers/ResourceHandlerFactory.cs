using Bicep.Extension.Host.Extensions;
using System.Collections.Immutable;

namespace Bicep.Extension.Host.Handlers;

internal record EmptyGeneric();
public class ResourceHandlerFactory
    : IResourceHandlerFactory
{
    public IImmutableDictionary<string, TypeResourceHandler>? TypedResourceHandlers { get; }
    public TypeResourceHandler? GenericResourceHandler { get; }

    public ResourceHandlerFactory(IEnumerable<IResourceHandler> resourceHandlers)
    {
        if (resourceHandlers is null || resourceHandlers.Count() == 0)
        {
            throw new InvalidOperationException("No resource handlers were provided.");
        }

        var resourceHandlerMaps = BuildResourceHandlerMap(resourceHandlers);

        TypedResourceHandlers = resourceHandlerMaps.Typed;
        GenericResourceHandler = resourceHandlerMaps.Generic;
    }


    public TypeResourceHandler? GetResourceHandler(Type resourceType)
        => GetResourceHandler(resourceType?.Name ?? throw new ArgumentNullException(nameof(resourceType)));

    public TypeResourceHandler? GetResourceHandler(string resourceType)
    {
        TypeResourceHandler? handlerMap;
        if (TypedResourceHandlers?.TryGetValue(resourceType, out handlerMap) == true)
        {
            return handlerMap;
        }
        else if (GenericResourceHandler is not null)
        {
            return GenericResourceHandler;
        }

        return null;
    }

    private static (TypeResourceHandler? Generic, ImmutableDictionary<string, TypeResourceHandler> Typed) BuildResourceHandlerMap(IEnumerable<IResourceHandler> resourceHandlers)
    {
        var handlerDictionary = new Dictionary<string, TypeResourceHandler>();
        TypeResourceHandler? genericHandler = null;
        // if the resource handler is generic extract the type and add it to the type decleration dictionary
        // as well as the resource handler map
        foreach (var resourceHandler in resourceHandlers)
        {
            var resourceHandlerType = resourceHandler.GetType();

            if (resourceHandlerType.TryGetTypedResourceHandlerInterface(out Type? baseInterface))
            {
                Type resourceType = baseInterface.GetGenericArguments()[0];
                if (!handlerDictionary.TryAdd(resourceType.Name, new(resourceType, resourceHandler)))
                {
                    throw new ArgumentException($"A resource handler for {resourceType.Name} has already been registered.");
                }
            }
            else if (resourceHandlerType.IsGenericTypedResourceHandler())
            {
                if (genericHandler is not null)
                {
                    throw new ArgumentException($"A generic resource handler has already been registered.");
                }

                genericHandler = new TypeResourceHandler(typeof(EmptyGeneric), resourceHandler);
            }
            else
            {
                throw new ArgumentException($"Unable to register handler {resourceHandlerType.FullName}");
            }
        }

        return (genericHandler, handlerDictionary.ToImmutableDictionary());
    }
}
