using Bicep.Local.Extension.Protocol;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bicep.Extension.Host
{
    public class BicepResourceHandlerMap
    {
        private readonly ImmutableDictionary<string, Type> typeDeclerations;
        private readonly ImmutableDictionary<Type, ITypedResourceHandler> resourceHandlerMap;

        public BicepResourceHandlerMap(IEnumerable<ITypedResourceHandler>? resourceHandlers)
        {
            if ((resourceHandlers is null || resourceHandlers.Count() == 0))
            {
                throw new InvalidOperationException("No resource handlers were provided.");
            }

            this.resourceHandlerMap = BuildResourceHandlerMap(resourceHandlers);
        }


        public bool TryGetResourceType(string typeName, out Type? type)
            => typeDeclerations.TryGetValue(typeName, out type);

        public ITypedResourceHandler GetResourceHandler(string typeName)
        {
            if (typeDeclerations.TryGetValue(typeName, out var resourceType))
            {
                return GetResourceHandler(resourceType);
            }
            throw new ArgumentException($"No resource handler found for type {typeName}");
        }

        public ITypedResourceHandler GetResourceHandler<T>()
            where T : TypedResourceHandler<T>
            => GetResourceHandler(typeof(T));

        public ITypedResourceHandler GetResourceHandler(Type resourceType)
        {
            if (resourceHandlerMap.TryGetValue(resourceType, out var handler))
            {
                return handler;
            }

            throw new ArgumentException($"No resource handler found for type {resourceType}");
        }

        private static ImmutableDictionary<Type, ITypedResourceHandler> BuildResourceHandlerMap(IEnumerable<ITypedResourceHandler>? resourceHandlers)
        {
            var hanlderDictionary = new Dictionary<Type, ITypedResourceHandler>();

            // if the resource handler is generic extract the type and add it to the type decleration dictionary
            // as well as the resource handler map
            foreach (var resourceHandler in resourceHandlers)
            {
                Type? resourceType = typeof(object);
                if (resourceHandler.GetType().IsGenericType)
                {
                    resourceType = resourceHandler.GetType().GetGenericArguments()[0];

                }
                
                if (!hanlderDictionary.TryAdd(resourceType, resourceHandler))
                {
                    throw new ArgumentException($"Resource handler for { (resourceType == typeof(object) ? "generic type" : $"type {resourceType}")}  already exists.");
                }                
            }

            return hanlderDictionary.ToImmutableDictionary();
        }
    }
}
