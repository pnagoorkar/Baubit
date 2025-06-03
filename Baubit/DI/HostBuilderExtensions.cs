using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Baubit.DI
{
    public static class HostBuilderExtensions
    {
        public static THostApplicationBuilder UseConfiguredServiceProviderFactory<THostApplicationBuilder>(this THostApplicationBuilder hostApplicationBuilder,
                                                                                                           IConfiguration configuration = null,
                                                                                                           Action<THostApplicationBuilder, IResultBase> onFailure = null) where THostApplicationBuilder : IHostApplicationBuilder
        {
            if (onFailure == null) onFailure = Exit;
            if (configuration != null) hostApplicationBuilder.Configuration.AddConfiguration(configuration);

            var registrationResult = RootModuleFactory.Create(hostApplicationBuilder.Configuration)
                                                      .Bind(rootModule => rootModule.UseConfiguredServiceProviderFactory(hostApplicationBuilder));

            if (registrationResult.IsFailed)
            {
                onFailure(hostApplicationBuilder, registrationResult);
            }

            return hostApplicationBuilder;
        }

        private static void Exit<THostApplicationBuilder>(THostApplicationBuilder hostApplicationBuilder,
                                                          IResultBase result) where THostApplicationBuilder : IHostApplicationBuilder
        {
            Console.WriteLine(result.ToString());
            Environment.Exit(-1);
        }
    }
}
