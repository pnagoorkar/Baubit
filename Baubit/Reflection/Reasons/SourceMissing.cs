using Baubit.Traceability.Reasons;

namespace Baubit.Reflection.Reasons
{
    public class SourceMissing<TSelfContained> : AReason where TSelfContained : class, ISelfContained
    {
        public SourceMissing() : base($"{typeof(TSelfContained).Name}{Environment.NewLine}The generic type parameter TSelfContained requires a {nameof(SourceAttribute)} to be instantiated", default)
        {
            
        }
    }
}
