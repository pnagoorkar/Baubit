using FluentResults;

namespace Baubit.Store
{
    public static class Operations
    {
        public static Search Search = Search.GetInstance();
        public static LoadAssembly LoadAssembly = LoadAssembly.GetInstance();
        public static DownloadPackage DownloadPackage = DownloadPackage.GetInstance();
        public static DetermineAssemblyDependencies DetermineAssemblyDependencies = DetermineAssemblyDependencies.GetInstance();
        public static AddToRegistry AddToRegistry = AddToRegistry.GetInstance();
    }
}
