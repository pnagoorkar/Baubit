# Baubit

[![CircleCI](https://dl.circleci.com/status-badge/img/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/main.svg?style=svg&circle-token=CCIPRJ_Laqns3C4sRXuApqb6m3r4s_1b81262a15527abad719fc5e0cfbf205e5cef624)](https://dl.circleci.com/status-badge/redirect/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/main)
[![NuGet](https://img.shields.io/nuget/v/Baubit.svg)](https://www.nuget.org/packages/Baubit)

## Introduction
**Baubit** is a lightweight, modular framework for building scalable and maintainable .NET applications. It provides a clean abstraction for organizing functionality into independently configured modules, supporting recursive loading, dependency injection, and multiple configuration sources.

## Why Use Baubit?
- **🧩 Modular Architecture**: Encapsulate related functionality in self-contained units (modules).
- **🗂️ Configuration Management**: Modules support their own typed configuration with support for JSON, embedded resources, and secrets.
- **⚙️ Clean DI Integration**: Each module registers services into `IServiceCollection`, respecting lifecycle and separation of concerns.
- **🔁 Recursive Nesting**: Modules can declare and load other modules as dependencies.
- **📦 Configurable Bootstrapping**: Load and configure modules via JSON, appsettings, code, or embedded resources.
- **🧪 Testability & Reusability**: Modules are isolated and easily testable.
- **🛡️ Validatable**: Not only are modules and configurations validated (in isolation) before loding, modules are checked against the module tree to avoid redundant / missing modules. 



## 🚀 Getting Started

### 1️⃣ Installation
```bash
dotnet add package Baubit
```

## 📦 Defining a Module
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
    public MyModule(MyConfiguration configuration, 
                    List<AModule> nestedModules, 
                    List<IConstraint> constraints) : base(configuration, nestedModules, constraints) { }

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


## ⚙️ Loading a Module
Baubit supports multiple ways to load modules:

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



### 2. Via `ConfigurationSource` (Direct Code-Based)

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



### 3. Manual DI (Without a Host Builder)
```csharp
var configSource = new ConfigurationSource { EmbeddedJsonSources = ["MyApp;myConfig.json"] };
var services = new ServiceCollection();
services.AddFrom(configSource); // Loads all modules (recursively) defined in myConfig.json
var serviceProvider = services.BuildServiceProvider();
```

> This approach is particularly useful when used in unit tests. See [Baubit.xUnit](https://github.com/pnagoorkar/Baubit.xUnit) for developing modular unit tests using Baubit


## 🗂️ Configuration Sources
Baubit supports a mix of external and embedded configuration options:

#### ✅ Supported Sources
- **`jsonUriStrings`**: Local or remote JSON paths
- **`embeddedJsonResources`**: Embedded resources within assemblies
- **`localSecrets`**: User secrets via GUID ID

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
> All sources are merged in order.



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
> 
## ✅ Validation
Baubit introduces a powerful validation mechanism to ensure the integrity of your module configurations and their interdependencies.
### Configuration Validation
Create your configuration
```cs
public class Configuration : AConfiguration
{
    public string MyStringProperty { get; init;}
}
```
Implement the AValidator class to define validation logic for configuration.
```cs
public class MyConfigurationValidator : AValidator<Configuration>
{
    protected override IEnumerable<Expression<Func<Configuration, Result>>> GetRules()
    {
        return [config => Result.OkIf(!string.IsNullOrEmpty(config.MyStringProperty), new Error($"{nameof(config.MyStringProperty)} cannot be null or empty")];
    }
}
```
Multiple validators can be defined via the validatorKeys configuration property. Validators for modules can also be defined in the similar fashion
```json
{
  "modules": [
    {
        "type": "...",
        "configuration": {
          "validatorKeys": [ "MyLib.MyConfigurationValidator, MyLib" ],
          "moduleValidatorKeys": [ "MyLib.MyModuleValidator, MyLib" ]
          //"myStringProperty" : "" //<not_defined_on_purpose>
        }
    }
  ]
}
```

### Module Validation

While modules can also be validated in isolation (similar to shown above), Baubit allows defining constraints under which modules can/cannot be included in a module tree

```cs
public class SingularityConstraint<TModule> : IConstraint
{
    public string ReadableName => string.Empty;

    public Result Check(List<IModule> modules)
    {
        return modules.Count(mod => mod is TModule) == 1 ? Result.Ok() : Result.Fail(string.Empty).AddReasonIfFailed(new SingularityCheckFailed());
    }
}
public class SingularityCheckFailed : AReason
{

}
```
A module can then simply use these constraints to valide the module tree

```cs
public class Module : AModule<Configuration>
{
    public Module(ConfigurationSource configurationSource) : base(configurationSource)
    {
    }

    public Module(IConfiguration configuration) : base(configuration)
    {
    }

    public Module(Configuration configuration, 
                  List<Baubit.DI.AModule> nestedModules, 
                  List<IConstraint> constraints) : base(configuration, nestedModules, constraints)
    {
    }

    protected override IEnumerable<IConstraint> GetKnownConstraints()
    {
        return [new SingularityConstraint<Module>()];
    }
}
```
This allows preventing redundant service registrations and checking module dependencies at bootstrapping 

## 📜 Roadmap
Future enhancements for Baubit:
- ✅ **Configuration Extensions**: Support for more configuration sources.
- ✅ **Middleware Support**: Integrate modules with ASP.NET middleware.
- 🚧 **Logging & Monitoring**: Improve logging support within modules.
- 🚧 **Community Contributions**: Open-source enhancements and community-driven improvements.




## 🤝 Contributing
Contributions are welcome! If you’d like to improve Baubit:
1. **Fork the repository**.
2. **Create a new branch** (`feature/new-feature`).
3. **Submit a pull request** with detailed changes.

For major contributions, open an issue first to discuss the change.




## 🛠 Troubleshooting & FAQs

### Q: How do I use multiple modules together?
A: You can initialize multiple modules and inject them into your service container.

### Q: Can I override module configurations?
A: Yes! You can extend configurations by passing custom settings to `ConfigurationSource`.

For more support, open an issue on GitHub.




## 🔗 Resources
- [Samples](https://github.com/pnagoorkar/Baubit/tree/master/Samples)
- Official Documentation (Coming Soon)
- Issue Tracker: [GitHub Issues](https://github.com/pnagoorkar/Baubit/issues)
- Discussions: [GitHub Discussions](https://github.com/pnagoorkar/Baubit/discussions)

---
## Acknowledgments & Inspiration

See [ACKNOWLEDGEMENT.md](./ACKNOWLEDGEMENT.md) and [INSPIRATION.md](./INSPIRATION.md) for details on libraries and ideas that influenced this project.


---
## Copyright
Copyright (c) Prashant Nagoorkar. See [LICENSE](LICENSE) for details.
