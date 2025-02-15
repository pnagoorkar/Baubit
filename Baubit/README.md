Baubit is a framework for developing modular apps in .NET

[![CircleCI](https://dl.circleci.com/status-badge/img/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/main.svg?style=svg&circle-token=CCIPRJ_Laqns3C4sRXuApqb6m3r4s_1b81262a15527abad719fc5e0cfbf205e5cef624)](https://dl.circleci.com/status-badge/redirect/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/main)

# Get started

Creating a module requires creating at least 2 classes - One for the module itself and another for the associated configuration

```csharp
public class MyConfiguration : AConfiguration
{
  public string MyStringProperty { get; set; }
}

public class MyModule : AModule<MyConfiguration>
{
  protected MyModule(ConfigurationSource configurationSource) : base(configurationSource)
  {

  }

  protected MyModule(IConfiguration configuration) : base(configuration)
  {

  }
  protected MyModule(MyConfiguration configuration,
                     List<AModule> nestedModules) : base(configuration, nestedModules)
  {
  }
  public override void Load(IServiceCollection services)
  {
    var myStrProp = Configuration.MyStringProperty;
    // add required services to the IServiceCollection based on the module's configuration
    base.Load(services);
  }
}
```
## Loading modules

myConfig.json

```json
{
  "modules": [
    {
      "type": "MyProject.MyModule, MyProject",
      "parameters": {
        "configuration": {
          "myStringProperty" : "some string value"
          }
        }
    }
  ]
}
```

### Using HostApplicationBuilder

```csharp
var hostApplicationBuilder = new HostApplicationBuilder();
hostApplicationBuilder.Configuration.AddJsonFile("myConfig.json");
new ServiceProviderFactoryRegistrar().UseDefaultServiceProviderFactory(hostApplicationBuilder);
var host = hostApplicationBuilder.Build();
host.Run();
```

### Using WebApplicationBuilder

```csharp
var webAppBuilder = WebApplication.CreateBuilder();
webAppBuilder.Configuration.AddJsonFile("myConfig.json");
new ServiceProviderFactoryRegistrar().UseDefaultServiceProviderFactory(webAppBuilder.Host);
var webApp = webAppBuilder.Build()
webApp.Run();
```

## Nesting Modules

Modules can be loaded side by side. <br>
They can also be loaded as a nested module (sub-module)

```csharp
public class AnotherModuleConfiguration : AConfiguration
{
  public string AnotherStringProperty { get; set; }
}

public class AnotherModule : AModule<AnotherModuleConfiguration>
{
  protected AnotherModule(ConfigurationSource configurationSource) : base(configurationSource)
  {

  }

  protected AnotherModule(IConfiguration configuration) : base(configuration)
  {

  }
  protected AnotherModule(AnotherModuleConfiguration configuration,
                          List<AModule> nestedModules) : base(configuration, nestedModules)
  {
  }
  public override void Load(IServiceCollection services)
  {
    var anotherStringProperty = Configuration.AnotherStringProperty;
    // add required services to the IServiceCollection based on the module's configuration
    base.Load(services);
  }
}
```

myConfig.json

```json
{
  "modules": [
    {
      "type": "MyProject.MyModule, MyProject",
      "parameters": {
        "configuration": {
          "myStringProperty" : "some string value",
          "modules": [
              {
                "type": "MyProject.AnotherModule, MyProject",
                "parameters": {
                  "configuration": {
                    "anotherStringProperty" : "another string value"
                    }
                  }
              }
            ]
          }
        }
    }
  ]
}
```
## Linking configurations
Module configurations can be loaded by referencing a json file<br>
myConfig.json
```json
{
  "modules": [
    {
      "type": "MyProject.MyModule, MyProject",
      "parameters": {
        "configuration": {
          "myStringProperty" : "some string value",
          "modules": [
              {
                "type": "MyProject.AnotherModule, MyProject",
                "parameters": {
                  "configurationSource": {
                    "jsonUriStrings" : ["anotherModule.json"]
                    }
                  }
              }
            ]
          }
        }
    }
  ]
}
```
anotherModule.json
```json
{
  "anotherStringProperty" : "another string value"
}
```

