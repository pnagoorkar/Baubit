using FluentResults;
using Microsoft.Extensions.Configuration;

namespace Baubit.DI
{
    public static partial class Operations
    {
        public static async Task<Result<IServiceProviderFactoryRegistrar>> InitializeServiceProviderFactoryRegistrarAsync(ServiceProviderFactoryRegistrarInitializationContext context)
        {
            var serviceProviderMetaFactoryTypeAQN = context.Configuration["serviceProviderFactoryRegistrar"];
            var serviceProviderMetaFactoryConcreteType = Type.GetType(serviceProviderMetaFactoryTypeAQN!);
            var serviceProviderMetaFactory = (IServiceProviderFactoryRegistrar)Activator.CreateInstance(serviceProviderMetaFactoryConcreteType!)!;
            return Result.Ok(serviceProviderMetaFactory);
        }
    }

    public class ServiceProviderFactoryRegistrarInitializationContext
    {
        public IConfiguration Configuration { get; init; }
        public ServiceProviderFactoryRegistrarInitializationContext(IConfiguration configuration)
        {
            Configuration = configuration;
        }
    }
}
