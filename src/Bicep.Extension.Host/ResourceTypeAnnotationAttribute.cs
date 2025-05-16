using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bicep.Host.Types
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class ResourceTypeAnnotationAttribute
        : Attribute
    {
        public ResourceTypeAnnotationAttribute(bool genericHandling = true) 
        { 
            GenericHandling = genericHandling; 
        }

        public ResourceTypeAnnotationAttribute(string? description)
        {
            Description = description;         
        }

        public bool? GenericHandling { get; }

        public string? Description { get; }
    }
}
