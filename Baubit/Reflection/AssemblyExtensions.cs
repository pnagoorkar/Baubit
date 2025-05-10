using Baubit.IO;
using FluentResults;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;

namespace Baubit.Reflection
{
    public static class AssemblyExtensions
    {
        public static AssemblyName GetAssemblyNameFromPersistableString(string value)
        {
            var nameParts = value.Split('/');
            return new AssemblyName { Name = nameParts[0], Version = new Version(nameParts[1]) };
        }

        public static Assembly? TryResolveAssembly(this AssemblyName assemblyName)
        {
            return AssemblyLoadContext.Default.Assemblies.FirstOrDefault(assembly => assembly.GetName().IsSameAs(assemblyName));
        }

        public static bool IsSameAs(this AssemblyName assemblyName, AssemblyName otherAssemblyName)
        {
            bool isNameEqual = otherAssemblyName.Name!.Equals(assemblyName.Name, StringComparison.OrdinalIgnoreCase);

            if (otherAssemblyName.Version == null) return isNameEqual;

            bool isVersionEqual = otherAssemblyName.Version!.Major == assemblyName.Version!.Major &&
                                  otherAssemblyName.Version!.Minor == assemblyName.Version!.Minor &&
                                  otherAssemblyName.Version!.Build == assemblyName.Version!.Build;

            if (otherAssemblyName.Version.Revision != -1 && assemblyName.Version.Revision != -1) // both assemblies have a Revision explicitly defined. We therefore check that they are also equal
            {
                isVersionEqual = isVersionEqual && otherAssemblyName.Version!.Revision == assemblyName.Version!.Revision;
            }

            return isNameEqual && isVersionEqual;
        }

        public static async Task<Result<string>> ReadResource(this Assembly assembly, string resourceName)
        {
            return await Result.Try(() => assembly.GetManifestResourceStream(resourceName))
                               .Bind(stream => stream!.ReadStringAsync());
        }

        public static Result<string> GetBaubitFormattedAssemblyQualifiedName(this Type type)
        {
            return Result.Try(() => type.AssemblyQualifiedName)
                         .Bind(assemblyQualifiedName => Result.Try(() => Regex.Replace(assemblyQualifiedName, @"(,\s*Version=[^,]+|,\s*Culture=[^,]+|,\s*PublicKeyToken=[^,\]]+(?=\]))", string.Empty)))
                         .Bind(assemblyQualifiedName => Result.Try(() => assemblyQualifiedName.Substring(0, assemblyQualifiedName.LastIndexOf(','))));
        }
    }
}
