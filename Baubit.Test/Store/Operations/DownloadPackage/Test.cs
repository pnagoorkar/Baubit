using System.Reflection;

namespace Baubit.Test.Store.Operations.DownloadPackage
{
    public class Test
    {
        [Fact]
        public async void CanDownloadPackages()
        {
            var x = Application.TargetFramework; // initializing the application for handling assembly resolution event
            var assemblyName = new AssemblyName { Name = "Autofac.Configuration", Version = new Version("7.0.0") };
            var packageRoot = Path.Combine(Application.BaubitRootPath,
                                           Application.TargetFramework,
                                           assemblyName.Name);

            var dll = Path.Combine(packageRoot, 
                                   assemblyName.Version.ToString(), 
                                   $"{assemblyName.Name}.dll");

            if (Directory.Exists(packageRoot)) Directory.Delete(packageRoot, true);

            var downloadResult = await Baubit.Store
                                            .Operations
                                            .DownloadPackageAsync(new Baubit.Store.PackageDownloadContext(assemblyName,
                                                                                                          Application.TargetFramework,
                                                                                                          Application.BaubitRootPath));
            Assert.True(downloadResult.IsSuccess);
            Assert.EndsWith(downloadResult.Value.AssemblyName.Name, $"{assemblyName.Name}.dll", StringComparison.OrdinalIgnoreCase);
        }
    }
}
