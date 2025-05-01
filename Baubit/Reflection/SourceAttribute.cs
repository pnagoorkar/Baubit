using Baubit.Configuration;
using FluentResults;

namespace Baubit.Reflection
{

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class SourceAttribute : Attribute
    {
        public string[] JsonUriStrings { get; init; } = [];
        public string[] EmbeddedJsonResources { get; init; } = [];
    }

    public static class SourceAttributeExtensions
    {
        public static Result<ConfigurationSource> GetConfigSourceFromSourceAttribute(this SourceAttribute sourceAttribute)
        {
            return ConfigurationSourceBuilder.CreateNew()
                                             .Bind(configSourceBuilder => configSourceBuilder.WithJsonUriStrings(sourceAttribute.JsonUriStrings))
                                             .Bind(configSourceBuilder => configSourceBuilder.WithEmbeddedJsonResources(sourceAttribute.EmbeddedJsonResources))
                                             .Bind(configSourceBuilder => configSourceBuilder.Build());
        }
    }
}
