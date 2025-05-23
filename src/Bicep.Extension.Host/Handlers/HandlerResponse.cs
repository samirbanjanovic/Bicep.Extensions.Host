using System.Text.Json.Nodes;

namespace Bicep.Extension.Host.Handlers;
public enum HandlerResponseStatus
{
    Succeeded,
    Failed,
    Canceled,
    TimedOut
}

public record Error(string Code, string Target, string Message);

public class HandlerResponse
{
    public HandlerResponse(string type, string version, HandlerResponseStatus status, JsonObject properties)
        : this(type, version, status, properties, null)
    { }

    public HandlerResponse(string type, string version, HandlerResponseStatus status, JsonObject properties, Error? error, string? message = null)
    {
        Type = type;
        Version = version;
        Status = status;
        Properties = properties;
        Error = error;
        Message = message;
    }

    public string Type { get; }
    public string Version { get; }
    public HandlerResponseStatus Status { get; }
    public JsonObject Properties { get; }
    public JsonObject? ExtensionSettings { get; }
    public Error? Error { get; }
    public string? Message { get; }

    public static HandlerResponse Success(string resourceType, string apiVersion, JsonObject properties, string? message = null)
        => new HandlerResponse(resourceType, apiVersion, HandlerResponseStatus.Succeeded, properties, null, message: message);

    public static HandlerResponse Failed(string resourceType, string apiVersion, JsonObject properties, Error? errors, string? message = null)
        => new HandlerResponse(resourceType, apiVersion, HandlerResponseStatus.Failed, properties, errors, message: message);

    public static HandlerResponse Canceled(string resourceType, string apiVersion, JsonObject properties, string? message = null)
        => new HandlerResponse(resourceType, apiVersion, HandlerResponseStatus.Canceled, properties, null, message: message);

    public static HandlerResponse TimedOut(string resourceType, string apiVersion, JsonObject properties, string? message = null)
        => new HandlerResponse(resourceType, apiVersion, HandlerResponseStatus.TimedOut, properties, null, message: message);
}
