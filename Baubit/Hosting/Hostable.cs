using Baubit.Configuration;
using Baubit.DI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using FluentResults;
using Baubit.Reflection;

namespace Baubit.Hosting
{
    public class Hostable
    {
        public string ServiceProviderFactoryRegistrarType { get; init; }
        public HostApplicationBuilderSettings HostApplicationBuilderSettings { get; init; }
        public ConfigurationSource AppConfiguration { get; init; }
        public IConfiguration? Configuration { get => AppConfiguration?.Load(); }

        public Hostable(HostApplicationBuilderSettings hostApplicationBuilderSettings)
        {
            HostApplicationBuilderSettings = hostApplicationBuilderSettings;
        }

        public async Task<Result> HostAsync()
        {
            try
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.None);
                var hostApplicationBuilder = Host.CreateEmptyApplicationBuilder(HostApplicationBuilderSettings);
                hostApplicationBuilder.Configuration.AddConfiguration(Configuration!);
                var resolveTypeResult = await TypeResolver.TryResolveTypeAsync(ServiceProviderFactoryRegistrarType!, cts.Token);
                if (!resolveTypeResult.IsSuccess) return Result.Fail("").WithReasons(resolveTypeResult.Reasons);
                var serviceProviderFactoryRegistrar = (IServiceProviderFactoryRegistrar)Activator.CreateInstance(resolveTypeResult.Value!)!;
                serviceProviderFactoryRegistrar.UseConfiguredServiceProviderFactory(hostApplicationBuilder);
                var host = hostApplicationBuilder.Build();
                await host.RunAsync();

                return Result.Ok();
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }
    }
}
