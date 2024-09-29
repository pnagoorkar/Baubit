
using Baubit.Store;
using System.Reflection;
using System.Runtime.Loader;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Baubit.Test.Store.Operations.TypeResolver
{
    [Trait("TestName", nameof(Baubit.Test.Store.Operations.TypeResolver))]
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
            foreach(var resolvableType in fixture.ResolvableTypes)
            {
                var assemblyName = new AssemblyName(resolvableType.Split(',').Skip(1).Aggregate("", (seed, next) => $"{seed},{next}").TrimStart(',').Trim());
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
                var addResult = await PackageRegistry2.Add(Application.TargetFramework, package);
                Assert.True(addResult.IsSuccess);
            }
        }

        [Fact]
        [Order("caa")]
        public async void CanSearchRegistry()
        {
            foreach (var package in fixture.Downloadables)
            {
                var searchResult = await PackageRegistry2.SearchAsync(package.AssemblyName, Application.TargetFramework);
                Assert.True(searchResult.IsSuccess);
            }
        }

        [Fact]
        [Order("d")]
        public async void CanLoadPackages()
        {
            foreach (var package in fixture.Downloadables)
            {
                var downloadResult = await package.LoadAsync(fixture.IsolatedContext1);
                Assert.True(downloadResult.IsSuccess);
            }
        }

        [Fact]
        [Order("e")]
        public async void Reset()
        {
            fixture.IsolatedContext1.Unload();
            GC.Collect();
            await Task.Delay(1000);
            if (Directory.Exists(Application.BaubitRootPath))
            {
                Directory.Delete(Application.BaubitRootPath, true);
            }
        }

        [Fact]
        [Order("f")]
        public async void CanResolveTypeUsingTypeResolver()
        {
            foreach (var resolvableType in fixture.ResolvableTypes)
            {
                var result = await Baubit.Store.TypeResolver.ResolveTypeAsync(resolvableType);

                Assert.True(result.IsSuccess);
                Assert.NotNull(result.Value);
            }
        }

        //[Theory]
        //[InlineData("Autofac.Configuration.ConfigurationModule, Autofac.Configuration, Version=7.0.0")]
        //public async void CanResolveTypeFromAssemblyQualifiedName(string assemblyQualifiedName)
        //{
        //    var result = await Baubit.Store.TypeResolver.ResolveTypeAsync(assemblyQualifiedName);

        //    Assert.True(result.IsSuccess);
        //    Assert.NotNull(result.Value);
        //}
    }

    public class TypeResolverFixture
    {
        public string[] ResolvableTypes { get; set; } = ["Autofac.Configuration.ConfigurationModule, Autofac.Configuration, Version=7.0.0"];
        public List<Package2> Downloadables { get; set; } = new List<Package2>();
        public AssemblyLoadContext IsolatedContext1 { get; set; } = new AssemblyLoadContext(nameof(IsolatedContext1), true);
        public AssemblyLoadContext IsolatedContext2 { get; set; } = new AssemblyLoadContext(nameof(IsolatedContext2), true);
    }
    public sealed class TestCaseByOrderOrderer : ITestCaseOrderer
    {
        public const string Name = "Baubit.Test.Store.Operations.TypeResolver.TestCaseByOrderOrderer";
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
