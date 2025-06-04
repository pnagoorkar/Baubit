using Baubit.Reflection.Reasons;
using Baubit.Traceability;
using FluentResults;
using System.Reflection;
using System.Runtime.Loader;

namespace Baubit.Reflection
{
    public sealed class TypeResolver
    {
        static TypeResolver()
        {
            AssemblyLoadContext.Default.Resolving += OnAssemblyLoading;
        }

        private static Assembly? OnAssemblyLoading(AssemblyLoadContext assemblyLoadContext, AssemblyName assemblyName)
        {
            return null;//TODO
        }

        public static Result<Type> TryResolveType(string assemblyQualifiedName)
        {
            return Result.Try(() => Type.GetType(assemblyQualifiedName))
                         .Bind(type => Result.FailIf(type == null, new Error(string.Empty))
                                             .AddReasonIfFailed(new TypeNotDefined(assemblyQualifiedName))
                                             .Bind(() => Result.Ok(type!)));
        }
    }
}
