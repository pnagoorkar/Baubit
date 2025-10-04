using Microsoft.Extensions.DependencyInjection;

namespace Baubit.DI
{
    public sealed record RootModuleConfiguration : ARootModuleConfiguration
    {
        public ServiceProviderOptions ServiceProviderOptions { get; init; } = new ServiceProviderOptions();
    }
}
