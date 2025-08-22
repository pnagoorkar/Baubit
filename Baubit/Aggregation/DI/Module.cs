using Baubit.Caching;
using Baubit.Configuration;
using Baubit.DI;
using Baubit.Observation;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Baubit.Aggregation.DI
{
    public class Module<T> : AModule<Configuration>
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
            services.AddSingleton(BuildAggregator);
            services.AddSingleton<IPublisher<T>>(serviceProvider => serviceProvider.GetRequiredService<Aggregator<T>>());
            services.AddSingleton<SubscriptionFactory<T>>(BuildSubscriptionFactory);
        }

        private Aggregator<T> BuildAggregator(IServiceProvider serviceProvider)
        {
            return new Aggregator<T>(serviceProvider.GetRequiredService<IOrderedCache<T>>(),
                                     serviceProvider.GetRequiredService<SubscriptionFactory<T>>(),
                                     serviceProvider.GetRequiredService<ILoggerFactory>());
        }

        private SubscriptionFactory<T> BuildSubscriptionFactory(IServiceProvider serviceProvider)
        {
            return (subscriber, postDeliveryHandler, disposeHandler) => BuildSubscription(subscriber, postDeliveryHandler, disposeHandler, serviceProvider);
        }

        private Subscription<T> BuildSubscription(ISubscriber<T> subscriber, 
                                                  Func<Subscription<T>, long, Result> postDeliveryHandler, 
                                                  Func<Subscription<T>, Result> disposeHandler, 
                                                  IServiceProvider serviceProvider)
        {
            return new Subscription<T>(subscriber, 
                                       postDeliveryHandler, 
                                       disposeHandler,
                                       serviceProvider.GetRequiredService<IOrderedCache<T>>(),
                                       serviceProvider.GetRequiredService<IOrderedCache<long>>(),
                                       serviceProvider.GetRequiredService<ILoggerFactory>());
        }
    }
}
