using Baubit.DI;
using Microsoft.Extensions.Hosting;

await Host.CreateApplicationBuilder()
          .UseConfiguredServiceProviderFactory()
          .Build()
          .RunAsync();