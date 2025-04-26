using Baubit.Configuration.Reasons;
using Baubit.Traceability;
using FluentResults;

namespace Baubit.Configuration
{
    public sealed class ConfigurationSourceBuilder : IDisposable
    {
        private List<string> RawJsonStrings { get; set; } = new List<string>();
        private List<string> JsonUriStrings { get; set; } = new List<string>();
        private List<string> EmbeddedJsonResources { get; set; } = new List<string>();
        private List<string> LocalSecrets { get; set; } = new List<string>();

        private bool _isDisposed;
        private ConfigurationSourceBuilder()
        {
        }

        public static Result<ConfigurationSource> BuildEmpty() => CreateNew().Bind(configSourceBuilder => configSourceBuilder.Build()).ThrowIfFailed();

        public static Result<ConfigurationSourceBuilder> CreateNew() => Result.Ok(new ConfigurationSourceBuilder());

        public Result<ConfigurationSourceBuilder> WithJsonUriStrings(params string[] jsonUriStrings)
        {
            return Result.FailIf(_isDisposed, new Error(string.Empty))
                         .AddReasonIfFailed(new ConfigurationBuilderDisposed())
                         .Bind(() => Result.Try(() => JsonUriStrings.AddRange(jsonUriStrings)))
                         .Bind(() => Result.Ok(this));
        }
        public Result<ConfigurationSourceBuilder> WithEmbeddedJsonResources(params string[] embeddedJsonResources)
        {
            return Result.FailIf(_isDisposed, new Error(string.Empty))
                         .AddReasonIfFailed(new ConfigurationBuilderDisposed())
                         .Bind(() => Result.Try(() => EmbeddedJsonResources.AddRange(embeddedJsonResources)))
                         .Bind(() => Result.Ok(this));
        }
        public Result<ConfigurationSourceBuilder> WithLocalSecrets(params string[] localSecrets)
        {
            return Result.FailIf(_isDisposed, new Error(string.Empty))
                         .AddReasonIfFailed(new ConfigurationBuilderDisposed())
                         .Bind(() => Result.Try(() => LocalSecrets.AddRange(localSecrets)))
                         .Bind(() => Result.Ok(this));
        }
        public Result<ConfigurationSourceBuilder> WithRawJsonStrings(params string[] rawJsonStrings)
        {
            return Result.FailIf(_isDisposed, new Error(string.Empty))
                         .AddReasonIfFailed(new ConfigurationBuilderDisposed())
                         .Bind(() => Result.Try(() => RawJsonStrings.AddRange(rawJsonStrings)))
                         .Bind(() => Result.Ok(this));
        }
        public Result<ConfigurationSource> Build()
        {
            return Result.FailIf(_isDisposed, new Error(string.Empty))
                         .AddReasonIfFailed(new ConfigurationBuilderDisposed())
                         .Bind(() => Result.Try(() => new ConfigurationSource(RawJsonStrings.ToList(), JsonUriStrings.ToList(), EmbeddedJsonResources.ToList(), LocalSecrets.ToList())))
                         .Bind(configSource => Result.Try(() => { Dispose(); return configSource; }));
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
                    RawJsonStrings.Clear();
                    JsonUriStrings.Clear();
                    EmbeddedJsonResources.Clear();
                    LocalSecrets.Clear();

                    RawJsonStrings = null;
                    JsonUriStrings = null;
                    EmbeddedJsonResources = null;
                    LocalSecrets = null;
                }
                _isDisposed = true;
            }
        }
    }
}
