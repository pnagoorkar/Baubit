
using System.Reflection;
using Xunit.Abstractions;

namespace Baubit.Test.Store.Operations.DetermineDownloadablePackages
{
    [Trait("TestName", nameof(Baubit.Test.Store.Operations.DetermineDownloadablePackages))]
    public class Test
    {
        private ITestOutputHelper testOutputHelper;
        public Test(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }
        [Fact]
        public async void Works()
        {
            var assemblyName = new AssemblyName { Name = "Autofac.Configuration", Version = new Version("7.0.0") };
            var determinePackageResult = await Baubit.Store
                                                    .Operations
                                                    .DetermineDownloadablePackagesAsync(new Baubit.Store.DownloadablePackagesDeterminationContext(assemblyName, Application.TargetFramework));

            Assert.True(determinePackageResult.IsSuccess);
        }
    }
}
