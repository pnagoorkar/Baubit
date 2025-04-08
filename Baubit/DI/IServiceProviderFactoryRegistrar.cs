//using Baubit.Traceability.Errors;
//using FluentResults;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Hosting;

//namespace Baubit.DI
//{
//    public interface IServiceProviderFactoryRegistrar
//    {
//        public Result<THostApplicationBuilder> UseConfiguredServiceProviderFactory<THostApplicationBuilder>(THostApplicationBuilder hostApplicationBuilder) where THostApplicationBuilder : IHostApplicationBuilder;
//    }

//    public static class HostBuilderExtensions
//    {
//        public static THostApplicationBuilder UseConfiguredServiceProviderFactory<THostApplicationBuilder>(this THostApplicationBuilder hostApplicationBuilder,
//                                                                                                           IConfiguration configuration = null,
//                                                                                                           Action<THostApplicationBuilder, IError> onFailure = null) where THostApplicationBuilder : IHostApplicationBuilder
//        {
//            if (onFailure == null) onFailure = Exit;
//            if (configuration != null) hostApplicationBuilder.Configuration.AddConfiguration(configuration);

//            var registrationResult = hostApplicationBuilder.Configuration
//                                                           .GetServiceProviderFactorySection()
//                                                           .Bind(section => section.TryAs<IServiceProviderFactoryRegistrar>())
//                                                           .Bind(registrar => registrar.UseConfiguredServiceProviderFactory(hostApplicationBuilder));

//            if (!registrationResult.IsSuccess)
//            {
//                var error = new CompositeError<THostApplicationBuilder>(registrationResult);
//                onFailure(hostApplicationBuilder, error);
//            }

//            return hostApplicationBuilder;
//        }

//        private static void Exit<THostApplicationBuilder>(THostApplicationBuilder hostApplicationBuilder,
//                                                          IError error) where THostApplicationBuilder : IHostApplicationBuilder
//        {
//            Console.WriteLine(error);
//            Environment.Exit(-1);
//        }
//    }
//}
