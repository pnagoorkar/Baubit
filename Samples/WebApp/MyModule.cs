using Baubit.Configuration;
using Baubit.DI;
using Baubit.Logging.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp
{
    public class MyModule : AModule<MyConfiguration>
    {
        public MyModule(ConfigurationSource configurationSource) : base(configurationSource)
        {
        }

        public MyModule(IConfiguration configuration) : base(configuration)
        {
        }

        public MyModule(MyConfiguration configuration, List<AModule> nestedModules, List<IConstraint> constraints) : base(configuration, nestedModules, constraints)
        {
        }
        public override void Load(IServiceCollection services)
        {
            services.AddScoped(serviceProvider => new MyComponent(Configuration.MyStringProperty, serviceProvider.GetRequiredService<ActivityTracker>()));
            base.Load(services);
        }
    }
}
