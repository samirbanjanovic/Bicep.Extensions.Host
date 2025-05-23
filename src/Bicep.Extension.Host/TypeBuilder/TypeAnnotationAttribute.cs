using Azure.Bicep.Types.Concrete;

namespace Bicep.Extension.Host.TypeBuilder;


[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class BicepTypeAttribute : Attribute
{
    public BicepTypeAttribute(bool isActive = true)
    {
        IsActive = isActive;
    }

    public bool IsActive { get; set; }
}


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

