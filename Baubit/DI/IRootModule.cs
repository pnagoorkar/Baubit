using FluentResults;
using Microsoft.Extensions.Hosting;

namespace Baubit.DI
{
    public interface IRootModule : IModule
    {
        public Result<THostApplicationBuilder> UseConfiguredServiceProviderFactory<THostApplicationBuilder>(THostApplicationBuilder hostApplicationBuilder) where THostApplicationBuilder : IHostApplicationBuilder;
    }
}
