using Baubit.Configuration;
using Baubit.DI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using FluentResults;

namespace Baubit.Hosting
{
    public static partial class Operations
    {
        public static async Task<Result> HostApplicationAsync(ApplicationHostingContext context)
        {
            try
            {
                var hostApplicationBuilder = Host.CreateEmptyApplicationBuilder(context.HostApplicationBuilderSettings);
                hostApplicationBuilder.Configuration.AddConfiguration(context.Configuration!);
                var resolveTypeResult = await Baubit.Store.Operations.ResolveTypeAsync(new Store.TypeResolutionContext(context.ServiceProviderFactoryRegistrarType!));
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

    public class ApplicationHostingContext
    {
        public string ServiceProviderFactoryRegistrarType { get; init; }
        public HostApplicationBuilderSettings HostApplicationBuilderSettings { get; init; }
        public MetaConfiguration AppConfiguration { get; init; }
        public IConfiguration? Configuration { get => AppConfiguration?.Load(); }

        public ApplicationHostingContext(HostApplicationBuilderSettings hostApplicationBuilderSettings)
        {
            HostApplicationBuilderSettings = hostApplicationBuilderSettings;
        }
    }
}
