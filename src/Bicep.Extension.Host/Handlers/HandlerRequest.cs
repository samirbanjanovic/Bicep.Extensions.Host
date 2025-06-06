﻿using System.Text.Json.Nodes;

namespace Bicep.Extension.Host.Handlers;

public class HandlerRequest
{
    public HandlerRequest(string type, string apiVersion)
        : this(type, apiVersion, new(), new())
    { }

    public HandlerRequest(string type, string apiVersion, JsonObject? extensionSettings, JsonObject? resourceJson)
    {
        Type = string.IsNullOrWhiteSpace(type)
            ? throw new ArgumentNullException(nameof(type)) : type;

        ApiVersion = string.IsNullOrWhiteSpace(apiVersion)
            ? throw new ArgumentNullException(nameof(apiVersion)) : apiVersion;

        ExtensionSettings = extensionSettings;
        ResourceJson = resourceJson;
    }
    public string Type { get; }
    public string ApiVersion { get; }
    public JsonObject? ExtensionSettings { get; }
    public JsonObject? ResourceJson { get; }
}

public class HandlerRequest<TResource>
    : HandlerRequest
    where TResource : class
{
    public HandlerRequest(TResource resource, string apiVersion, JsonObject? extensionSettings, JsonObject? resourceJson)
        : base(resource.GetType().Name, apiVersion, extensionSettings, resourceJson)
    {
        Resource = resource ?? throw new ArgumentNullException(nameof(Resource));
    }

    public TResource Resource { get; }
}
