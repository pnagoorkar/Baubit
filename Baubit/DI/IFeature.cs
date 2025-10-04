using Baubit.Configuration;
using Microsoft.Extensions.Configuration;

namespace Baubit.DI
{
    public interface IFeature
    {
        public IEnumerable<IModule> Modules { get; }
    }

    public record FeatureDescriptor(string Function, string Variant);
}

