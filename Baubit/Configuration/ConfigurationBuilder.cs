using Baubit.Configuration.Reasons;
using Baubit.Traceability;
using FluentResults;
using Microsoft.Extensions.Configuration;

namespace Baubit.Configuration
{

    public sealed class ConfigurationBuilder : IDisposable
    {
        private ConfigurationSourceBuilder _configurationSourceBuilder;
        private bool _isDisposed;
        private ConfigurationBuilder()
        {
            _configurationSourceBuilder = ConfigurationSourceBuilder.CreateNew().Value;
        }

        public static Result<ConfigurationBuilder> CreateNew() => Result.Ok(new ConfigurationBuilder());

        public Result<ConfigurationBuilder> WithJsonUriStrings(params string[] jsonUriStrings)
        {
            return Result.FailIf(_isDisposed, new Error(string.Empty))
                         .AddReasonIfFailed((res, reas) => res.WithReasons(reas), new ConfigurationBuilderDisposed())
                         .Bind(() => _configurationSourceBuilder.WithJsonUriStrings(jsonUriStrings))
                         .Bind(_ => Result.Ok(this));
        }
        public Result<ConfigurationBuilder> WithEmbeddedJsonResources(params string[] embeddedJsonResources)
        {
            return Result.FailIf(_isDisposed, new Error(string.Empty))
                         .AddReasonIfFailed((res, reas) => res.WithReasons(reas), new ConfigurationBuilderDisposed())
                         .Bind(() => _configurationSourceBuilder.WithEmbeddedJsonResources(embeddedJsonResources))
                         .Bind(_ => Result.Ok(this));
        }
        public Result<ConfigurationBuilder> WithLocalSecrets(params string[] localSecrets)
        {
            return Result.FailIf(_isDisposed, new Error(string.Empty))
                         .AddReasonIfFailed((res, reas) => res.WithReasons(reas), new ConfigurationBuilderDisposed())
                         .Bind(() => _configurationSourceBuilder.WithLocalSecrets(localSecrets))
                         .Bind(_ => Result.Ok(this));
        }
        public Result<ConfigurationBuilder> WithRawJsonStrings(params string[] rawJsonStrings)
        {
            return Result.FailIf(_isDisposed, new Error(string.Empty))
                         .AddReasonIfFailed((res, reas) => res.WithReasons(reas), new ConfigurationBuilderDisposed())
                         .Bind(() => _configurationSourceBuilder.WithRawJsonStrings(rawJsonStrings))
                         .Bind(_ => Result.Ok(this));
        }
        public Result<IConfiguration> Build()
        {
            return Result.FailIf(_isDisposed, new Error(string.Empty))
                         .AddReasonIfFailed((res, reas) => res.WithReasons(reas), new ConfigurationBuilderDisposed())
                         .Bind(_configurationSourceBuilder.Build)
                         .Bind(configSource => configSource.Build())
                         .Bind(configuration => Result.Try(() => { Dispose(); return configuration; }));
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
                    _configurationSourceBuilder.Dispose();
                    _configurationSourceBuilder = null;
                }
                _isDisposed = true;
            }
        }
    }
}
