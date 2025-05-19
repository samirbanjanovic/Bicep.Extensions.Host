using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bicep.Extension.Host
{
    public static class TypeExtensions
    {

        public static bool IsGenericTypedResourceHandler(this Type type)
            => type.GetInterfaces().Any(i => !i.IsGenericType && i == typeof(ITypedResourceHandler));


        public static bool TryGetTypedResourceHandlerInterface(this Type type, out Type? resourceHandlerInterface)
        {
            resourceHandlerInterface = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITypedResourceHandler<>));

            return resourceHandlerInterface is not null;
        }
    }
}
