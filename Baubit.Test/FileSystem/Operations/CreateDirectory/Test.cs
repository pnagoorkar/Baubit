namespace Baubit.Test.FileSystem.Operations.CreateDirectory
{
    public class Test
    {
        [Fact]
        public async void SuccessfulCreateResultsInASuccessfulResult()
        {
            var path = Path.Combine(Environment.CurrentDirectory, "Subfolder");
            var createResult = await Baubit.FileSystem.Operations.CreateDirectory.RunAsync(new Baubit.FileSystem.CreateDirectory.Context(path));
            Assert.True(createResult.Success);
        }
    }
}
