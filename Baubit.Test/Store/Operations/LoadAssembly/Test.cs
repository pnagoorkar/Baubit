using FluentResults;
using FluentResults.Extensions;
using System.Reflection;

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

        [Fact]
        public async void FluentResultTrial()
        {
            //var result = await (await (await (await Method1()).Bind(str => Method2())).Bind(str => MethodThatWillFail())).Bind(str => MethodThatShouldNotbeCalled());

            var result = await Method1().Bind(str => Method2())
                                        .Bind(i => MethodThatWillFail())
                                        .Bind(i => MethodThatShouldNotbeCalled());
        }

        private async Task<Result<string>> Method1()
        {
            await Task.Yield();
            return "Some string";
        }

        private async Task<Result<int>> Method2()
        {
            await Task.Yield();
            return 42;
        }

        private async Task<Result<int>> MethodThatWillFail()
        {
            await Task.Yield();
            return Result.Fail(new MyCustomError());
        }

        private async Task<Result<string>> MethodThatShouldNotbeCalled()
        {
            await Task.Yield();
            return "Another string";
        }
    }

    public class MyCustomError : IError
    {
        public List<IError> Reasons { get; set; }

        public string Message { get; set; }

        public Dictionary<string, object> Metadata { get; set; }
    }
}
