using Baubit.DI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var hostAppBuilder = new HostApplicationBuilder();
hostAppBuilder.Configuration.AddJsonFile("myConfig.json");
hostAppBuilder.UseConfiguredServiceProviderFactory();

var host = hostAppBuilder.Build();
await host.RunAsync();