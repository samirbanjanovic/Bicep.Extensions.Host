# Bicep.Extensions.JumpStart

Bicep.Extensions.JumpStart is a lightweight wrapper designed to simplify the creation of Bicep Extensions. It provides a streamlined approach to building extensions by introducing dependency injection (DI) support for handlers, enabling developers to focus on their core logic without worrying about boilerplate code.

## Features

- **Lightweight and Simple**: A minimalistic framework to quickly bootstrap your Bicep Extensions.
- **Dependency Injection Support**: Easily configure and inject dependencies into your handlers using the built-in DI container.
- **Flexible Entry Point**: Provides a single entry method, `FlexAsync`, to initialize and run your extensions with ease.

## How It Works

The core functionality of this framework revolves around the `FlexAsync` method, which simplifies the process of defining and registering your services and handlers. Additionally, you can register services to further configure the backend Kestrel server that hosts the extension endpoint, allowing for greater flexibility and customization of your extension's runtime environment.

You can implement one of the following handler types:

- **`IResourceHandler`**: A handler designed to process a single resource type.
- **`IGenericResourceHandler`**: A more flexible handler capable of processing all resource definitions within a file.

```text
`IResourceHandler` is a specialized implementation of `IGenericResourceHandler`.
```

```text
By design you can only define one `IGenericResourceHandler` per extension
```

### Example Usage

Here’s an example of how to use `FlexAsync`. In this example we've implemented `OmniHandler` as a `IGenericResourceHandler` that has a dependency on some kind of `IBackendService`.  

```csharp
await FlexAsync(services =>
{
    // Register your handlers and services
    services.AddSingleton<OmniHandler>();
    services.AddSingleton<IBackendService, LocalOutputService>();
}, args);
