using Baubit.Configuration;
using Baubit.DI;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Baubit.Reflection
{
    public static class ObjectLoader
    {
        /// <summary>
        /// Loads a self contained object into the application domain. <typeparamref name="TSelfContained"/> must be decorated with <see cref="SourceAttribute"/>
        /// </summary>
        /// <typeparam name="TSelfContained">Type of the object to be loaded</typeparam>
        /// <returns>A result that will hold the object in its value if successful </returns>
        public static Result<TSelfContained> Load<TSelfContained>() where TSelfContained : class, ISelfContained
        {
            return Result.Try(() => typeof(TSelfContained).GetCustomAttribute<SourceAttribute>())
                         .Bind(sourceAttribute => sourceAttribute == null ? Result.Fail($"{typeof(TSelfContained).Name}{Environment.NewLine}The generic type parameter TSelfContained requires a {nameof(SourceAttribute)} to be instantiated") : Result.Ok(sourceAttribute))
                         .Bind(sourceAttribute => Result.Try(() => sourceAttribute.ConfigurationSource))
                         .Bind(Load<TSelfContained>);
        }
        public static Result<TSelfContained> Load<TSelfContained>(ConfigurationSource configSource) where TSelfContained : class, ISelfContained
        {
            return Result.Try(() => new ServiceCollection())
                         .Bind(services => services.AddSingleton<TSelfContained>().AddFrom(configSource))
                         .Bind(services => Result.Try(() => services.BuildServiceProvider().GetRequiredService<TSelfContained>()));
        }
        public static Result<TSelfContained> Load<TSelfContained>(ConfigurationSource configSource, string assemblyQualifiedName)
        {
            return TypeResolver.TryResolveTypeAsync(assemblyQualifiedName)
                               .Bind(type => Result.Try(() => new ServiceCollection().AddSingleton(serviceType: typeof(TSelfContained), implementationType: type)))
                               .Bind(services => Result.Try(() => services.BuildServiceProvider().GetRequiredService<TSelfContained>()));
        }
        public static Result<TSelfContained> Load<TSelfContained>(ConfigurationSource configSource, Type concreteType)
        {
            return Result.Try(() => new ServiceCollection().AddSingleton(serviceType: typeof(TSelfContained), implementationType: concreteType))
                         .Bind(services => Result.Try(() => services.BuildServiceProvider().GetRequiredService<TSelfContained>()));
        }
    }
}
