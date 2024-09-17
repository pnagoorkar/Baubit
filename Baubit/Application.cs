using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text.Json;

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
            AssemblyLoadContext.Default.Resolving += Default_Resolving;
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

        private static List<BaubitAssemblyLoadContext> baubitAssemblyLoadContexts = new List<BaubitAssemblyLoadContext>();
        private static Assembly? Default_Resolving(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            if (assemblyName.Name.Equals(nameof(Baubit))) return Assembly.GetExecutingAssembly();

            //var target = baubitAssemblyLoadContexts.FirstOrDefault(c => c.Assembly.GetName().Name.Equals(assemblyName.Name, StringComparison.OrdinalIgnoreCase) &&
            //                                                            c.Assembly.GetName().Version.Equals(assemblyName.Version));

            //if (target != null)
            //{
            //    return target.Assembly;
            //}
            //else
            //{
            //    var ctxt = new BaubitAssemblyLoadContext();
            //    ctxt.LoadFromAssemblyPath("");
            //}

            var currentAssembly = AppDomain.CurrentDomain
                                       .GetAssemblies()
                                       .FirstOrDefault(assembly => assembly.GetName().Name.Equals(assemblyName.Name, StringComparison.OrdinalIgnoreCase) &&
                                                                   assembly.GetName().Version >= assemblyName.Version);
            
            if (currentAssembly != null) return currentAssembly;

            var loadResult = Store.Operations
                                  .LoadAssembly
                                  .RunAsync(new Store.LoadAssembly.Context(assemblyName, Application.TargetFramework, true))
                                  .GetAwaiter()
                                  .GetResult();
            return loadResult.Value;
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
