namespace Baubit.Configuration.Exceptions
{
    public class EnvironmentVariableNotFound : Exception
    {
        public string EnvVariable { get; init; }
        public EnvironmentVariableNotFound(string envVariable)
        {
            EnvVariable = envVariable;
        }
    }
}
