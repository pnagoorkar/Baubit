using Microsoft.Extensions.DependencyInjection;

namespace Baubit.DI
{
    public sealed class RootModuleConfiguration : ARootModuleConfiguration
    {
        public ServiceProviderOptions ServiceProviderOptions { get; init; } = new ServiceProviderOptions();
    }
}
