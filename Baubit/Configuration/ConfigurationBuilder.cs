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
            return FailIfDisposed().Bind(() => _configurationSourceBuilder.WithJsonUriStrings(jsonUriStrings))
                                   .Bind(_ => Result.Ok(this));
        }
        public Result<ConfigurationBuilder> WithEmbeddedJsonResources(params string[] embeddedJsonResources)
        {
            return FailIfDisposed().Bind(() => _configurationSourceBuilder.WithEmbeddedJsonResources(embeddedJsonResources))
                                   .Bind(_ => Result.Ok(this));
        }
        public Result<ConfigurationBuilder> WithLocalSecrets(params string[] localSecrets)
        {
            return FailIfDisposed().Bind(() => _configurationSourceBuilder.WithLocalSecrets(localSecrets))
                                   .Bind(_ => Result.Ok(this));
        }
        public Result<ConfigurationBuilder> WithRawJsonStrings(params string[] rawJsonStrings)
        {
            return FailIfDisposed().Bind(() => _configurationSourceBuilder.WithRawJsonStrings(rawJsonStrings))
                                   .Bind(_ => Result.Ok(this));
        }
        public Result<IConfiguration> Build()
        {
            return FailIfDisposed().Bind(_configurationSourceBuilder.Build)
                                   .Bind(configSource => configSource.Build())
                                   .Bind(configuration => Result.Try(() => { Dispose(); return configuration; }));
        }

        private Result FailIfDisposed()
        {
            return Result.FailIf(_isDisposed, new Error(string.Empty))
                         .AddReasonIfFailed(new ConfigurationBuilderDisposed());
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
