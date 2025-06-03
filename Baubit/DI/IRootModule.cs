using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Baubit.DI
{
    public interface IRootModule : IModule
    {
        public Result<THostApplicationBuilder> UseConfiguredServiceProviderFactory<THostApplicationBuilder>(THostApplicationBuilder hostApplicationBuilder) where THostApplicationBuilder : IHostApplicationBuilder;
        public IServiceProvider BuildServiceProvider(IServiceCollection services);
    }

    public interface IRootModule<TContainerBuilder> : IRootModule where TContainerBuilder : notnull
    {
        public IServiceProviderFactory<TContainerBuilder> ServiceProviderFactory { get; }
    }
}
