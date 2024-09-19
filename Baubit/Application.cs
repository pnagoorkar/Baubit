using Baubit.Store;
using FluentResults;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Baubit
{
    public class Application
    {
        public const string PathKey_BaubitRoot = "~BaubitRoot~";
        private const string BaubitBase_RootDirectoryName = "Baubit";
        public const string PathKey_ExecutingAssemlyLocation = "~ExecutingAssemblyLocation~";
        public const string RuntimeFrameworkExpectedPrefix = ".net";

        public static Dictionary<string, string> Paths = new Dictionary<string, string>
        {
            { PathKey_BaubitRoot, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), BaubitBase_RootDirectoryName) },
            { PathKey_ExecutingAssemlyLocation, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)! },
            {$"~{Environment.SpecialFolder.MyDocuments}~", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) }
        };

        public static string BaubitRootPath { get => Paths[PathKey_BaubitRoot]; }
        public static string BaubitPackageRegistry { get => Path.Combine(Paths[PathKey_BaubitRoot], "PackageRegistry.json"); }
        public static string TargetFramework { get; private set; }
        public static OSPlatform? OSPlatform { get; private set; }

        internal static Mutex BaubitStoreRegistryAccessor = new Mutex(false, nameof(BaubitStoreRegistryAccessor));

        static Application()
        {
            DetermineTargetFramework();
            DetermineOSPlatform();
        }

        public static void Initialize()
        {

        }

        private static void DetermineTargetFramework()
        {
            var frameworkDesc = RuntimeInformation.FrameworkDescription;
            if (!frameworkDesc.StartsWith(RuntimeFrameworkExpectedPrefix, StringComparison.CurrentCultureIgnoreCase))
            {
                throw new Exception("Invalid target framework !");
            }
            var version = new Version(frameworkDesc.Split(' ')[1]);
            TargetFramework = $"net{version.Major}.{version.Minor}";
        }

        private static void DetermineOSPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                OSPlatform = System.Runtime.InteropServices.OSPlatform.Windows;
            }
            else if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                OSPlatform = System.Runtime.InteropServices.OSPlatform.Linux;
            }
            else
            {
                OSPlatform = null;
            }
        }

        public static async Task<Result<Type>> ResolveTypeAsync(string assemblyQualifiedName)
        {
            try
            {
                await Task.Yield();
                var type = Type.GetType(assemblyQualifiedName, ResolveAssembly, (assembly, aqn, ignoreCase) => assembly.GetType(aqn, false, ignoreCase));
                if (type != null)
                {
                    return Result.Ok(type);
                }
                else
                {
                    return Result.Fail("");
                }
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }

        }

        private static Assembly? ResolveAssembly(AssemblyName assemblyName)
        {
            Package package = null;
            var searchResult = Store.Operations
                                    .SearchPackageAsync(new Store.PackageSearchContext(Application.BaubitPackageRegistry, assemblyName, Application.TargetFramework))
                                    .GetAwaiter()
                                    .GetResult();
            if (searchResult.IsSuccess)
            {
                package = searchResult.Value;
            }
            else
            {
                var downloadResult = Store.Operations
                                          .DownloadPackageAsync(new PackageDownloadContext(assemblyName, Application.TargetFramework, Application.BaubitRootPath))
                                          .GetAwaiter()
                                          .GetResult();

                if (downloadResult.IsSuccess)
                {
                    package = downloadResult.Value;
                }
            }
            if (package == null)
            {
                return null;
            }
            var loadResult = Store.Operations.LoadAssemblyAsync(new AssemblyLoadingContext(package, AssemblyLoadContext.Default)).GetAwaiter().GetResult();
            if (loadResult.IsSuccess)
            {
                return loadResult.Value;
            }
            else
            {
                return null;
            }
        }
    }
    public class BaubitAssemblyLoadContext : AssemblyLoadContext
    {
        public Assembly Assembly { get; private set; }
        public BaubitAssemblyLoadContext() : base(isCollectible: true) { }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            // Custom logic to resolve assemblies
            return null;
        }
    }
}
