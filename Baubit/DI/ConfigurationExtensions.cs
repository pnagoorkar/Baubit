using Baubit.Configuration;
using Baubit.Reflection;
using Baubit.Traceability.Errors;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.DI
{
    public static class ConfigurationExtensions
    {
        public static Result<IServiceProvider> Load(this IConfiguration configuration)
        {
            return Result.Try(() => new ServiceCollection())
                         .Bind(services => services.AddFrom(configuration))
                         .Bind(services => Result.Try<IServiceProvider>(() => services.BuildServiceProvider()));
        }
        public static Result<IServiceCollection> AddFrom(this IServiceCollection services, IConfiguration configuration)
        {
            return Result.Try(() => new RootModule(configuration))
                         .Bind(rootModule => Result.Try(() => rootModule.Load(services)))
                         .Bind(() => Result.Ok(services));
        }
        public static Result<IServiceCollection> AddFrom(this IServiceCollection services, ConfigurationSource configurationSource) =>  configurationSource.Build().Bind(services.AddFrom);

        public static Result<List<TModule>> GetNestedModules<TModule>(this IConfiguration configuration) where TModule : IModule
        {
            List<TModule> directlyDefinedModules = new List<TModule>();
            List<TModule> indirectlyDefinedModules = new List<TModule>();

            var directlyDefinedModulesExtractionResult = configuration.GetModulesSectionOrDefault()
                                                                      .Bind(modulesSection => Result.Try(() => modulesSection?.GetChildren() ?? new List<IConfigurationSection>()))
                                                                      .Bind(sections => Result.Merge(sections.Select(section => section.TryAs<TModule>()).ToArray()))
                                                                      .Bind(modules => { directlyDefinedModules = modules.ToList(); return Result.Ok(); });

            var indirectlyDefinedModulesExtractionResult = configuration.GetModuleSourcesSectionOrDefault()
                                                                        .Bind(moduleSourceSection => Result.Try(() => moduleSourceSection?.GetChildren() ?? new List<IConfigurationSection>()))
                                                                        .Bind(configSections => Result.Merge(configSections.Select(section => section.Get<ConfigurationSource>().Build()).ToArray()))
                                                                        .Bind(configs => Result.Merge(configs.Select(config => config.GetNestedModules<TModule>()).ToArray()))
                                                                        .Bind(modules => { indirectlyDefinedModules = modules.SelectMany(x => x).ToList(); return Result.Ok(); });

            return directlyDefinedModulesExtractionResult.IsSuccess && indirectlyDefinedModulesExtractionResult.IsSuccess ?
                   Result.Ok<List<TModule>>([.. directlyDefinedModules, .. indirectlyDefinedModules]) :
                   Result.Fail(new CompositeError<IEnumerable<TModule>>(directlyDefinedModulesExtractionResult, indirectlyDefinedModulesExtractionResult));
        }
    }
}
