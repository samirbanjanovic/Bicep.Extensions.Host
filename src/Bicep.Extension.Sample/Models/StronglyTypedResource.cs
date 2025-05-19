using Azure.Bicep.Types.Concrete;
using Bicep.Host.Types;

namespace Bicep.Extension.Sample.Models;

public class StronglyTypedResource
{
    public StronglyTypedResource() { }

    [TypeAnnotation(description: "Resource Name", flags: ObjectTypePropertyFlags.Required)]
    public string? Name { get; set; }

    [TypeAnnotation(description: "Action type. Options consist of Post and Fetch.")]
    public Action? ActionType { get; set; }

    public Parameters? Properties { get; set; }
}

public enum Action
{
    Post,
    Fetch
}

public class Parameters
{
    [TypeAnnotation(description: "Supported Region")]
    public string? Region { get; set; }

    [TypeAnnotation(description: "Subscription id")]
    public string? Subscriptionid { get; set; }

    [TypeAnnotation(description: "Service Endpoint")]
    public string? EndpointName { get; set; }
}