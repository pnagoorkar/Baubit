using Baubit.Traceability;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Baubit.DI
{
    public static class HostBuilderExtensions
    {
        public static THostApplicationBuilder UseConfiguredServiceProviderFactory<THostApplicationBuilder>(this THostApplicationBuilder hostApplicationBuilder,
                                                                                                           IConfiguration configuration = null,
                                                                                                           Func<IEnumerable<IFeature>> withFeatures = null,
                                                                                                           Action<THostApplicationBuilder, IResultBase> onFailure = null) where THostApplicationBuilder : IHostApplicationBuilder
        {
            if (onFailure == null) onFailure = Exit;
            if (configuration != null) hostApplicationBuilder.Configuration.AddConfiguration(configuration);

            var registrationResult = RootModuleFactory.Create(hostApplicationBuilder.Configuration, withFeatures?.Invoke()?.ToArray() ?? [])
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

        public static IServiceCollection AddBaubit(this IServiceCollection services,
                                                   IConfiguration configuration,
                                                   params IFeature[] features)
        {
            features.SelectMany(feature => feature.Modules)
                    .SerializeAsJsonObject(default)
                    .Bind(modules => Baubit.Configuration.ConfigurationBuilder.CreateNew()
                                           .Bind(configurationBuilder => configurationBuilder.WithAdditionalConfigurations(configuration))
                                           .Bind(configurationBuilder => configurationBuilder.WithRawJsonStrings(modules))
                                           .Bind(configurationBuilder => configurationBuilder.Build()))
                    .Bind(config => RootModuleFactory.Create(config))
                    .Bind(rootModule => Result.Try(() => rootModule.Load(services)))
                    .ThrowIfFailed();
            return services;
        }
    }
}
