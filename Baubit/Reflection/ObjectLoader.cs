using Baubit.Configuration;
using Baubit.DI;
using Baubit.Reflection.Reasons;
using Baubit.Traceability;
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
        public static Result<TSelfContained> Load<TSelfContained>() where TSelfContained : class, ISelfContained => Load<TSelfContained>(typeof(TSelfContained));

        public static Result<TSelfContained> Load<TSelfContained>(ConfigurationSource configSource) where TSelfContained : class, ISelfContained => Load<TSelfContained>(configSource, typeof(TSelfContained));


        public static Result<TSelfContained> Load<TSelfContained>(Type concreteType) where TSelfContained : class, ISelfContained
        {
            return Result.OkIf(typeof(TSelfContained).IsAssignableFrom(concreteType), new Error(string.Empty))
                         .AddReasonIfFailed((res, reas) => res.WithReasons(reas), new IncompatibleTypes(typeof(TSelfContained), concreteType))
                         .Bind(() => Result.Try(() => typeof(TSelfContained).GetCustomAttribute<SourceAttribute>()))
                         .Bind(sourceAttribute => sourceAttribute == null ? Result.Fail($"{typeof(TSelfContained).Name}{Environment.NewLine}The generic type parameter TSelfContained requires a {nameof(SourceAttribute)} to be instantiated") : Result.Ok(sourceAttribute))
                         .Bind(sourceAttribute => Result.Try(() => sourceAttribute.ConfigurationSource))
                         .Bind(configSource => Load<TSelfContained>(configSource, concreteType));
        }
        public static Result<TSelfContained> Load<TSelfContained>(ConfigurationSource configSource, Type concreteType) where TSelfContained : class, ISelfContained
        {
            return Result.FailIf(concreteType.IsGenericType, new Error(string.Empty))
                         .AddReasonIfFailed((res, reas) => res.WithReasons(reas), new GenericTypesNotSupported())
                         .Bind(configSource.Build)
                         .Bind(config => ComponentBuilder<TSelfContained>.Create(config))
                         .Bind(compBuilder => compBuilder.WithRegistrationHandler(services => services.AddSingleton(concreteType)))
                         .Bind(compBuilder => compBuilder.Build());

            //return Result.FailIf(concreteType.IsGenericType, new Error(string.Empty))
            //             .AddReasonIfFailed((res, reas) => res.WithReasons(reas), new GenericTypesNotSupported())
            //             .Bind(configSource.Build)
            //             .Bind(config => config.LoadServices(services => services.AddSingleton(concreteType)))
            //             .Bind(serviceProvider => Result.Try(() => (TSelfContained)serviceProvider.GetRequiredService(concreteType)));
        }
        public static Result<TSelfContained> Load<TSelfContained>(ConfigurationSource configSource, string assemblyQualifiedName) where TSelfContained : class, ISelfContained
        {
            return TypeResolver.TryResolveTypeAsync(assemblyQualifiedName).Bind(Load<TSelfContained>);
        }
    }
}
