using System.Reflection;

namespace Baubit.Test.Store.Operations.DeterminePackageDependencies
{
    public class Test
    {
        [Fact]
        public async void Works()
        {
            var x = Application.TargetFramework; // initializing the application for handling assembly resolution event
            var assemblyName = new AssemblyName { Name = "Autofac.Configuration", Version = new Version("7.0.0") };
            var determinePackageResult = await Baubit.Store
                                                    .Operations
                                                    .DetermineAssemblyDependenciesAsync(new Baubit.Store.AssemblyDependencyDeterminationContext(assemblyName, Application.TargetFramework));
            Assert.True(determinePackageResult.IsSuccess);
        }
    }
}
