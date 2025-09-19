using Baubit.Caching.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Caching.InMemory.DI
{
    public record Configuration : AConfiguration
    {
        #region Variants
        /// <summary>
        /// Default configuration
        /// </summary>
        public static readonly Configuration C000 = new Configuration();

        /// <summary>
        /// <see cref="C000"/> with L1 caching; fixed size of 100
        /// </summary>
        public static readonly Configuration C001 = C000 with { IncludeL1Caching = true, L1MinCap = 100, L1MaxCap = 100 };

        /// <summary>
        /// <see cref="C001"/> with a transient service lifetime
        /// </summary>
        public static readonly Configuration C002 = C001 with { CacheLifetime = ServiceLifetime.Transient };

        /// <summary>
        /// <see cref="C001"/> with adaptive resizing enabled and max cap of 8K
        /// </summary>
        public static readonly Configuration C003 = C001 with { L1MinCap = 0, L1MaxCap = 8192, CacheConfiguration = new Caching.Configuration { RunAdaptiveResizing = true } };
        #endregion
    }
}
