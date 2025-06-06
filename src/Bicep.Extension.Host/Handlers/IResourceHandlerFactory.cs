﻿using System.Collections.Immutable;

namespace Bicep.Extension.Host.Handlers;
public record TypeResourceHandler(Type Type, IResourceHandler Handler);

public interface IResourceHandlerFactory
{
    IImmutableDictionary<string, TypeResourceHandler>? TypedResourceHandlers { get; }
    TypeResourceHandler? GenericResourceHandler { get; }

    TypeResourceHandler? GetResourceHandler(string resourceType);
    TypeResourceHandler? GetResourceHandler(Type resourceType);
}