using Baubit.Reflection.Reasons;
using Baubit.Traceability.Errors;
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

        public static Result<Type> TryResolveTypeAsync(string assemblyQualifiedName)
        {
            return Result.Try(() => Type.GetType(assemblyQualifiedName))
                         .Bind(type => type == null ? Result.Fail(new CompositeError<Type>([new TypeNotDefined(assemblyQualifiedName)], null, "", default)) : Result.Ok(type));
        }
    }
}
