using Microsoft.Extensions.Hosting;

namespace Baubit.DI
{
    public interface IServiceProviderMetaFactory
    {
        public IHostBuilder UseConfiguredServiceProviderFactory(IHostBuilder hostBuilder);
        public IHostBuilder UseConfiguredServiceProviderFactory(IHostApplicationBuilder hostApplicationBuilder);
    }
}
