using Baubit.DI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baubit.Caching.InMemory.Features
{
    public class F000<TValue> : IFeature
    {
        public IEnumerable<IModule> Modules =>
        [
            new Baubit.Caching.DI.Module<TValue>(new Baubit.Caching.DI.Configuration{CacheConfiguration = new Configuration{ L1StoreInitialCap = 100 } }, [], [])
        ];
    }
}
