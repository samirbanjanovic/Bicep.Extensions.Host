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
            where T : ITypedResourceHandler
            => GetResourceHandler(typeof(T));

        public ITypedResourceHandler GetResourceHandler(Type resourceType)
        {
            ITypedResourceHandler handler;
            if (resourceHandlerMap.TryGetValue(resourceType, out handler))
            {
                return handler;
            }
            else if (resourceHandlerMap.TryGetValue(typeof(object), out handler))
            {
                return handler;
            }

            throw new ArgumentException($"No generic resource handler or strongly typed resource handler exists for type {resourceType}");
        }

        private static ImmutableDictionary<Type, ITypedResourceHandler> BuildResourceHandlerMap(IEnumerable<ITypedResourceHandler>? resourceHandlers)
        {
            var hanlderDictionary = new Dictionary<Type, ITypedResourceHandler>();

            // if the resource handler is generic extract the type and add it to the type decleration dictionary
            // as well as the resource handler map
            foreach (var resourceHandler in resourceHandlers)
            {
                var resourceHandlerType = resourceHandler.GetType();
                // use typeof(object) to indicate a generic resource handler
                var resourceType = typeof(object);
                if (resourceHandlerType.TryGetTypedResourceHandlerInterface(out Type? baseInterface))
                {
                    resourceType = baseInterface.GetGenericArguments()[0];
                }
                else if (!resourceHandlerType.IsGenericTypedResourceHandler())
                {
                    throw new ArgumentException($"Resource handler {resourceHandler.GetType()} is not a generic resource handler or typed resource handler.");
                }

                if (!hanlderDictionary.TryAdd(resourceType, resourceHandler))
                {
                    throw new ArgumentException($"Resource handler for {(resourceType == typeof(object) ? "generic type" : $"type {resourceType}")}  already exists.");
                }
            }

            return hanlderDictionary.ToImmutableDictionary();
        }
    }
}
