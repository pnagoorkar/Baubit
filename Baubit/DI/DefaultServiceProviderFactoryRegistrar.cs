using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Baubit.DI
{
    public sealed class DefaultServiceProviderFactoryRegistrar : IServiceProviderFactoryRegistrar
    {
        private RootModule _rootModule;
        public IHostApplicationBuilder UseConfiguredServiceProviderFactory(IHostApplicationBuilder hostApplicationBuilder)
        {
            if (_rootModule == null) _rootModule = new RootModule(hostApplicationBuilder.Configuration);
            hostApplicationBuilder.ConfigureContainer(new DefaultServiceProviderFactory(), _rootModule.Load);
            return hostApplicationBuilder;
        }
    }
}
