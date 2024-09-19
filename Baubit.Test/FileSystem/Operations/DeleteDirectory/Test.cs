using FluentResults;

namespace Baubit.Test.FileSystem.Operations.DeleteDirectory
{
    public class Test
    {
        [Fact]
        public async void SuccessfulDeleteResultsInASuccessfulResult()
        {
            var path = Path.Combine(Environment.CurrentDirectory, "DeleteDirectorySubfolder");
            Directory.CreateDirectory(path);
            var deleteResult = await Baubit.FileSystem.Operations.DeleteDirectoryAsync(new Baubit.FileSystem.DirectoryDeleteContext(path, true));
            Assert.True(deleteResult.IsSuccess);
        }
        [Fact]
        public async void HandlesExceptionsGracefully()
        {
            var path = Path.Combine(Environment.CurrentDirectory, "NonExistentDirectory");

            if(Directory.Exists(path)) Directory.Delete(path, true);

            var deleteResult = await Baubit.FileSystem.Operations.DeleteDirectoryAsync(new Baubit.FileSystem.DirectoryDeleteContext(path, true));

            Assert.False(deleteResult.IsSuccess);
            Assert.Contains(deleteResult.Errors, error => error is ExceptionalError expError && expError.Exception is DirectoryNotFoundException);
        }
    }
}
