using Baubit.Store;
using FluentResults.Extensions;
using System.Reflection;
using System.Runtime.Loader;

namespace Baubit.Test.Store.Operations.LoadAssembly
{
    public class Test
    {
        [Fact]
        public async void CanLoadAssembliesDynamically()
        {
            var assemblyName = new AssemblyName { Name = "Autofac.Configuration", Version = new Version("7.0.0") };

            var loadResult = await Baubit.FileSystem
                                             .Operations
                                             .DeleteDirectoryIfExistsAsync(new Baubit.FileSystem.DirectoryDeleteContext(Application.BaubitRootPath, true))
                                             .Bind(() => Baubit.Store
                                                               .Operations
                                                               .DownloadPackageAsync(new Baubit.Store.PackageDownloadContext(assemblyName,
                                                                                                                             Application.TargetFramework,
                                                                                                                             Application.BaubitRootPath, true)))
                                             .Bind(package => Baubit.Store.Operations.LoadAssemblyAsync(new AssemblyLoadingContext(package, AssemblyLoadContext.Default)));

            Assert.True(loadResult.IsSuccess);
            Assert.NotNull(loadResult.Value);
            Assert.Contains(loadResult.Value.ExportedTypes, type => type.AssemblyQualifiedName!.Contains("Autofac.Configuration.ConfigurationModule"));
        }
    }
}
