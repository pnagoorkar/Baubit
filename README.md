# Baubit

[![CircleCI](https://dl.circleci.com/status-badge/img/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/main.svg?style=svg&circle-token=CCIPRJ_Laqns3C4sRXuApqb6m3r4s_1b81262a15527abad719fc5e0cfbf205e5cef624)](https://dl.circleci.com/status-badge/redirect/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/main)

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

```
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

### 📁 Configuration Management
Baubit supports various ways to manage module configurations.

```csharp
var configSource = new ConfigurationSource { JsonUriStrings = ["myConfig.json"] };
var myModule = new MyModule(configSource);
```
- **File-based**: Load configurations from JSON, XML, or environment variables.
- **Code-based**: Manually define configurations in C#.

---

### 🏗 Dependency Injection & Services
Modules can register services with `IServiceCollection`:
```csharp
public override void Load(IServiceCollection services)
{
    services.AddTransient<IMyService, MyService>();
}
```
This enables modularized service registration, making dependency management cleaner.

---

## 🔍 Example Usage

### Bootstrapping the Application

#### Using HostApplicationBuilder

```csharp
var hostApplicationBuilder = new HostApplicationBuilder();
hostApplicationBuilder.Configuration.AddJsonFile("myConfig.json");
hostApplicationBuilder.UseConfiguredServiceProviderFactory();
var host = hostApplicationBuilder.Build();
host.Run();
```

#### Using WebApplicationBuilder

```csharp
var webAppBuilder = WebApplication.CreateBuilder();
webAppBuilder.Configuration.AddJsonFile("myConfig.json");
webAppBuilder.UseConfiguredServiceProviderFactory();
var webApp = webAppBuilder.Build()
webApp.Run();
```

myConfig.json

```json
{
  "modules": [
    {
      "type": "MyProject.MyModule, MyProject",
      "configuration": {
          "myStringProperty" : "some string value"
          }
     }
  ]
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

