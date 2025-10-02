using Baubit.Caching.DI;
using Baubit.Configuration;
using Baubit.DI;
using Baubit.Serialization.MessagePack;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Net;

namespace Baubit.Caching.Redis.DI
{
    public class Module<TValue> : AModule<Configuration, TValue>
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
            services.AddSingleton<IConnectionMultiplexer, ConnectionMultiplexer>(serviceProvider => ConnectionMultiplexer.Connect(BuildRedisConfigurationOptions(serviceProvider)));
            services.AddSingleton<IServer>(serviceProvider => serviceProvider.GetRequiredService<IConnectionMultiplexer>().GetServer(Configuration.Host, Configuration.Port));
            services.AddSingleton<IDatabase>(serviceProvider => serviceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
            services.AddKeyedSingleton<IMetadata, Baubit.Caching.InMemory.Metadata>(Configuration.InternalMetadataDIKey);
            services.AddSingleton<ISerializer>(BuildMessagePackSerializer);
            base.Load(services);
        }

        private Serializer BuildMessagePackSerializer(IServiceProvider serviceProvider)
        {
            IFormatterResolver[] formatterResolvers =
            [
                Baubit.Serialization.MessagePack.MessagePackResolver.Instance,
                NativeGuidResolver.Instance, // optional: fastest Guid
                StandardResolver.Instance, // built-ins (Guid, DateTime, etc.)
            ];
            var options = MessagePackSerializerOptions.Standard.WithResolver(CompositeResolver.Create(formatterResolvers));
            return new Serializer(options);
        }

        private ConfigurationOptions BuildRedisConfigurationOptions(IServiceProvider serviceProvider)
        {
            var options = new ConfigurationOptions
            {
                EndPoints = [new IPEndPoint(IPAddress.Parse(Configuration.Host), Configuration.Port)],
                LoggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>()
            };
            return options;
        }

        protected override IStore<TValue> BuildL1DataStore(IServiceProvider serviceProvider)
        {
            return new Baubit.Caching.InMemory.Store<TValue>(Configuration.L1MinCap, Configuration.L1MaxCap, serviceProvider.GetRequiredService<ILoggerFactory>());
        }

        protected override IStore<TValue> BuildL2DataStore(IServiceProvider serviceProvider)
        {
            return new Store<TValue>(Configuration.L1MinCap,
                                     Configuration.L1MaxCap,
                                     serviceProvider.GetRequiredService<IDatabase>(),
                                     serviceProvider.GetRequiredService<IServer>(),
                                     serviceProvider.GetRequiredService<ISerializer>(),
                                     serviceProvider.GetRequiredService<ILoggerFactory>());
        }

        protected override IMetadata BuildMetadata(IServiceProvider serviceProvider)
        {
            return new Metadata(Configuration.SynchronizationOptions,
                                serviceProvider.GetRequiredKeyedService<IMetadata>(Configuration.InternalMetadataDIKey),
                                serviceProvider.GetRequiredService<IDatabase>(),
                                serviceProvider.GetRequiredService<ILoggerFactory>());
        }
    }
}
