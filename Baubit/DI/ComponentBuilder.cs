using Baubit.Configuration;
using Baubit.DI.Reasons;
using Baubit.Reflection;
using Baubit.Traceability;
using Baubit.Validation;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Baubit.DI
{
    public sealed class ComponentBuilder<T> : IDisposable where T : class
    {
        private RootModule _rootModule;
        private List<Func<IServiceCollection, IServiceCollection>> _handlers = new List<Func<IServiceCollection, IServiceCollection>>();
        private bool _isDisposed;
        private ComponentBuilder(IConfiguration configuration)
        {
            _rootModule = new RootModule(configuration);
        }

        public static Result<ComponentBuilder<T>> Create() => Configuration.ConfigurationBuilder.CreateNew().Bind(cB => cB.Build()).Bind(Create);
        public static Result<ComponentBuilder<T>> Create(ConfigurationSource configSource) => configSource.Build().Bind(Create);
        public static Result<ComponentBuilder<T>> Create(IConfiguration configuration) => Result.Ok(new ComponentBuilder<T>(configuration));

        public static Result<ComponentBuilder<T>> CreateFromSourceAttribute()
        {
            return Result.Try(() => typeof(T).GetCustomAttribute<SourceAttribute>())
                         .Bind(sourceAttribute => Result.FailIf(sourceAttribute == null, new Error(string.Empty))
                                                        .AddReasonIfFailed(new SourceMissing<T>())
                                                        .Bind(() => Result.Ok(sourceAttribute)))
                         .Bind(sourceAttribute => sourceAttribute!.GetConfigSourceFromSourceAttribute())
                         .Bind(Create);
        }

        public Result<ComponentBuilder<T>> WithRegistrationHandler(Func<IServiceCollection, IServiceCollection> handler)
        {
            return FailIfDisposed().Bind(() => Result.Try(() => { if (!_handlers.Contains(handler)) _handlers.Add(handler); }))
                                   .Bind(() => Result.Ok(this));
        }
        public Result<T> Build(bool validateRoot = false)
        {
            return FailIfDisposed().Bind(() => validateRoot ? _rootModule.TryValidate(_rootModule.Configuration.ModuleValidatorTypes).Bind(_ => Result.Ok()) : Result.Ok())
                                   .Bind(() => Result.Try(() => new ServiceCollection()))
                                   .Bind(services => Result.Try(() => { _rootModule.Load(services); return services; }))
                                   .Bind(services => _handlers.Aggregate(Result.Ok<IServiceCollection>(services), (seed, next) => seed.Bind(s => Result.Try(() => next(s)))))
                                   .Bind(services => Result.Try(() => services.BuildServiceProvider().GetRequiredService<T>()))
                                   .Bind(component => Result.Try(() => { Dispose(); return component; }));
        }

        private Result FailIfDisposed()
        {
            return Result.FailIf(_isDisposed, new Error(string.Empty))
                         .AddReasonIfFailed(new ComponentBuilderDisposed<T>());
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _rootModule = null;
                }
                _isDisposed = true;
            }
        }
    }
}
