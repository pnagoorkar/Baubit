//using Baubit.DI;
//using Baubit.Operation;
//using Microsoft.Extensions.Hosting;

//namespace Baubit.Hosting
//{
//    public sealed class BuildHost : IOperation<BuildHost.Context, BuildHost.Result>
//    {
//        private BuildHost()
//        {

//        }
//        private static BuildHost _singletonInstance = new BuildHost();
//        public static BuildHost GetInstance()
//        {
//            return _singletonInstance;
//        }
//        public async Task<Result> RunAsync(Context context)
//        {
//            var hostBuilder = Host.CreateDefaultBuilder(context.Args);

//            IServiceProviderMetaFactory serviceProviderMetaFactory = default;
//            serviceProviderMetaFactory.UseConfiguredServiceProviderFactory(hostBuilder);

//            return new Result(true, hostBuilder.Build());
//        }

//        public sealed class Context : IContext
//        {
//            public string[] Args { get; set; }
//            public string HostConfigurationJson { get; set; }
//            public Context(string[] args)
//            {
//                Args = args;
//            }
//        }

//        public sealed class Result : AResult<IHost>
//        {
//            public Result(Exception? exception) : base(exception)
//            {
//            }

//            public Result(bool? success, IHost? value) : base(success, value)
//            {
//            }

//            public Result(bool? success, string? failureMessage, object? failureSupplement) : base(success, failureMessage, failureSupplement)
//            {
//            }
//        }
//    }
//}
