using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Caching.Storage.DI
{
    public abstract class AConfiguration : Baubit.DI.AConfiguration
    {
        public string DIKey { get; init; }
    }
}
