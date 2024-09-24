using FluentResults;
using System.Reflection;

namespace Baubit.Store
{
    public static class AssemblyExtensions
    {
        public static async Task<Result<Package2>> DetermineDownloadablePackagesAsync(this AssemblyName assemblyName, string targetFramework)
        {
            return await new MockProject(assemblyName, targetFramework).BuildAsync();
        }
        public static string GetPersistableAssemblyName(this AssemblyName assemblyName)
        {
            return $"{assemblyName.Name}/{assemblyName.Version}";
        }

        public static AssemblyName GetAssemblyNameFromPersistableString(string value)
        {
            var nameParts = value.Split('/');
            return new AssemblyName { Name = nameParts[0], Version = new Version(nameParts[1]) };
        }
    }
}
