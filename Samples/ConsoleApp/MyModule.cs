using Baubit.Configuration;
using Baubit.DI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ConsoleApp
{
    public class MyModule : AModule<MyConfiguration>
    {
        public MyModule(ConfigurationSource configurationSource) : base(configurationSource)
        {
        }

        public MyModule(IConfiguration configuration) : base(configuration)
        {
        }

        public MyModule(MyConfiguration configuration, List<AModule> nestedModules) : base(configuration, nestedModules)
        {
        }
        public override void Load(IServiceCollection services)
        {
            services.AddSingleton(serviceProvider => new MyComponent(Configuration.MyStringProperty, serviceProvider.GetRequiredService<ILogger<MyComponent>>()));
            services.AddHostedService<MyHostedService>();
            base.Load(services);
        }
    }
}
