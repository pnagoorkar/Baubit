using Baubit.Traceability.Reasons;

namespace Baubit.Configuration.Reasons
{
    public class EnvVarNotFound : AReason
    {
        public string EnvVariable { get; init; }
        public EnvVarNotFound(string envVar) : base($"Environemnt variable: {envVar} not found", default)
        {
            EnvVariable = envVar;
        }
    }
}
