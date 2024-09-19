namespace Baubit.Test.FileSystem.Operations.CreateDirectory
{
    [Trait("Runtime", "Shared")]
    public class Test
    {
        [Fact]
        public async void CanCreateDirectories()
        {
            var path = Path.Combine(Environment.CurrentDirectory, "Subfolder");
            var createResult = await Baubit.FileSystem.Operations.CreateDirectoryAsync(new Baubit.FileSystem.DirectoryCreateContext(path));
            Assert.True(createResult.IsSuccess);
        }
    }
}
