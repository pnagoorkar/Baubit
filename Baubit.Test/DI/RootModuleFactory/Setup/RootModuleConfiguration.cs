using Baubit.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Test.DI.RootModuleFactory.Setup
{
    public record RootModuleConfiguration : ARootModuleConfiguration
    {
        public ServiceProviderOptions ServiceProviderOptions { get; init; } = new ServiceProviderOptions();
    }
}
