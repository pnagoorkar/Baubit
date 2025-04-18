
using Baubit.Configuration;

namespace Baubit.Test.Configuration.AConfiguration
{
    public class Configuration : Baubit.Configuration.AConfiguration
    {
        [URI]
        public string CurrentEnvironment { get; init; }
    }
}
