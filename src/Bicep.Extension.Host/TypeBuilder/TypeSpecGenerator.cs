using Azure.Bicep.Types;
using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Index;
using Azure.Bicep.Types.Serialization;
using Bicep.Extension.Host.Handlers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;

namespace Bicep.Extension.Host.TypeBuilder;
public class TypeSpecGenerator
     : ITypeSpecGenerator
{
    private readonly HashSet<Type> visited;

    private readonly ImmutableDictionary<Type, Func<TypeBase>> typeToTypeBaseMap;

    protected readonly IImmutableDictionary<string, TypeResourceHandler>? resourceHandlers;

    protected readonly ConcurrentDictionary<Type, TypeBase> typeCache;
    protected readonly TypeFactory factory;

    public TypeSettings Settings { get; }

    public TypeSpecGenerator(TypeSettings typeSettings
                            , TypeFactory factory
                            , IResourceHandlerFactory resourceHandlerFactory
                            , ImmutableDictionary<Type, Func<TypeBase>> typeToTypeBaseMap)
    {
        if (typeSettings is null)
        {
            throw new ArgumentNullException(nameof(typeSettings));
        }

        this.typeToTypeBaseMap = typeToTypeBaseMap is null || typeToTypeBaseMap.Count < 1
                ? throw new ArgumentNullException(nameof(typeToTypeBaseMap))
                : typeToTypeBaseMap;

        this.visited = new HashSet<Type>();
        this.resourceHandlers = resourceHandlerFactory?.TypedResourceHandlers;

        this.typeCache = new ConcurrentDictionary<Type, TypeBase>();
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));

        Settings = typeSettings;
        this.typeToTypeBaseMap = typeToTypeBaseMap;
    }

    protected virtual Type[] GetResourceTypes()
    {
        var types = new Dictionary<string, Type>();

        if (resourceHandlers?.Count() > 0)
        {
            foreach (var resourceHandler in this.resourceHandlers)
            {
                types.TryAdd(resourceHandler.Key, resourceHandler.Value.Type);
            }
        }

        AppDomain
            .CurrentDomain
            .GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type =>
            {
                var bicepType = type.GetCustomAttributes(typeof(BicepTypeAttribute), true).FirstOrDefault();

                if (bicepType is not null)
                {
                    return ((BicepTypeAttribute)bicepType).IsActive;
                }

                return false;
            })
            .Select(type => types.TryAdd(type.Name, type))
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
            if (visited.Contains(property.PropertyType))
            {
                continue;
            }

            var annotation = property.GetCustomAttributes<TypeAnnotationAttribute>(true).FirstOrDefault();
            var propertyType = property.PropertyType;
            TypeBase typeReference;

            if (propertyType == typeof(string) && annotation?.IsSecure == true)
            {
                typeReference = factory.Create(() => new StringType(sensitive: true));
            }
            else if (typeToTypeBaseMap.TryGetValue(propertyType, out var typeFunc))
            {
                typeReference = typeCache.GetOrAdd(propertyType, _ => factory.Create(typeFunc));
            }
            else if (propertyType.IsClass)
            {
                // protect against infinite recursion
                visited.Add(property.PropertyType);

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
