using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Baubit.DI
{
    public interface IServiceProviderFactoryRegistrar
    {
        public IHostApplicationBuilder UseConfiguredServiceProviderFactory(IHostApplicationBuilder hostApplicationBuilder);
    }

    public static class HostBuilderExtensions
    {
        public static IHostApplicationBuilder UseConfiguredServiceProviderFactory(this IHostApplicationBuilder hostApplicationBuilder)
        {
            var serviceProviderFactorySection = hostApplicationBuilder.Configuration
                                                                      .GetSection("serviceProviderFactory");

            var serviceProviderFactoryRegistrar = serviceProviderFactorySection.Exists() ? 
                                                  serviceProviderFactorySection.As<IServiceProviderFactoryRegistrar>() : 
                                                  new DefaultServiceProviderFactoryRegistrar();

            return serviceProviderFactoryRegistrar.UseConfiguredServiceProviderFactory(hostApplicationBuilder);
        }
    }
}
