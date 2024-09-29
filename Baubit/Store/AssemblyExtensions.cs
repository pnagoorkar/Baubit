using Baubit.IO;
using FluentResults;
using System.Reflection;

namespace Baubit.Store
{
    public static class AssemblyExtensions
    {
        public static async Task<Result<Package>> DetermineDownloadablePackagesAsync(this AssemblyName assemblyName, string targetFramework)
        {
            return await new MockProject(assemblyName, targetFramework).BuildAsync();
        }
        public static string GetPersistableAssemblyName(this AssemblyName assemblyName)
        {
            return $"{assemblyName.Name}/{assemblyName.Version}";
        }

        public static string GetPackageRootPath(this AssemblyName assemblyName)
        {
            return Path.Combine(Application.BaubitRootPath, assemblyName.Name!, assemblyName.Version!.ToString()!);
        }

        public static AssemblyName GetAssemblyNameFromPersistableString(string value)
        {
            var nameParts = value.Split('/');
            return new AssemblyName { Name = nameParts[0], Version = new Version(nameParts[1]) };
        }

        public static async Task<Result<string>> ReadResource(this Assembly assembly, string resourceName)
        {
            return await Result.Try(() => assembly.GetManifestResourceStream(resourceName))
                               .Bind(stream => stream!.ReadStringAsync());
        }
    }
}
