using Baubit.Configuration;
using Baubit.DI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Test.DI.Setup
{
    public class Module : AModule<ModuleConfiguration>
    {
        public Module(ConfigurationSource configurationSource) : base(configurationSource)
        {
        }

        public Module(IConfiguration configuration) : base(configuration)
        {
        }

        public Module(ModuleConfiguration configuration, List<AModule> nestedModules) : base(configuration, nestedModules)
        {
        }

        public override void Load(IServiceCollection services)
        {
            services.AddSingleton(new Component(Configuration.SomeString, Configuration.SomeSecretString));
            base.Load(services);
        }
    }
}
