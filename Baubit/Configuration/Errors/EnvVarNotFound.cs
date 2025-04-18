using Baubit.Traceability.Errors;

namespace Baubit.Configuration.Errors
{
    public class EnvVarNotFound : AError
    {
        public string EnvVariable { get; init; }
        public EnvVarNotFound(string envVar) : base([], $"Environemnt variable: {envVar} not found", default)
        {
            EnvVariable = envVar;
        }
    }
}
