# Bicep.Extensions.JumpStart

Bicep.Extensions.JumpStart is a lightweight wrapper designed to simplify the creation of Bicep Extensions. It provides a streamlined approach to building extensions by introducing dependency injection (DI) support for handlers, enabling developers to focus on their core logic without worrying about boilerplate code.

## Features

- **Lightweight and Simple**: A minimalistic framework to quickly bootstrap your Bicep Extensions.
- **Dependency Injection Support**: Easily configure and inject dependencies into your handlers using the built-in DI container.
- **Flexible Entry Point**: Provides a single entry method, `FlexAsync`, to initialize and run your extensions with ease.

## How It Works

The core functionality revolves around the `FlexAsync` method, which allows you to define and register your services and handlers.
You can write a single handler per resource type by implementing the `IResourceHandler` interface. Or, you can write a generic handler that will handle
all resource defintions within a file by implementing the `IGenericResourceHandler` interface.

> Note: `IResourceHandler` is a child of `IGenericResourceHandler`

> Important: The DI bag is configured to only resolve a single `IGenericResourceHandler` per extension.

### Example Usage

Here’s an example of how to use `FlexAsync`. In this example we've implemented `OmniHandler` as a `IGenericResourceHandler` that has a dependency on some kind of `IBackendService`.  

Upon start all services are resolved and the extension is running and listening for requests.

```csharp
await FlexAsync(services =>
{
    // Register your handlers and services
    services.AddSingleton<OmniHandler>();
    services.AddSingleton<IBackendService, LocalOutputService>();
}, args);
