using Azure.Bicep.Types;
using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Index;
using Azure.Bicep.Types.Serialization;
using Bicep.Extension.Host.Handlers;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;

namespace Bicep.Extension.Host.TypeBuilder;
public class TypeSpecGenerator
     : ITypeSpecGenerator
{
    private readonly HashSet<Type> visited;

    protected readonly ImmutableArray<IResourceHandler>? resourceHandlers;

    protected readonly ConcurrentDictionary<Type, TypeBase> typeCache;
    protected readonly TypeFactory factory;

    public TypeSettings Settings { get; }

    public TypeSpecGenerator(TypeSettings typeSettings
                            , TypeFactory factory
                            , IEnumerable<IResourceHandler>? resourceHandlers)
    {
        if (typeSettings is null)
        {
            throw new ArgumentNullException(nameof(typeSettings));
        }
            
        this.visited = new HashSet<Type>();
        this.resourceHandlers = resourceHandlers?.ToImmutableArray();

        this.typeCache = new ConcurrentDictionary<Type, TypeBase>();
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));

        Settings = typeSettings;
    }

    protected virtual Type[] GetResourceTypes()
    {
        if (this.resourceHandlers is null)
            throw new ArgumentNullException(nameof(resourceHandlers));

        var types = new Dictionary<string, Type>();
        foreach (var resourceHandler in this.resourceHandlers)
        {
            if (resourceHandler.GetType().TryGetTypedResourceHandlerInterface(out var resourceHandlerInterface))
            {
                var genericType = resourceHandlerInterface.GetGenericArguments()[0];

                types.TryAdd(genericType.Name, genericType);
            }
        }

        AppDomain
            .CurrentDomain
            .GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type =>
                {
                    var bicepType = type.GetCustomAttributes(typeof(BicepTypeAttribute), true).FirstOrDefault();
                    bool activeBicepType = false;
                    if (bicepType is not null)
                    {
                        activeBicepType = ((BicepTypeAttribute)bicepType).IsActive;
                    }

                    return type.IsClass && activeBicepType && types.TryAdd(type.Name, type);
                })
            .ToList();
        
        return types.Values.ToArray();
    }

    public virtual TypeSpec GenerateBicepResourceTypes()
    {
        var resourceTypes = GetResourceTypes()
                                .Select(rt => GenerateResource(factory, typeCache, rt))
                                .ToDictionary(rt => rt.Name, rt => new CrossFileTypeReference("types.json", factory.GetIndex(rt)));

        var index = new TypeIndex(resourceTypes
                    , new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<CrossFileTypeReference>>>()
                    , Settings
                    , null);

        return new(GetString(stream => TypeSerializer.Serialize(stream, factory.GetTypes())),
                   GetString(stream => TypeSerializer.SerializeIndex(stream, index)));
    }

    protected virtual TypeBase GenerateForRecord(TypeFactory factory, ConcurrentDictionary<Type, TypeBase> typeCache, Type type)
    {
        var typeProperties = new Dictionary<string, ObjectTypeProperty>();
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if(visited.Contains(property.PropertyType))
            {
                continue;
            }
            visited.Add(property.PropertyType);

            var annotation = property.GetCustomAttributes<TypeAnnotationAttribute>(true).FirstOrDefault();
            var propertyType = property.PropertyType;
            TypeBase typeReference;

            if (propertyType == typeof(string) && annotation?.IsSecure == true)
            {
                typeReference = factory.Create(() => new StringType(sensitive: true));
            }
            else if (propertyType == typeof(string))
            {
                typeReference = typeCache.GetOrAdd(propertyType, _ => factory.Create(() => new StringType()));
            }
            else if (propertyType == typeof(bool))
            {
                typeReference = typeCache.GetOrAdd(propertyType, _ => factory.Create(() => new BooleanType()));
            }
            else if (propertyType == typeof(int))
            {
                typeReference = typeCache.GetOrAdd(propertyType, _ => factory.Create(() => new IntegerType()));
            }
            else if (propertyType.IsClass)
            {
                typeReference = typeCache.GetOrAdd(propertyType, _ => factory.Create(() => GenerateForRecord(factory, typeCache, propertyType)));
            }
            else if (propertyType.IsGenericType &&
                propertyType.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                propertyType.GetGenericArguments()[0] is { IsEnum: true } enumType)
            {
                var enumMembers = enumType.GetEnumNames()
                    .Select(x => factory.Create(() => new StringLiteralType(x)))
                    .Select(x => factory.GetReference(x))
                    .ToImmutableArray();

                typeReference = typeCache.GetOrAdd(propertyType, _ => factory.Create(() => new UnionType(enumMembers)));
            }
            else
            {
                throw new NotImplementedException($"Unsupported property type {propertyType}");
            }

            typeProperties[CamelCase(property.Name)] = new ObjectTypeProperty(
                factory.GetReference(typeReference),
                annotation?.Flags ?? ObjectTypePropertyFlags.None,
                annotation?.Description);
        }

        return new ObjectType(
            $"{type.Name}",
            typeProperties,
            null);
    }

    protected virtual ResourceType GenerateResource(TypeFactory typeFactory, ConcurrentDictionary<Type, TypeBase> typeCache, Type type)
        => typeFactory.Create(() => new ResourceType(
            name: $"{type.Name}",
            scopeType: ScopeType.Unknown,
            readOnlyScopes: null,
            body: typeFactory.GetReference(typeFactory.Create(() => GenerateForRecord(typeFactory, typeCache, type))),
            flags: ResourceFlags.None,
            functions: null));

    protected virtual string GetString(Action<Stream> streamWriteFunc)
    {
        using var memoryStream = new MemoryStream();
        streamWriteFunc(memoryStream);

        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }

    protected static string CamelCase(string input)
        => $"{input[..1].ToLowerInvariant()}{input[1..]}";
}
