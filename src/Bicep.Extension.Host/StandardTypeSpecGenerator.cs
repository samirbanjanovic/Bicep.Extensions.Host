using Azure.Bicep.Types;
using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Index;
using Azure.Bicep.Types.Serialization;
using Bicep.Extension.Host;
using Bicep.Host.Types;
using Microsoft.AspNetCore.Identity;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;

namespace Bicep.Host.Types
{
    public class TypeConfiguration
    {
        private readonly IImmutableDictionary<string, ObjectTypeProperty> configuration;

        public TypeConfiguration(IDictionary<string, ObjectTypeProperty>? configuration)
        {
            this.configuration = configuration?.Any() == true ?
                configuration.ToImmutableDictionary() : ImmutableDictionary.Create<string, ObjectTypeProperty>();
        }

        public IImmutableDictionary<string, ObjectTypeProperty> Value => configuration;

    }
    public record ExtensionSpec(string Name, string Version);

    public class StandardTypeSpecGenerator : ITypeSpecGenerator
    {
        private readonly TypeSettings settings;
        private readonly ImmutableArray<ITypedResourceHandler> typedResourceHandlers;

        private readonly ConcurrentDictionary<Type, TypeBase> typeCache;
        private readonly TypeFactory factory;

        public StandardTypeSpecGenerator(ExtensionSpec extensionSpec
                                , TypeFactory factory
                                , TypeConfiguration typeConfiguration
                                , IEnumerable<ITypedResourceHandler> typedResourceHandlers)
        {
            if(extensionSpec is null || (string.IsNullOrEmpty(extensionSpec.Name) || string.IsNullOrEmpty(extensionSpec.Version)))
            {
                throw new ArgumentException($"Invalid extension spec {extensionSpec}. Name and Version must have valid values");
            }

            this.typedResourceHandlers = typedResourceHandlers?.Any() == true ?
                typedResourceHandlers.ToImmutableArray() : throw new ArgumentNullException($"No handlers have been registered");

            this.typeCache = new ConcurrentDictionary<Type, TypeBase>();
            this.factory = factory ?? throw new ArgumentNullException($"{nameof(factory)} cannot be null");

            var configurationType = factory.Create(() => 
                new ObjectType("configuration", 
                    typeConfiguration?.Value ?? throw new ArgumentNullException($"{nameof(typeConfiguration)} cannot be null.")
                , null));

            this.settings = new TypeSettings(
                  name: extensionSpec.Name
                , version: extensionSpec.Version
                , isSingleton: true
                , configurationType: new Azure.Bicep.Types.CrossFileTypeReference("types.json", factory.GetIndex(configurationType)));
        }

        public TypeSpec GenerateBicepResourceTypes()
        {
            var resourceTypes = GetResourceTypes(this.typedResourceHandlers)
                                    .Select(rt => GenerateResource(factory, typeCache, rt))
                                    .ToDictionary(rt => rt.Name, rt => new CrossFileTypeReference("types.json", factory.GetIndex(rt)));

            var index = new TypeIndex(resourceTypes
                        , new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<CrossFileTypeReference>>>()
                        , this.settings
                        , null);

            return new(GetString(stream => TypeSerializer.SerializeIndex(stream, index))
                      , GetString(stream => TypeSerializer.Serialize(stream, factory.GetTypes())));
        }

        private static Type[] GetResourceTypes(IEnumerable<ITypedResourceHandler> typedResourceHandlers)
        {
            var types = new List<Type>();
            foreach (var resourceHandler in typedResourceHandlers)
            {
                if(resourceHandler.GetType().TryGetStronglyTypedResourceHandler(out var resourceHandlerInterface))
                {
                    var genericType = resourceHandlerInterface.GetGenericArguments()[0];
                    types.Add(genericType);
                }
            }
            return types.ToArray();
        }

        private static string CamelCase(string input)
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


        private ResourceType GenerateResource(TypeFactory typeFactory, ConcurrentDictionary<Type, TypeBase> typeCache, Type type)
            => typeFactory.Create(() => new ResourceType(
                name: $"frp/{type.Name}",
                scopeType: ScopeType.Unknown,
                readOnlyScopes: null,
                body: typeFactory.GetReference(typeFactory.Create(() => GenerateForRecord(typeFactory, typeCache, type))),
                flags: ResourceFlags.None,
                functions: null));

        private string GetString(Action<Stream> streamWriteFunc)
        {
            using var memoryStream = new MemoryStream();
            streamWriteFunc(memoryStream);

            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
    }
}
