//using Baubit.Caching.InMemory.DI;
//using Baubit.DI;
//using Microsoft.Extensions.DependencyInjection;

//namespace Baubit.Caching.InMemory.Features
//{
//    public class F002<TValue> : IFeature
//    {
//        public IEnumerable<IModule> Modules =>
//        [
//            new Module<TValue>(new DI.Configuration { IncludeL1Caching = true, L1MinCap = 100, L1MaxCap = 100, CacheLifetime = ServiceLifetime.Transient }, [], [])
//        ];
//    }
//}
