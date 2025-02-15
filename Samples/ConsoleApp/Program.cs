using Baubit.DI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var hostAppBuilder = new HostApplicationBuilder();
hostAppBuilder.Configuration.AddJsonFile("myConfig.json");
new ServiceProviderFactoryRegistrar().UseDefaultServiceProviderFactory(hostAppBuilder);

var host = hostAppBuilder.Build();
await host.RunAsync();