using Baubit.Reflection;
using Baubit.Traceability;
using System.Text.Json.Serialization;

namespace Baubit.DI
{
    public abstract class AConfiguration : Configuration.AConfiguration
    {
        public List<string> ModuleValidatorKeys { get; init; } = new List<string>();

        private List<Type> moduleValidatorTypes;

        [JsonIgnore]
        public List<Type> ModuleValidatorTypes
        {
            get
            {
                if (moduleValidatorTypes == null)
                {
                    moduleValidatorTypes = ModuleValidatorKeys.Select(key => TypeResolver.TryResolveType(key).ThrowIfFailed().Value).ToList();
                }
                return moduleValidatorTypes;
            }
        }
    }
}
