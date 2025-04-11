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
### ⚙️ Loading a module
Baubit supports various ways to load modules.

1) via appsettings.json (*Recommended*)

```json
{
  "rootModule": {
    "type": "Baubit.DI.RootModule, Baubit",
    "configurationSource": {
      "embeddedJsonResources": [ "MyApp;myConfig.json" ]
    }
  }
}
```

##### Using HostApplicationBuilder

```csharp
await Host.CreateApplicationBuilder()
          .UseConfiguredServiceProviderFactory()
          .Build()
          .RunAsync();
```

##### Using WebApplicationBuilder

```csharp
var webApp = WebApplication.CreateBuilder()
                           .UseConfiguredServiceProviderFactory()
                           .Build();

// use HTTPS, HSTS, CORS, Auth and other middleware
// map endpoints

await webApp.RunAsync();
```
2) via ConfigurationSource

##### Using HostApplicationBuilder

```csharp
var configSource = new ConfigurationSource { EmbeddedJsonSources = ["MyApp;myConfig.json"] };
await Host.CreateApplicationBuilder()
          .UseConfiguredServiceProviderFactory(configSource.Build())
          .Build()
          .RunAsync();
```

##### Using WebApplicationBuilder

```csharp
var configSource = new ConfigurationSource { EmbeddedJsonSources = ["MyApp;myConfig.json"] };
var webApp = WebApplication.CreateBuilder()
                           .UseConfiguredServiceProviderFactory(configSource.Build())
                           .Build();

// use HTTPS, HSTS, CORS, Auth and other middleware
// map endpoints

await webApp.RunAsync();
```
3) Direct loading (not using IHostApplicationBuilder)

```csharp
var configSource = new ConfigurationSource { EmbeddedJsonSources = ["MyApp;myConfig.json"] };
var services = new ServiceCollection();
services.AddFrom(configSource);//Loads all modules (recursively) defined in myConfig.json
var serviceProvider = services.BuildServiceProvider();
```

---
### 🗂️ Configuration Sources

Configurations can currently loaded from the following sources:

1) JsonUriStrings - Json files on paths accessible to the application.
Example:
```json
{
  "modules": {
    "type": "...",
    "configurationSource": {
      "jsonUriStrings": [ "<fully_qualified_path>/myConfig.json" ] // os (win/lin) independent path format
    }
  }
}
```
2) EmbeddedJsonResources - Json files packaged into a dll as Embedded Resources
Example:
```json
{
  "modules": {
    "type": "...",
    "configurationSource": {
      "embeddedJsonResources": [ "<assembly_name>;<directory>/<another_directory>.myConfig.json" ] //ex: "MyApp;MyComponent.SubComponent.myConfig.json"
    }
  }
}
```
3) LocalSecrets - Ids to secrets.json files on the local file system
Example:
```json
{
  "modules": {
    "type": "...",
    "configurationSource": {
      "localSecrets": [ "0657aef1-6dc5-48f1-8ae4-172674291be0" ] //loads secrets.json at <user_secrets_path>/UserSecrets/0657aef1-6dc5-48f1-8ae4-172674291be0/secrets.json - win/lin compatible
    }
  }
}
```
Multiple sources can be defined to load a single configuration. The below is a valid definition for configurationSource:
```json
{
  "modules": {
    "type": "...",
    "configurationSource": {
      "jsonUriStrings": [ "<fully_qualified_path>/myConfig.json" ]
      "embeddedJsonResources": [ "<assembly_name>;<directory>/<another_directory>.myConfig.json" ]
      "localSecrets": [ "0657aef1-6dc5-48f1-8ae4-172674291be0" ] 
    }
  }
}
```
The above will result in a configuration built by combining all 3 sources.

Configuration keys can also be defined (in addition to a defining a configurationSource) explicitly. The below json will load a single configuration by combining the 3 configuration sources with the explicitly defined configuration
```json
{
  "modules": {
    "type": "...",
    "configuration": {
      "myConfigurationProperty": "some value"
    },
    "configurationSource": {
      "jsonUriStrings": [ "<fully_qualified_path>/myConfig.json" ]
      "embeddedJsonResources": [ "<assembly_name>;<directory>/<another_directory>.myConfig.json" ]
      "localSecrets": [ "0657aef1-6dc5-48f1-8ae4-172674291be0" ] 
    }
  }
}
```
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
