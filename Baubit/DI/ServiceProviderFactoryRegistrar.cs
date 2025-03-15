using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Baubit.DI
{
    public sealed class ServiceProviderFactoryRegistrar : IServiceProviderFactoryRegistrar
    {
        private readonly RootModule _rootModule;
        public ServiceProviderFactoryRegistrar(IConfiguration configuration)
        {
            _rootModule = new RootModule(configuration);
        }
        public THostApplicationBuilder UseConfiguredServiceProviderFactory<THostApplicationBuilder>(THostApplicationBuilder hostApplicationBuilder) where THostApplicationBuilder : IHostApplicationBuilder
        {
            hostApplicationBuilder.ConfigureContainer(new DefaultServiceProviderFactory(), _rootModule.Load);
            return hostApplicationBuilder;
        }
    }
}
