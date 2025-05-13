
using Bicep.Local.Extension;
using Bicep.Local.Extension.Protocol;
using Microsoft.Extensions.DependencyInjection;

namespace Bicep.Extension.Tricep
{
    public static class Tricep
    {
        public static async Task FlexAsync(Action<IServiceCollection>? services, string[] args)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // resolve all handlers 
            var serviceCollection = new ServiceCollection();

            // configure the DI container
            services(serviceCollection);
           
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // resolve all handlers
            var typedHandlers = FindHandler<IResourceHandler>(serviceCollection);

            // ensure that we only grab the generic handler that isn't a typed handler, if there are any
            var genericHandler = FindHandler<IGenericResourceHandler>(serviceCollection, typedHandlers).FirstOrDefault();

            await ProviderExtension.Run(new TricepKestrelProviderExtension(services), builder =>
            {
                if(typedHandlers is not null && typedHandlers.Length > 0)
                {
                    foreach (var handler in typedHandlers)
                    {
                        builder.AddHandler((IResourceHandler)serviceProvider.GetService(handler!)!);
                    }
                }

                if(genericHandler is not null)
                {
                    builder.AddGenericHandler((IGenericResourceHandler)serviceProvider.GetRequiredService(genericHandler!));
                }
                
            }, args);
        }

        private static Type?[] FindHandler<T>(IServiceCollection serviceCollection, Type?[] exclusionSet = null!)
            where T : IGenericResourceHandler
        {
            var query = serviceCollection.Where(service => service.ServiceType.IsAssignableTo(typeof(T)));

            if (exclusionSet is not null)
            {
                query = query.Where(service => !exclusionSet.Contains(service.ImplementationType));
            }

            return query.Select(service => service.ImplementationType)
                        .ToArray();
        }
    }
}
