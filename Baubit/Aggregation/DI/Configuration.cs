using Baubit.DI;

namespace Baubit.Aggregation.DI
{
    public record Configuration : AConfiguration
    {
        /// <summary>
        /// Default configuration
        /// </summary>
        public static readonly Configuration C000 = new Configuration();
    }
}
