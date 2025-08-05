namespace Baubit.DI
{
    public interface IFeature
    {
        public IEnumerable<IModule> Modules { get; }
    }
}
