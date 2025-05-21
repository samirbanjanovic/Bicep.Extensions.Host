using Bicep.Extension.Host.TypeBuilder;
using Bicep.Local.Extension.Protocol;
using System.Collections.Immutable;

namespace Bicep.Extension.Host.Handlers
{
    public record TypedHandlerMap(Type Type, IGenericResourceHandler Handler);

    public class ResourceHandlerMap 
        : IResourceHandlerMap
    {
        private readonly ImmutableDictionary<string, TypedHandlerMap> typedResourceHandlers;
        private TypedHandlerMap? genericResourceHandler;

        public ResourceHandlerMap(IEnumerable<ITypedResourceHandler<object>> resourceHandlers)
        {
            if (resourceHandlers is null || resourceHandlers.Count() == 0)
            {
                throw new InvalidOperationException("No resource handlers were provided.");
            }

            var resourceHandlerMaps = BuildResourceHandlerMap(resourceHandlers);

            typedResourceHandlers = resourceHandlerMaps.Typed;
            genericResourceHandler = resourceHandlerMaps.Generic;
        }


        public TypedHandlerMap GetResourceHandler<T>()
            where T : IGenericResourceHandler
            => GetResourceHandler(typeof(T));

        public TypedHandlerMap GetResourceHandler(Type resourceType)
            => GetResourceHandler(resourceType?.Name ?? throw new ArgumentNullException(nameof(resourceType)));

        public TypedHandlerMap GetResourceHandler(string resourceType)
        {
            TypedHandlerMap? handlerMap;
            if (typedResourceHandlers.TryGetValue(resourceType, out handlerMap))
            {
                return handlerMap;
            }
            else if (genericResourceHandler is not null)
            {
                return genericResourceHandler;
            }

            throw new ArgumentException($"No generic resource handler is regsitered and no strongly typed resource handler exists for type {resourceType}");
        }
        
        private static (TypedHandlerMap? Generic, ImmutableDictionary<string, TypedHandlerMap> Typed) BuildResourceHandlerMap(IEnumerable<IGenericResourceHandler>? resourceHandlers)
        {
            var hanlderDictionary = new Dictionary<string, TypedHandlerMap>();
            TypedHandlerMap? genericHandler = null;
            // if the resource handler is generic extract the type and add it to the type decleration dictionary
            // as well as the resource handler map
            foreach (var resourceHandler in resourceHandlers)
            {
                var resourceHandlerType = resourceHandler.GetType();

                if (resourceHandlerType.TryGetTypedResourceHandlerInterface(out Type? baseInterface))
                {
                    var resourceType = baseInterface.GetGenericArguments()[0];
                    if(!hanlderDictionary.TryAdd(resourceType.Name, new(resourceType, resourceHandler)))
                    {
                        throw new ArgumentException($"A resource handler for {resourceType.Name} has already been registered.");
                    }
                }
                else if (resourceHandlerType.IsGenericTypedResourceHandler())
                {
                    if(genericHandler is null)
                    {
                        genericHandler = new(typeof(object), resourceHandler);
                    }
                    else
                    {
                        throw new ArgumentException("Only one generic handler can be registered");
                    }
                }
                else
                {
                    throw new ArgumentException($"Unable to register handler {resourceHandlerType.FullName}");
                }
            }

            return (genericHandler, hanlderDictionary.ToImmutableDictionary());
        }
    }
}
