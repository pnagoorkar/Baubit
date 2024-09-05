namespace Baubit.Test.Store.Operations.LoadAssembly
{
    public class Test
    {
        [Theory]
        //[InlineData("Autofac.Configuration.ConfigurationModule, Autofac.Configuration, Version=7.0.0")]
        [InlineData("Autofac.Module, Autofac, Version=8.1.0")]
        public async void CanLoadAssembliesDynamically(string typeName)
        {
            var x = Application.TargetFramework; // initializing the application for handling assembly resolution event
            var type = Type.GetType(typeName);
            Assert.NotNull(type);
        }
    }
}
