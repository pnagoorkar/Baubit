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
            services.AddSingleton<ISerializer>(BuildMessagePackSerializer);
        }

        private Serializer BuildMessagePackSerializer(IServiceProvider serviceProvider)
        {
            IFormatterResolver[] formatterResolvers =
            [
                Baubit.Serialization.MessagePack.MessagePackResolver.Instance, // default - required for generated resolvers
                NativeGuidResolver.Instance, // optional: fastest Guid
                StandardResolver.Instance, // built-ins (Guid, DateTime, etc.)
            ];

            var options = MessagePackSerializerOptions.Standard.WithResolver(CompositeResolver.Create(formatterResolvers.ToArray()));
            return new Serializer(options);
        }
    }
}
