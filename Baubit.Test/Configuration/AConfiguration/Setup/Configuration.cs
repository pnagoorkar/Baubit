using Baubit.Configuration;

namespace Baubit.Test.Configuration.AConfiguration.Setup
{
    public class Configuration : Baubit.Configuration.AConfiguration
    {
        [URI]
        public string CurrentEnvironment { get; init; }
    }
}
