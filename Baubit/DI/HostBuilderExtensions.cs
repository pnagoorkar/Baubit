using Baubit.Configuration;
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
                                                                                                           Action<THostApplicationBuilder, IResultBase> onFailure = null) where THostApplicationBuilder : IHostApplicationBuilder
        {
            if (onFailure == null) onFailure = Exit;
            if (configuration != null) hostApplicationBuilder.Configuration.AddConfiguration(configuration);

            var registrationResult = RootModuleFactory.Create(hostApplicationBuilder.Configuration)
                                                      .Bind(registrar => registrar.UseConfiguredServiceProviderFactory(hostApplicationBuilder));

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
        /// <summary>
        /// Loads all modules defined by <paramref name="embeddedJsonResources"/>
        /// </summary>
        /// <param name="services">Your custom service collection</param>
        /// <param name="embeddedJsonResources">An array of json resources</param>
        /// <returns><see cref="Result"/></returns>
        public static Result AddFromEmbeddedJsonResources(this IServiceCollection services, params string[] embeddedJsonResources)
        {
            return ConfigurationSourceBuilder.CreateNew()
                                      .Bind(configSourceBuilder => configSourceBuilder.WithEmbeddedJsonResources(embeddedJsonResources))
                                      .Bind(configSourceBuilder => configSourceBuilder.Build())
                                      .Bind(configSource => ComponentBuilder<object>.Create(configSource))
                                      .Bind(componentBuilder => componentBuilder.WithServiceCollection(services))
                                      .Bind(componentBuilder => componentBuilder.Build(false))
                                      .Bind(_ => Result.Ok());
        }
        /// <summary>
        /// Loads all modules defined by <paramref name="rawJsonStrings"/>
        /// </summary>
        /// <param name="services">Your custom service collection</param>
        /// <param name="rawJsonStrings">An array of json configurations</param>
        /// <returns><see cref="Result"/></returns>
        public static Result AddFromRawJsonStrings(this IServiceCollection services, params string[] rawJsonStrings)
        {
            return ConfigurationSourceBuilder.CreateNew()
                                      .Bind(configSourceBuilder => configSourceBuilder.WithRawJsonStrings(rawJsonStrings))
                                      .Bind(configSourceBuilder => configSourceBuilder.Build())
                                      .Bind(configSource => ComponentBuilder<object>.Create(configSource))
                                      .Bind(componentBuilder => componentBuilder.WithServiceCollection(services))
                                      .Bind(componentBuilder => componentBuilder.Build(false))
                                      .Bind(_ => Result.Ok());
        }
    }
}
