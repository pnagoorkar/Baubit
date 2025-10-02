using Baubit.Configuration;
using Baubit.DI;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Serialization.MessagePack.DI
{
    public class Module : AModule<Configuration>
    {
        public Module(ConfigurationSource configurationSource) : base(configurationSource)
        {
        }

        public Module(IConfiguration configuration) : base(configuration)
        {
        }

        public Module(Configuration configuration, List<IModule> nestedModules, List<IConstraint> constraints) : base(configuration, nestedModules, constraints)
        {
        }

        public override void Load(IServiceCollection services)
        {
            var options = MessagePackSerializerOptions.Standard.WithResolver(CompositeResolver.Create(Configuration.FormatResolvers.ToArray()));
            services.AddSingleton(serviceProvider => options);
            services.AddSingleton<ISerializer, Serializer>();
        }
    }
}
