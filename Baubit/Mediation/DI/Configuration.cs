using Baubit.DI;

namespace Baubit.Mediation.DI
{
    public record Configuration : AConfiguration
    {
        public static readonly Configuration C000 = new Configuration();
    }
}
