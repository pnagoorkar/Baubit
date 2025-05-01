using Baubit.DI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Baubit.Logging.DI
{
    public abstract class AModule<TConfiguration> : Baubit.DI.AModule<TConfiguration> where TConfiguration : AConfiguration
    {
        protected AModule(Baubit.Configuration.ConfigurationSource configurationSource) : base(configurationSource)
        {
        }

        protected AModule(IConfiguration configuration) : base(configuration)
        {
        }

        protected AModule(TConfiguration configuration, List<Baubit.DI.AModule> nestedModules, List<IConstraint> constraints) : base(configuration, nestedModules, constraints)
        {
        }

        public override void Load(IServiceCollection services)
        {
            services.AddLogging(loggingBuilder => { loggingBuilder.ClearProviders(); ConfigureLogging(loggingBuilder); });
            base.Load(services);
        }

        protected virtual void ConfigureLogging(ILoggingBuilder loggingBuilder)
        {
            if (Configuration.AddConsole)
            {
                loggingBuilder.AddConsole();
            }
            if (Configuration.AddDebug)
            {
                loggingBuilder.AddDebug();
            }
            if (Configuration.AddEventSource)
            {
                loggingBuilder.AddEventSourceLogger();
            }
            if (Configuration.AddEventLog)
            {
                loggingBuilder.AddEventLog();
            }
        }
    }
}
