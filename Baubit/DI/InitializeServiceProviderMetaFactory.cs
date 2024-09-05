using Baubit.Operation;
using Microsoft.Extensions.Configuration;

namespace Baubit.DI
{
    public class InitializeServiceProviderMetaFactory : IOperation<InitializeServiceProviderMetaFactory.Context, InitializeServiceProviderMetaFactory.Result>
    {
        public async Task<Result> RunAsync(Context context)
        {
            var serviceProviderMetaFactoryTypeAQN = context.Configuration["serviceProviderMetaFactory"];
            var serviceProviderMetaFactoryConcreteType = Type.GetType(serviceProviderMetaFactoryTypeAQN!);
            var serviceProviderMetaFactory = (IServiceProviderMetaFactory)Activator.CreateInstance(serviceProviderMetaFactoryConcreteType!)!;
            return new Result(true, serviceProviderMetaFactory);
        }

        public sealed class Context : IContext
        {
            public IConfiguration Configuration { get; init; }
            public Context(IConfiguration configuration)
            {
                Configuration = configuration;
            }
        }

        public sealed class Result : AResult<IServiceProviderMetaFactory>
        {
            public Result(Exception? exception) : base(exception)
            {
            }

            public Result(bool? success, IServiceProviderMetaFactory? value) : base(success, value)
            {
            }

            public Result(bool? success, string? failureMessage, object? failureSupplement) : base(success, failureMessage, failureSupplement)
            {
            }
        }
    }
}
