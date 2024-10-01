using Baubit.IO;
using FluentResults;
using FluentResults.Extensions;
using System;
using System.Reflection;

namespace Baubit.Store
{
    public static class AssemblyExtensions
    {
        public static async Task<Result<Package>> DetermineDownloadablePackagesAsync(this AssemblyName assemblyName, string targetFramework)
        {
            return await MockProject.Build(assemblyName, targetFramework)
                                    .Bind(mockProject => mockProject.BuildAsync());
        }
        public static async Task<Result> DetermineAssemblyVersion(this AssemblyName assemblyName)
        {
            return await NugetPackageDownloader.BuildAsync(assemblyName)
                                               .Bind(downloader => downloader.RunAsync())
                                               .Bind(nupkgFile => Result.Try(() => nupkgFile.GetAssemblyVersion(assemblyName.Name!)))
                                               .Bind(version => Result.Try(() => { assemblyName.Version = version; }));
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

        public static bool IsSameAs(this AssemblyName assemblyName, AssemblyName otherAssemblyName)
        {
            bool isNameEqual = otherAssemblyName.Name!.Equals(assemblyName.Name, StringComparison.OrdinalIgnoreCase);

            //bool isVersionEqual = otherAssemblyName.Version!.Major == assemblyName.Version!.Major &&
            //                      otherAssemblyName.Version!.Minor == assemblyName.Version!.Minor &&
            //                      otherAssemblyName.Version!.Build == assemblyName.Version!.Build;

            //if (otherAssemblyName.Version.Revision != -1 && assemblyName.Version.Revision != -1) // both assemblies have a Revision explicitly defined. We therefore check that they are also equal
            //{
            //    isVersionEqual = isVersionEqual && otherAssemblyName.Version!.Revision == assemblyName.Version!.Revision;
            //}

            return isNameEqual;
        }

        public static string ToNormalizedString(this Version version)
        {
            return version.ToString();
        }
    }
}
