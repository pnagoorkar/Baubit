using Baubit.Store;
using System.Reflection;
using System.Runtime.Loader;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Baubit.Test.Store.TypeResolver
{
    [TestCaseOrderer(TestCaseByOrderOrderer.Name, TestCaseByOrderOrderer.Assembly)]
    public class Test : IClassFixture<TypeResolverFixture>
    {
        private ITestOutputHelper testOutputHelper;
        private TypeResolverFixture fixture;
        public Test(TypeResolverFixture fixture, ITestOutputHelper testOutputHelper)
        {
            this.fixture = fixture;
            this.testOutputHelper = testOutputHelper;
        }

        [Fact]
        [Order("a")]
        public async void Setup()
        {
            if (Directory.Exists(Application.BaubitRootPath))
            {
                Directory.Delete(Application.BaubitRootPath, true);
            }
        }

        [Fact]
        [Order("b")]
        public async void CanDetermineDependenciesForTypeResolution()
        {
            foreach (var resolvableType in fixture.ResolvableTypes)
            {
                var assemblyName = new AssemblyName(resolvableType.Split(',').Skip(1).Aggregate("", (seed, next) => $"{seed},{next}").TrimStart(',').Trim());
                if (assemblyName.Version == null)
                {
                    var versionDeterminationResult = await assemblyName.DetermineAssemblyVersion();
                    Assert.True(versionDeterminationResult.IsSuccess);
                }
                var result = await assemblyName.DetermineDownloadablePackagesAsync(Application.TargetFramework);
                Assert.True(result.IsSuccess);
                fixture.Downloadables.Add(result.Value);
            }
        }

        [Fact]
        [Order("c")]
        public async void CanDownloadPackages()
        {
            foreach (var package in fixture.Downloadables)
            {
                var downloadResult = await package.DownloadAsync(true);
                Assert.True(downloadResult.IsSuccess);
                Assert.True(File.Exists(package.DllFile));
            }
        }

        [Fact]
        [Order("ca")]
        public async void CanPersistPackagesToRegistry()
        {
            foreach (var package in fixture.Downloadables)
            {
                var addResult = PackageRegistry.Add(Application.BaubitPackageRegistry, package, Application.TargetFramework);
                Assert.True(addResult.IsSuccess);
            }
        }

        [Fact]
        [Order("caa")]
        public async void CanSearchRegistry()
        {
            foreach (var package in fixture.Downloadables)
            {
                var searchResult = PackageRegistry.Search(Application.BaubitPackageRegistry, Application.TargetFramework, package.AssemblyName);
                Assert.True(searchResult.IsSuccess);
            }
        }

        [Fact]
        [Order("d")]
        public async void CanLoadPackages()
        {
            foreach (var package in fixture.Downloadables)
            {
                var loadResult = await package.LoadAsync(fixture.IsolatedContext1);
                Assert.True(loadResult.IsSuccess);
            }
        }

        [Fact]
        [Order("e")]
        public async void Reset()
        {
            fixture.IsolatedContext1.Unload();

            for (int i = 0; i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        [Fact]
        [Order("f")]
        public async void CanResolveTypeUsingTypeResolver()
        {
            if (Directory.Exists(Application.BaubitRootPath))
            {
                Directory.Delete(Application.BaubitRootPath, true);
            }
            foreach (var resolvableType in fixture.ResolvableTypes)
            {
                var result = await Baubit.Store.TypeResolver.ResolveTypeAsync(resolvableType);

                Assert.True(result.IsSuccess);
                Assert.NotNull(result.Value);
            }
        }
    }

    public class TypeResolverFixture
    {
        //public string[] ResolvableTypes { get; set; } = ["Autofac.Configuration.ConfigurationModule, Autofac.Configuration, Version=7.0.0"];
        public string[] ResolvableTypes { get; set; } = ["Autofac.Configuration.ConfigurationModule, Autofac.Configuration"];
        public List<Package> Downloadables { get; set; } = new List<Package>();
        public AssemblyLoadContext IsolatedContext1 { get; set; } = new AssemblyLoadContext(nameof(IsolatedContext1), true);
        public AssemblyLoadContext IsolatedContext2 { get; set; } = new AssemblyLoadContext(nameof(IsolatedContext2), true);
    }
    public sealed class TestCaseByOrderOrderer : ITestCaseOrderer
    {
        public const string Name = "Baubit.Test.Store.TypeResolver.TestCaseByOrderOrderer";
        public const string Assembly = "Baubit.Test";
        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases) where TTestCase : ITestCase
        {
            return testCases.OrderBy(testCase => string.IsNullOrEmpty(testCase.TestMethod.Method.GetOrder()))
                            .ThenBy(testCase => testCase.TestMethod.Method.GetOrder());
        }
    }

    [AttributeUsage(validOn: AttributeTargets.Method, AllowMultiple = false)]
    public class OrderAttribute : Attribute
    {
        public string Order { get; private set; }
        public OrderAttribute(string order)
        {
            Order = order;
        }
    }

    public static class OrderExtensions
    {
        public static string? GetOrder(this IMethodInfo methodInfo)
        {
            return methodInfo?.GetCustomAttributes(typeof(OrderAttribute).AssemblyQualifiedName)?
                              .FirstOrDefault()?
                              .GetConstructorArguments()?
                              .FirstOrDefault()?
                              .ToString();
        }
    }
}
