namespace Baubit.Test.FileSystem.Operations.DeleteDirectory
{
    public class Test
    {
        [Fact]
        public async void SuccessfulDeleteResultsInASuccessfulResult()
        {
            var path = Path.Combine(Environment.CurrentDirectory, "DeleteDirectorySubfolder");
            Directory.CreateDirectory(path);
            var deleteResult = await Baubit.FileSystem.Operations.DeleteDirectory.RunAsync(new Baubit.FileSystem.DeleteDirectory.Context(path, true));
            Assert.True(deleteResult.Success);
        }
        [Fact]
        public async void HandlesExceptionsGracefully()
        {
            var path = Path.Combine(Environment.CurrentDirectory, "NonExistentDirectory");
            if(Directory.Exists(path)) Directory.Delete(path, true);
            var deleteResult = await Baubit.FileSystem.Operations.DeleteDirectory.RunAsync(new Baubit.FileSystem.DeleteDirectory.Context(path, true));
            Assert.Null(deleteResult.Success);
            Assert.IsType<DirectoryNotFoundException>(deleteResult.Exception);
        }
    }
}
