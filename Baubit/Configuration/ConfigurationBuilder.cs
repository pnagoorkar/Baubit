using Baubit.Configuration.Reasons;
using Baubit.Traceability;
using FluentResults;
using Microsoft.Extensions.Configuration;

namespace Baubit.Configuration
{

    public sealed class ConfigurationBuilder : IDisposable
    {
        private ConfigurationSource Value { get; set; }
        private bool _isDisposed;
        private ConfigurationBuilder()
        {
            Value = new ConfigurationSource();
        }

        public static Result<ConfigurationBuilder> CreateNew() => Result.Ok(new ConfigurationBuilder());

        public Result<ConfigurationBuilder> WithJsonUriStrings(params string[] jsonUriStrings)
        {
            return Result.FailIf(_isDisposed, new Error(string.Empty))
                         .AddReasonIfFailed((res, reas) => res.WithReasons(reas), new ConfigurationBuilderDisposed())
                         .Bind(() => Result.Try(() => Value.JsonUriStrings.AddRange(jsonUriStrings)))
                         .Bind(() => Result.Ok(this));
        }
        public Result<ConfigurationBuilder> WithEmbeddedJsonResources(params string[] embeddedJsonResources)
        {
            return Result.FailIf(_isDisposed, new Error(string.Empty))
                         .AddReasonIfFailed((res, reas) => res.WithReasons(reas), new ConfigurationBuilderDisposed())
                         .Bind(() => Result.Try(() => Value.EmbeddedJsonResources.AddRange(embeddedJsonResources)))
                         .Bind(() => Result.Ok(this));
        }
        public Result<ConfigurationBuilder> WithLocalSecrets(params string[] localSecrets)
        {
            return Result.FailIf(_isDisposed, new Error(string.Empty))
                         .AddReasonIfFailed((res, reas) => res.WithReasons(reas), new ConfigurationBuilderDisposed())
                         .Bind(() => Result.Try(() => Value.LocalSecrets.AddRange(localSecrets)))
                         .Bind(() => Result.Ok(this));
        }
        public Result<IConfiguration> Build()
        {
            return Result.FailIf(_isDisposed, new Error(string.Empty))
                         .AddReasonIfFailed((res, reas) => res.WithReasons(reas), new ConfigurationBuilderDisposed())
                         .Bind(Value.Build)
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
                    Value = null;
                }
                _isDisposed = true;
            }
        }
    }
}
