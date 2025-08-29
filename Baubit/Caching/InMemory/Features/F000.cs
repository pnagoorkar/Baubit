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
            new Baubit.Caching.InMemory.DI.Module<TValue>(new Baubit.Caching.InMemory.DI.Configuration{ IncludeL1Caching = true, L1MinCap = 100, L1MaxCap = 100 }, [], [])
        ];
    }
}
