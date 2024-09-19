using Microsoft.Extensions.Hosting;

namespace Baubit.DI
{
    public interface IServiceProviderFactoryRegistrar
    {
        public IHostApplicationBuilder UseConfiguredServiceProviderFactory(IHostApplicationBuilder hostApplicationBuilder);
    }
}
