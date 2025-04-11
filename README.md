# Baubit

[![CircleCI](https://dl.circleci.com/status-badge/img/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/main.svg?style=svg&circle-token=CCIPRJ_Laqns3C4sRXuApqb6m3r4s_1b81262a15527abad719fc5e0cfbf205e5cef624)](https://dl.circleci.com/status-badge/redirect/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/main)
[![NuGet](https://img.shields.io/nuget/v/Baubit.svg)](https://www.nuget.org/packages/Baubit)

## Introduction
**Baubit** is a modular framework for .NET applications that allows developers to build structured and scalable applications using independent modules. It simplifies dependency management and promotes reusability by enforcing a modular architecture.

## Why Use Baubit?
- 🚀 **Modular Architecture**: Define independent modules with their own configurations.
- 🔧 **Configuration Management**: Each module can have its own configuration, making applications more flexible.
- 🔗 **Seamless Integration**: Supports dependency injection using `IServiceCollection`.
- 📏 **Scalability & Maintainability**: Encourages a clean and structured application design.

---

## 🚀 Getting Started

### 1️⃣ Installation
```bash
dotnet add package Baubit
```
---

## 📌 How Baubit Works

Baubit is based on **modules** and **configuration**.

### 📦 Defining a Module
A Baubit **module** is a self-contained unit that adds one or more services to the application's IoC container.

```csharp
public class MyConfiguration : AConfiguration
{
    public string MyStringProperty { get; set; }
}

public class MyModule : AModule<MyConfiguration>
{
    public MyModule(ConfigurationSource configurationSource) : base(configurationSource) { }
    public MyModule(IConfiguration configuration) : base(configuration) { }
    public MyModule(MyConfiguration configuration, List<AModule> nestedModules) : base(configuration, nestedModules) { }

    public override void Load(IServiceCollection services)
    {
        var myStrProp = Configuration.MyStringProperty;
        services.AddSingleton(new MyService(myStrProp));
        //register other services as needed
        base.Load(services);
    }
}
```
Configuration for each registered service can be passed via the Module's specific configuration

---

## ⚙️ Loading a Module
Baubit supports multiple ways to load modules into your application. Below are the recommended and alternate approaches.

### 1. Via `appsettings.json` (**Recommended**)

**`appsettings.json` Example:**
```json
{
  "rootModule": [
    "type": "Baubit.DI.RootModule, Baubit",
    "configurationSource": {
      "embeddedJsonResources": [ "MyApp;myConfig.json" ]
    }
  ]
}
```

#### Using `HostApplicationBuilder`
```csharp
await Host.CreateApplicationBuilder()
          .UseConfiguredServiceProviderFactory()
          .Build()
          .RunAsync();
```

#### Using `WebApplicationBuilder`
```csharp
var webApp = WebApplication.CreateBuilder()
                           .UseConfiguredServiceProviderFactory()
                           .Build();

// Use HTTPS, HSTS, CORS, Auth and other middleware
// Map endpoints

await webApp.RunAsync();
```

---

### 2. Via `ConfigurationSource` (Programmatic Approach)

#### Using `HostApplicationBuilder`
```csharp
var configSource = new ConfigurationSource { EmbeddedJsonSources = ["MyApp;myConfig.json"] };
await Host.CreateApplicationBuilder()
          .UseConfiguredServiceProviderFactory(configSource.Build())
          .Build()
          .RunAsync();
```

#### Using `WebApplicationBuilder`
```csharp
var configSource = new ConfigurationSource { EmbeddedJsonSources = ["MyApp;myConfig.json"] };
var webApp = WebApplication.CreateBuilder()
                           .UseConfiguredServiceProviderFactory(configSource.Build())
                           .Build();

// Use HTTPS, HSTS, CORS, Auth and other middleware
// Map endpoints

await webApp.RunAsync();
```

---

### 3. Direct Loading (Without a Host Builder)
```csharp
var configSource = new ConfigurationSource { EmbeddedJsonSources = ["MyApp;myConfig.json"] };
var services = new ServiceCollection();
services.AddFrom(configSource); // Loads all modules (recursively) defined in myConfig.json
var serviceProvider = services.BuildServiceProvider();
```

---

## 🗂️ Configuration Sources
Baubit supports a variety of configuration input formats. You can also combine multiple sources.

### 1. `jsonUriStrings`
Loads JSON files from paths accessible to the application.

```json
{
  "modules": [
    {
        "type": "...",
        "configurationSource": {
          "jsonUriStrings": [ "/path/to/myConfig.json" ]
        }
    }
  ]
}
```

### 2. `embeddedJsonResources`
Loads JSON configuration embedded as a resource in a .NET assembly.

```json
{
  "modules": [
    {
        "type": "...",
        "configurationSource": {
          "embeddedJsonResources": [ "MyApp;MyComponent.SubComponent.myConfig.json" ]
        }
    }
  ]
}
```

### 3. `localSecrets`
Loads configuration from `secrets.json` files using a GUID reference (User Secrets ID).

```json
{
  "modules": [
    {
        "type": "...",
        "configurationSource": {
          "localSecrets": [ "0657aef1-6dc5-48f1-8ae4-172674291be0" ]
        }
    }
  ]
}
```
> This resolves to: `<user_secrets_path>/UserSecrets/{ID}/secrets.json`

---

### 🔗 Combining Multiple Sources
You can merge different configuration sources. Example:

```json
{
  "modules": [
    {
        "type": "...",
        "configurationSource": {
          "jsonUriStrings": [ "/path/to/myConfig.json" ],
          "embeddedJsonResources": [ "MyApp;MyComponent.SubComponent.myConfig.json" ],
          "localSecrets": [ "0657aef1-6dc5-48f1-8ae4-172674291be0" ]
        }
    }
  ]
}
```

---

### ➕ Combining Sources with Explicit Configuration
It’s also valid to define configuration values explicitly alongside configuration sources. The sources are merged with the explicit keys.

```json
{
  "modules": [
    {
        "type": "...",
        "configuration": {
          "myConfigurationProperty": "some value"
        },
        "configurationSource": {
          "jsonUriStrings": [ "/path/to/myConfig.json" ],
          "embeddedJsonResources": [ "MyApp;MyComponent.SubComponent.myConfig.json" ],
          "localSecrets": [ "0657aef1-6dc5-48f1-8ae4-172674291be0" ]
        }
    }
  ]
}
```

This will result in a configuration that combines values from all three sources plus the inline `configuration` block.

---
## 🪆 Nesting Modules
One of Baubit's most powerful features is its ability to **recursively load modules**, especially from configuration files. This enables complex service registration trees to be configured externally, promoting reusability and modularity.

### 🔁 Nested Configuration Example
```json
{
  "modules": [
    {
        "type": "<module1>",
        "configuration": {
          "<module1ConfigProperty>": "some value",
          "modules": [
            {
                "type": "<module2>",
                "configuration": {
                  "module2ConfigProperty": "some value"
                }
            },
            {
                "type": "<module3>",
                "configuration": {
                  "module3ConfigProperty": "some value",
                  "modules": [
                    {
                        "type": "<module4>",
                        "configuration": {
                          "module4ConfigProperty": "some value"
                        }
                    }
                  ]
                }
            }
          ]
        }
    }
  ]
}
```
This configuration will load **Module 1**, along with its nested modules **2**, **3**, and **4**, in a hierarchical manner. Each module can define its own configuration and optionally nest further modules.

> 🔧 This approach allows dynamic and flexible service registration — driven entirely by configuration without changing code.
---
## 📜 Roadmap
Future enhancements for Baubit:
- ✅ **Configuration Extensions**: Support for more configuration sources.
- ✅ **Middleware Support**: Integrate modules with ASP.NET middleware.
- 🚧 **Logging & Monitoring**: Improve logging support within modules.
- 🚧 **Community Contributions**: Open-source enhancements and community-driven improvements.

---

## 🤝 Contributing
Contributions are welcome! If you’d like to improve Baubit:
1. **Fork the repository**.
2. **Create a new branch** (`feature/new-feature`).
3. **Submit a pull request** with detailed changes.

For major contributions, open an issue first to discuss the change.

---

## 🛠 Troubleshooting & FAQs

### Q: How do I use multiple modules together?
A: You can initialize multiple modules and inject them into your service container.

### Q: Can I override module configurations?
A: Yes! You can extend configurations by passing custom settings to `ConfigurationSource`.

For more support, open an issue on GitHub.

---

## 📄 License
Baubit is licensed under the **Apache-2.0 License**. See the [LICENSE](LICENSE) file for details.

---

## 🔗 Resources
- Official Documentation (Coming Soon)
- Issue Tracker: [GitHub Issues](https://github.com/pnagoorkar/Baubit/issues)
- Discussions: [GitHub Discussions](https://github.com/pnagoorkar/Baubit/discussions)

---
## Acknowledgments & Inspiration

See [INSPIRATION.md](./INSPIRATION.md) for details on libraries and ideas that influenced this project.
