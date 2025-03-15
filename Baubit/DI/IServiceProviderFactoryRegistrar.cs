using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Baubit.DI
{
    public interface IServiceProviderFactoryRegistrar
    {
        public THostApplicationBuilder UseConfiguredServiceProviderFactory<THostApplicationBuilder>(THostApplicationBuilder hostApplicationBuilder) where THostApplicationBuilder : IHostApplicationBuilder;
    }

    public static class HostBuilderExtensions
    {
        public static THostApplicationBuilder UseConfiguredServiceProviderFactory<THostApplicationBuilder>(this THostApplicationBuilder hostApplicationBuilder) where THostApplicationBuilder : IHostApplicationBuilder
        {
            var serviceProviderFactorySection = hostApplicationBuilder.Configuration
                                                                      .GetSection("serviceProviderFactory");

            if(serviceProviderFactorySection.Exists())
            {
                var serviceProviderFactoryRegistrar = serviceProviderFactorySection.As<IServiceProviderFactoryRegistrar>();
                return serviceProviderFactoryRegistrar.UseConfiguredServiceProviderFactory(hostApplicationBuilder);
            }

            return hostApplicationBuilder;
        }
    }
}
