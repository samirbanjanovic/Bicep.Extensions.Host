using Azure.Bicep.Types;
using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Index;
using Azure.Bicep.Types.Serialization;
using Bicep.Host.Types;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;

namespace Bicep.Host.Types
{
    public record ExtensionSpec(string Name, string Version);

    public class TypeSpecGenerator
    {
        private readonly TypeSettings settings;
        private readonly Dictionary<string, ObjectTypeProperty> configuration;
        private readonly ConcurrentDictionary<Type, TypeBase> typeCache;
        private readonly TypeFactory factory;

        public TypeSpecGenerator(ExtensionSpec extensionSpec, TypeFactory factory, Dictionary<string, ObjectTypeProperty>? configuration = null)
        {

            this.configuration = configuration ?? new Dictionary<string, ObjectTypeProperty>();
            this.typeCache = new ConcurrentDictionary<Type, TypeBase>();
            this.factory = factory;

            var configurationType = factory.Create(() => new ObjectType("configuration",
                this.configuration
                , null
            ));

            settings = new TypeSettings(
                               name: extensionSpec.Name,
                               version: extensionSpec.Version,
                               isSingleton: true,
                               configurationType: new Azure.Bicep.Types.CrossFileTypeReference("types.json", factory.GetIndex(configurationType)));
        }

        public Dictionary<string, string> GenerateTypes()
        {

            // fetch all resource types defined in models directory
            var resourceTypes = typeof(TypeSpecGenerator)
                            .Assembly
                            .GetTypes()
                            .Where(t => t.GetCustomAttribute(typeof(ResourceTypeAnnotationAttribute)) is not null)
                            .Select(rt => GenerateResource(factory, typeCache, rt))
                            .ToDictionary(rt => rt.Name, rt => new CrossFileTypeReference("types.json", factory.GetIndex(rt)));

            var index = new TypeIndex(resourceTypes
                        , new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<CrossFileTypeReference>>>()
                        , this.settings
                        , null);

            return new Dictionary<string, string>
            {
                ["index.json"] = GetString(stream => TypeSerializer.SerializeIndex(stream, index)),
                ["types.json"] = GetString(stream => TypeSerializer.Serialize(stream, factory.GetTypes())),
            };
        }

        public static string CamelCase(string input)
            => $"{input[..1].ToLowerInvariant()}{input[1..]}";

        private TypeBase GenerateForRecord(TypeFactory factory, ConcurrentDictionary<Type, TypeBase> typeCache, Type type)
        {
            var typeProperties = new Dictionary<string, ObjectTypeProperty>();
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
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
                $"frp/{type.Name}",
                typeProperties,
                null);
        }


        public ResourceType GenerateResource(TypeFactory typeFactory, ConcurrentDictionary<Type, TypeBase> typeCache, Type type)
            => typeFactory.Create(() => new ResourceType(
                name: $"frp/{type.Name}",
                scopeType: ScopeType.Unknown,
                readOnlyScopes: null,
                body: typeFactory.GetReference(typeFactory.Create(() => GenerateForRecord(typeFactory, typeCache, type))),
                flags: ResourceFlags.None,
                functions: null));

        public string GetString(Action<Stream> streamWriteFunc)
        {
            using var memoryStream = new MemoryStream();
            streamWriteFunc(memoryStream);

            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
    }
}
