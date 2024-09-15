using Baubit.Configuration;
using Baubit.DI;
using Baubit.Operation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Baubit.Hosting
{
    public class HostApplication : IOperation<HostApplication.Context, HostApplication.Result>
    {
        private HostApplication()
        {

        }
        private static HostApplication _singletonInstance = new HostApplication();
        public static HostApplication GetInstance()
        {
            return _singletonInstance;
        }

        public async Task<Result> RunAsync(Context context)
        {
            try
            {
                var hostApplicationBuilder = Host.CreateEmptyApplicationBuilder(context.HostApplicationBuilderSettings);
                hostApplicationBuilder.Configuration.AddConfiguration(context.Configuration!);
                var serviceProviderMetaFactoryConcreteType = Type.GetType(context.ServiceProviderMetaFactoryType!);
                var serviceProviderMetaFactory = (IServiceProviderMetaFactory)Activator.CreateInstance(serviceProviderMetaFactoryConcreteType!)!;
                serviceProviderMetaFactory.UseConfiguredServiceProviderFactory(hostApplicationBuilder);
                var host = hostApplicationBuilder.Build();
                await host.RunAsync();

                return new Result(true, null);
            }
            catch (Exception exp)
            {
                return new Result(exp);
            }
        }

        public sealed class Context : IContext
        {
            public string ServiceProviderMetaFactoryType { get; init; }
            public HostApplicationBuilderSettings HostApplicationBuilderSettings { get; init; }
            public MetaConfiguration AppConfiguration { get; init; }
            public IConfiguration? Configuration { get => AppConfiguration?.Load(); }

            public Context(HostApplicationBuilderSettings hostApplicationBuilderSettings)
            {
                HostApplicationBuilderSettings = hostApplicationBuilderSettings;
            }
        }

        public sealed class Result : AResult
        {
            public Result(Exception? exception) : base(exception)
            {
            }

            public Result(bool? success, object? value) : base(success, value)
            {
            }

            public Result(bool? success, string? failureMessage, object? failureSupplement) : base(success, failureMessage, failureSupplement)
            {
            }
        }
    }
}
