using Baubit.Traceability.Reasons;

namespace Baubit.Reflection.Reasons
{
    public sealed class TypeNotDefined : AReason
    {
        public TypeNotDefined(string assemblyQualifiedName) : base($"Undefined type: {assemblyQualifiedName}", default)
        {
        }
    }
}
