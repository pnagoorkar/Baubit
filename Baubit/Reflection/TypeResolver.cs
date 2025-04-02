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
            return null;
        }

        //public static async Task<Result<Type?>> TryResolveTypeAsync(string assemblyQualifiedName, CancellationToken cancellationToken)
        //{
        //    await Task.Yield();
        //    return Result.Try(() => Type.GetType(assemblyQualifiedName));
        //}

        public static Result<Type> TryResolveTypeAsync(string assemblyQualifiedName)
        {
            return Result.Try(() => Type.GetType(assemblyQualifiedName))
                         .Bind(type => type == null ? Result.Fail(new CompositeError<Type>([new TypeNotDefined(assemblyQualifiedName)], null, "", default)) : Result.Ok(type));
        }

        //public static async Task<Result<T?>> TryCreateInstanceAsync<T>(string assemblyQualifiedName, CancellationToken cancellationToken)
        //{
        //    return await TryResolveTypeAsync(assemblyQualifiedName, cancellationToken).Bind(type => Result.Try(() => (T)Activator.CreateInstance(type)));
        //}
    }
}
