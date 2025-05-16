# Bicep.Extension.Host

The `Bicep.Extension.Host` project introduces a new, modernized approach for building Bicep extensions by fully embracing .NET conventions for Dependency Injection (DI) and service registration. You work directly with standard .NET patterns, making your extensions easier to build, test, and maintain.

## What's New

- **.NET Standard Patterns**: Extensions are now built using the familiar `WebApplicationBuilder` and DI container patterns.
- **Service Registration**: Handlers and services are registered using standard `IServiceCollection` methods.
- **Sample Implementation**: See [`Bicep.Extension.Sample`](src/Bicep.Extension.Sample/Program.cs) for a complete example.

## gRPC Server for Bicep Communication

A local extension consists of the following components:

- A binary executable which exposes the Bicep Extensibility Protocol over a [gRPC](https://grpc.io/) connection. This allows you to model interactions with your custom resource types. The gRPC contract is defined [here](https://github.com/Azure/bicep/blob/main/src/Bicep.Local.Extension/extension.proto).
- Type metadata stored in a structured JSON format. This allows Bicep to understand your custom resource types for editor validation and code completion. You can use packages defined in [bicep-types](https://github.com/Azure/bicep-types) to define and generate this structured format for your own custom resource types.

### Required Command-Line Arguments

All extension binaries are expected to accept the following CLI arguments:

- `-s | --socket <socet_-name>`: The path to the domain socket to connect on.
- `-p | --pipe <pipe-name>`: The named pipe to connect on.
- `-t | --http <port-number>`: Launch service in HTTP mode binding to specific port.  Default port is 5000.
- `-w | --wait-for-debugger`: Signals that you want to debug the extension, and that execution should pause until you are ready.

Once started (either via domain socket or named pipe), the extension:

- Exposes a gRPC endpoint over the relevant channel, adhering to the extension gRPC contract.
- Responds to `SIGTERM` to request a graceful shutdown.

## Handler Types

You can implement one of the following handler types:

- **`IResourceHandler`**: A handler designed to process a single resource type.
- **`IGenericResourceHandler`**: A more flexible handler capable of processing all resource definitions within a file.

> **Note:**  
> `IResourceHandler` is a specialized implementation of `IGenericResourceHandler`.

> **Important:**  
> By design, you can only define one `IGenericResourceHandler` per extension.

## Example Usage

Below is a minimal example from [`Bicep.Extension.Sample/Program.cs`](src/Bicep.Extension.Sample/Program.cs):

```csharp
public class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebApplication
                            .CreateBuilder()
                            .AddBicepExtensionHost(args);

        builder.Services
               .AddBicepServices()
               .AddBicepGenericResourceHandler<OmniHandler>()
               .AddSingleton<IBackendService, LocalOutputService>();

        var app = builder.Build();
        app.UseBicepDispatcher();

        await app.RunAsync();
    }
}
```

- Register your handlers and services using the DI container.
- Use `.AddBicepGenericResourceHandler<T>()` for your generic handler, or `.AddBicepResourceHandler<T>()` for a specific resource handler.
- Build and run your app as you would with any ASP.NET Core application.

For more details, see the sample implementation in [src/Bicep.Extension.Sample/Program.cs](src/Bicep.Extension.Sample/Program.cs).