using Microsoft.Extensions.Hosting;

namespace Baubit.DI
{
    public interface IServiceProviderMetaFactory
    {
        public IHostApplicationBuilder UseConfiguredServiceProviderFactory(IHostApplicationBuilder hostApplicationBuilder);
    }
}
