using Baubit.Configuration;

namespace Baubit.Test.Configuration.AConfiguration.Setup
{
    public record Configuration : Baubit.Configuration.AConfiguration
    {
        [URI]
        public string CurrentEnvironment { get; init; }
    }
}
