using System.Reflection;
using System.Runtime.InteropServices;

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
    }
}
