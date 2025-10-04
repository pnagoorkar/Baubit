using Baubit.Caching.DI;

namespace Baubit.Caching.Redis.DI
{
    public record Configuration : AConfiguration
    {
        public SynchronizationOptions SynchronizationOptions { get; init; }
        public string Host { get; init; }
        public int Port { get; init; }
        public string InternalMetadataDIKey { get; init; } = "InMemory_Metadata";
    }
}
