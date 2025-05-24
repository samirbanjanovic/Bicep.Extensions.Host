using System.Collections.Immutable;

namespace Bicep.Extension.Host.Handlers;
public record TypedHandlerMap(Type Type, IResourceHandler Handler);

public interface IResourceHandlerFactory
{
    IImmutableDictionary<string, TypedHandlerMap>? TypedResourceHandlers { get; }
    TypedHandlerMap? GenericResourceHandler { get; }

    TypedHandlerMap? GetResourceHandler(string resourceType);
    TypedHandlerMap? GetResourceHandler(Type resourceType);
}