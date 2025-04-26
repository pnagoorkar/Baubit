using Baubit.Traceability.Reasons;

namespace Baubit.Reflection.Reasons
{
    public class IncompatibleTypes : AReason
    {
        public Type Type { get; init; }
        public Type ConcreteType { get; init; }
        public IncompatibleTypes(Type type, Type concreteType)
        {
            Type = type;
            ConcreteType = concreteType;
        }
    }
}
