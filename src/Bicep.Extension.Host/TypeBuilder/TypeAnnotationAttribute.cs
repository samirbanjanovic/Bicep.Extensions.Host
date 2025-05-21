using Azure.Bicep.Types.Concrete;
using System;

namespace Bicep.Extension.Host.TypeBuilder
{
    [AttributeUsage(AttributeTargets.Property)]
    public class TypeAnnotationAttribute : Attribute
    {
        public TypeAnnotationAttribute(
            string? description,
            ObjectTypePropertyFlags flags = ObjectTypePropertyFlags.None,
            bool isSecure = false)
        {
            Description = description;
            Flags = flags;
            IsSecure = isSecure;
        }

        public string? Description { get; }

        public ObjectTypePropertyFlags Flags { get; }

        public bool IsSecure { get; }
    }
}
