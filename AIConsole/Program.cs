
using AIConsole;
using Baubit.DI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

await Host.CreateApplicationBuilder()
          .UseConfiguredServiceProviderFactory(withFeatures: () => [new DevFeature()])
          .Build()
          .RunAsync()
          .ConfigureAwait(false);