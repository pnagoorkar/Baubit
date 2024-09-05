using Baubit.Operation;
using System.Reflection;
using System.Text.Json;
using static Baubit.Store.Search;

namespace Baubit.Store
{
    public sealed class Search : IOperation<Context, Result>
    {
        private Search()
        {

        }
        private static Search _singletonInstance = new Search();
        public static Search GetInstance()
        {
            return _singletonInstance;
        }
        public async Task<Result> RunAsync(Context context)
        {
            try
            {
                await Task.Yield();
                if (!File.Exists(context.RegistryFilePath)) return new Result(false, "", null);

                try
                {
                    Application.BaubitStoreRegistryAccessor.WaitOne();

                    var registryContents = File.ReadAllText(context.RegistryFilePath);
                    var registry = JsonSerializer.Deserialize<PackageRegistry>(registryContents, Application.IndentedJsonWithCamelCase);

                    if (!registry!.ContainsKey(context.TargetFramework)) return new Result(false, "", null);

                    var package = registry![context.TargetFramework]
                                           .FirstOrDefault(package => package.AssemblyName.Name.Equals(context.AssemblyName.Name, StringComparison.OrdinalIgnoreCase) &&
                                                             package.AssemblyName.Version.Equals(context.AssemblyName.Version));

                    if (package == null) return new Result(false, "", null);

                    if (!File.Exists(package.DllFile)) return new Result(false, "", null);

                    return new Result(true, package);
                }
                catch
                {
                    throw;
                }
                finally
                {
                    Application.BaubitStoreRegistryAccessor.ReleaseMutex();
                }
            }
            catch (Exception ex)
            {
                return new Result(ex);
            }
        }

        private bool TryDeterminingPackageDependencies(Context context, out DetermineAssemblyDependencies.Result determinePackageResult, out Result result)
        {
            result = null;
            determinePackageResult = Baubit.Store
                                          .Operations
                                          .DetermineAssemblyDependencies
                                          .RunAsync(new Baubit.Store.DetermineAssemblyDependencies.Context(context.AssemblyName,
                                                                                                          context.TargetFramework))
                                          .GetAwaiter()
                                          .GetResult();
            switch (determinePackageResult.Success)
            {
                default:
                    result = new Result(new Exception("", determinePackageResult.Exception));
                    break;
                case false:
                    result = new Result(false, "", determinePackageResult);
                    break;
                case true: break;
            }
            return determinePackageResult.Success == true;
        }

        public sealed class Context : IContext
        {
            public string RegistryFilePath { get; init; }
            public AssemblyName AssemblyName { get; set; }
            public string TargetFramework { get; init; }
            public Context(string registryFilePath, AssemblyName assemblyName, string targetFramework)
            {
                RegistryFilePath = registryFilePath;
                AssemblyName = assemblyName;
                TargetFramework = targetFramework;
            }
        }

        public sealed class Result : AResult<Package>
        {
            public const string PackageNotFoundFailureMessage = $@"Package not found !";

            public static Result PackageNotFound { get => new Result(false, PackageNotFoundFailureMessage, null); }

            public Result(Exception? exception) : base(exception)
            {
            }

            public Result(bool? success, Package? value) : base(success, value)
            {
            }

            public Result(bool? success, string? failureMessage, object? failureSupplement) : base(success, failureMessage, failureSupplement)
            {
            }

        }
    }
}
