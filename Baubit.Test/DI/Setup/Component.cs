namespace Baubit.Test.DI.Setup
{
    public class Component
    {
        public string SomeString { get; init; }
        public string SomeSecretString { get; init; }

        public Component(string someString, string someSecretString)
        {
            SomeString = someString;
            SomeSecretString = someSecretString;
        }
    }
}
