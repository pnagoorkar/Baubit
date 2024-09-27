//using FluentResults;
//using System.Reflection;
//using System.Runtime.Loader;

//namespace Baubit.Store
//{
//    public static partial class Operations
//    {
//        public static async Task<Result<Type>> ResolveTypeAsync(TypeResolutionContext context)
//        {
//            try
//            {
//                await Task.Yield();

//                Result<Assembly> searchAndLoadResult = null;

//                Func<AssemblyName, Assembly?> assemblyResolver = assemblyName =>
//                {
//                    searchAndLoadResult = SearchDownloadAndLoadAssembly(assemblyName).GetAwaiter().GetResult();

//                    return searchAndLoadResult.IsSuccess ? searchAndLoadResult.Value : null;
//                };

//                var type = Type.GetType(context.AssemblyQualifiedName, assemblyResolver, (assembly, aqn, ignoreCase) => assembly.GetType(aqn, false, ignoreCase));
//                if (type == null)
//                {
//                    return Result.Fail($"Unable to resolve type: {context.AssemblyQualifiedName}").WithReasons(searchAndLoadResult!.Reasons);
//                }
//                else
//                {
//                    return Result.Ok(type).WithReasons(searchAndLoadResult!.Reasons);
//                }
//            }
//            catch (Exception exp)
//            {
//                return Result.Fail(new ExceptionalError(exp));
//            }
//        }

//        private static async Task<Result<Assembly>> SearchDownloadAndLoadAssembly(AssemblyName assemblyName)
//        {
//            try
//            {
//                var result = new Result<Assembly>();

//                //when assemblies are packaged along with the executable, versions are bound to be null.
//                //it may be assumed that the sought dll is in the current directory, but check before loading
//                if (assemblyName.Version == null && File.Exists(Path.Combine(Environment.CurrentDirectory, $"{assemblyName.Name}.dll")))
//                {
//                    var loadFromCWDResult = Result.Try(() => AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName));
//                }                

//                var existing = AssemblyLoadContext.Default
//                                                  .Assemblies
//                                                  .FirstOrDefault(assembly => assembly.GetName().Name.Equals(assemblyName.Name, StringComparison.OrdinalIgnoreCase));
//                if (existing == null)
//                {
//                    Package package = null;
//                    PackageRegistry registry = null;
//                    var searchResult = await Store.Operations.SearchPackageAsync(new Store.PackageSearchContext(Application.BaubitPackageRegistry, assemblyName, Application.TargetFramework));

//                    if (searchResult.IsSuccess)
//                    {
//                        registry = searchResult.Value.Registry;
//                        package = searchResult.Value.Package;
//                        result.Reasons.Add(new AssemblyFoundInLocalStore());
//                    }
//                    else
//                    {
//                        result.Reasons.Add(new AssemblyNotFoundInLocalStore());
//                        var download2Result = await Store.Operations.DownloadPackageAsync(new PackageDownloadContext(assemblyName, Application.TargetFramework, Application.BaubitRootPath, true));
//                        var downloadResult = await Store.Operations
//                                                        .DownloadPackageAsync(new PackageDownloadContext(assemblyName, Application.TargetFramework, Application.BaubitRootPath, true));

//                        if (downloadResult.IsSuccess)
//                        {
//                            registry = downloadResult.Value.Registry;
//                            package = downloadResult.Value.Package;
//                            result.Reasons.Add(new AssemblyDownlodedToLocalStore());
//                        }
//                        else
//                        {
//                            result.Reasons.Add(new AssemblyCouldNotBeDownloaded());
//                        }
//                    }
//                    if (package == null)
//                    {
//                        result = result.WithError("");
//                    }
//                    else
//                    {
//                        var loadResult = await Store.Operations.LoadAssemblyAsync(new AssemblyLoadingContext(package, registry, Application.TargetFramework, AssemblyLoadContext.Default));
//                        if (loadResult.IsSuccess)
//                        {
//                            result = result.WithSuccess("").WithValue(loadResult.Value);
//                        }
//                    }
//                }
//                else
//                {
//                    result = result.WithSuccess("").WithValue(existing).WithReason(new AssemblyFoundLoadedInDefaultAssemblyLoadContext());
//                }                

//                return result;
//            }
//            catch (Exception exp)
//            {
//                return Result.Fail(new ExceptionalError(exp));
//            }
//        }
//    }
//    public class TypeResolutionContext
//    {
//        public string AssemblyQualifiedName { get; init; }
//        public TypeResolutionContext(string assemblyQualifiedName)
//        {
//            AssemblyQualifiedName = assemblyQualifiedName;
//        }
//    }

//    public class AssemblyFoundLoadedInDefaultAssemblyLoadContext : IReason
//    {
//        public string Message { get => "Assembly found loaded in default AssemblyLoadContext !"; }

//        public Dictionary<string, object> Metadata { get; }
//    }

//    public class AssemblyFoundInLocalStore : IReason
//    {
//        public string Message { get => "Assembly found in local store !"; }

//        public Dictionary<string, object> Metadata { get; }
//    }

//    public class AssemblyDownlodedToLocalStore : IReason
//    {
//        public string Message { get => "Assembly downloaded to local store !"; }

//        public Dictionary<string, object> Metadata { get; }
//    }

//    public class AssemblyNotFoundInLocalStore : IReason
//    {
//        public string Message { get => "Assembly not found in local store !"; }

//        public Dictionary<string, object> Metadata { get; }
//    }

//    public class AssemblyCouldNotBeDownloaded : IReason
//    {
//        public string Message { get => "Assembly could not be downloaded local store !"; }

//        public Dictionary<string, object> Metadata { get; }
//    }
//}
