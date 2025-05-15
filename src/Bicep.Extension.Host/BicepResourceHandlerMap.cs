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
        private readonly ImmutableDictionary<string, IResourceHandler> resourceHandlerMap;
        private readonly IGenericResourceHandler genericResourceHandler;

        public BicepResourceHandlerMap(IEnumerable<IResourceHandler> resourceHandlers, IGenericResourceHandler genericResourceHandler)
        {
            if ((resourceHandlers is null || resourceHandlers.Count() == 0) && genericResourceHandler is null)
            {
                throw new InvalidOperationException("No resource handlers were provided.");
            }

            this.resourceHandlerMap = BuildResourceHandlerMap(resourceHandlers);
            this.genericResourceHandler = genericResourceHandler;
        }

        public IGenericResourceHandler GetResourceHandler(string resourceType)
        {
            if (resourceHandlerMap.TryGetValue(resourceType, out var handler))
            {
                return handler;
            }
            else if (genericResourceHandler is not null)
            {
                return genericResourceHandler;
            }


            throw new ArgumentException($"No resource handler found for type {resourceType}");
        }

        private static ImmutableDictionary<string, IResourceHandler> BuildResourceHandlerMap(IEnumerable<IResourceHandler>? resourceHandlers)
        {

            var hanlderDictionary = new Dictionary<string, IResourceHandler>();
            foreach (var resourceHandler in resourceHandlers!)
            {
                if (!hanlderDictionary.TryAdd(resourceHandler.ResourceType, resourceHandler))
                {
                    throw new ArgumentException($"A resource handler for type {resourceHandler.ResourceType} has already been registered");
                }
            }
            return hanlderDictionary.ToImmutableDictionary();
        }
    }
}
