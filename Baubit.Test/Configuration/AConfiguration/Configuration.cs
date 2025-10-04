
using Baubit.Configuration;

namespace Baubit.Test.Configuration.AConfiguration
{
    public record Configuration : Baubit.Configuration.AConfiguration
    {
        [URI]
        public string CurrentEnvironment { get; init; }
    }
}
