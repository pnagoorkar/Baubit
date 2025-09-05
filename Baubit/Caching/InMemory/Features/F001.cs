//using Baubit.Caching.InMemory.DI;
//using Baubit.DI;

//namespace Baubit.Caching.InMemory.Features
//{
//    public class F001<TValue> : IFeature
//    {
//        public IEnumerable<IModule> Modules =>
//        [
//            new Module<TValue>(new DI.Configuration{ IncludeL1Caching = true, L1MinCap = 0, L1MaxCap = 8192, CacheConfiguration = new Configuration{ RunAdaptiveResizing = true } }, [], [])
//        ];
//    }
//}
