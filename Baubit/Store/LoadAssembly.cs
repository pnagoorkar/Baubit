using Baubit.Operation;
using System.Reflection;
using static Baubit.Store.LoadAssembly;

namespace Baubit.Store
{
    public class LoadAssembly : IOperation<Context, Result>
    {
        private LoadAssembly()
        {

        }
        private static LoadAssembly _singletonInstance = new LoadAssembly();
        public static LoadAssembly GetInstance()
        {
            return _singletonInstance;
        }
        public async Task<Result> RunAsync(Context context)
        {
            try
            {
                if (!TrySearchForPackage(context, out var searchResult, out var result))
                {
                    if (context.DownloadIfRequired)
                    {
                        if (TryDownloadingPackage(context, out var downloadPackageResult, out result) && TrySearchForPackage(context, out searchResult, out result))
                        {
                            //All good
                        }
                        else
                        {
                            return result;
                        }
                    }
                }

                foreach(var dependency in searchResult.Value!.Dependencies)
                {
                    var nestedDependencyLoadResult = await Operations.LoadAssembly.RunAsync(new Context(dependency.AssemblyName, context.TargetFramework, context.DownloadIfRequired));
                    switch (nestedDependencyLoadResult.Success)
                    {
                        default: return new Result(new Exception("", nestedDependencyLoadResult.Exception));
                        case false: return new Result(false, "", nestedDependencyLoadResult);
                        case true: break;
                    }
                }

                var assembly = Assembly.LoadFile(searchResult.Value.DllFile);

                return new Result(true, assembly);
            }
            catch (Exception ex)
            {
                return new Result(ex);
            }
        }

        private bool TrySearchForPackage(Context context, out Search.Result searchResult, out Result result)
        {
            result = null;
            searchResult = Operations.Search.RunAsync(new Search.Context(Application.BaubitPackageRegistry, context.AssemblyName, context.TargetFramework)).GetAwaiter().GetResult();
            switch (searchResult.Success)
            {
                default:
                    result = new Result(new Exception("", searchResult.Exception));
                    break;
                case false:
                    result = new Result(false, "", searchResult);
                    break;
                case true: break;
            }
            return searchResult.Success == true;
        }

        private bool TryDownloadingPackage(Context context, out DownloadPackage.Result downloadPackageResult, out Result result)
        {
            result = null;
            downloadPackageResult = Operations.DownloadPackage
                                              .RunAsync(new DownloadPackage.Context(context.AssemblyName, context.TargetFramework, Application.BaubitRootPath))
                                              .GetAwaiter()
                                              .GetResult();
            switch (downloadPackageResult.Success)
            {
                default:
                    result = new Result(new Exception("", downloadPackageResult.Exception));
                    break;
                case false:
                    result = new Result(false, "", downloadPackageResult);
                    break;
                case true: break;
            }
            return downloadPackageResult.Success == true;
        }

        public sealed class Context : IContext
        {
            public AssemblyName AssemblyName { get; init; }
            public string TargetFramework { get; init; }
            public bool DownloadIfRequired { get; init; }
            public Context(AssemblyName assemblyName, string targetFramework, bool downloadIfRequired)
            {
                AssemblyName = assemblyName;
                TargetFramework = targetFramework;
                DownloadIfRequired = downloadIfRequired;
            }
        }

        public sealed class Result : AResult<Assembly>
        {
            public const string PackageSearchFailedFailureMessage = $@"Package search failed !";
            public const string PackageSearchResultedInAnExceptionMessage = $@"Package search resulted in an exception !";

            public const string PackageDownloadFailedFailureMessage = $@"Package download failed !";
            public const string PackageDownloadResultedInAnExceptionMessage = $@"Package download resulted in an exception !";

            public Result(Exception? exception) : base(exception)
            {
            }

            public Result(bool? success, Assembly? value) : base(success, value)
            {
            }

            public Result(bool? success, string? failureMessage, object? failureSupplement) : base(success, failureMessage, failureSupplement)
            {
            }
        }

    }
}
