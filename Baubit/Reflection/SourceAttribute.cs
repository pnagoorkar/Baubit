
using Baubit.Configuration;

namespace Baubit.Reflection
{

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class SourceAttribute : Attribute
    {
        private ConfigurationSource configSource;
        internal ConfigurationSource ConfigurationSource
        {
            get
            {
                if (configSource == null)
                {
                    configSource = new ConfigurationSource { JsonUriStrings = [.. JsonUriStrings], EmbeddedJsonResources = [.. EmbeddedJsonResources] };
                }
                return configSource;
            }
        }
        public string[] JsonUriStrings { get; init; } = [];
        public string[] EmbeddedJsonResources { get; init; } = [];
    }
}
