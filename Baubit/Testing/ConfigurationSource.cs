using Baubit.Configuration;
using Baubit.Reflection;
using FluentResults;

namespace Baubit.Testing
{
    public class ConfigurationSource<TSelfContained> : ConfigurationSource where TSelfContained : ISelfContained
    {
    }

    public static class ConfigurationSourceExtensions
    {
        public static Result<TSelfContained> Load<TSelfContained>(this ConfigurationSource<TSelfContained> configurationSource) where TSelfContained : class, ISelfContained
        {
            return ObjectLoader.Load<TSelfContained>(configurationSource);
        }
        public static Result<TSelfContained> Load<TSelfContained>(this ConfigurationSource<TSelfContained> configurationSource, string assemblyQualifiedName) where TSelfContained : class, ISelfContained
        {
            return ObjectLoader.Load<TSelfContained>(configurationSource, assemblyQualifiedName);
        }
    }
}
