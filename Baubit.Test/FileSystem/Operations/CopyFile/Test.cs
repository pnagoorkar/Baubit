using FluentResults;
using FluentResults.Extensions;

namespace Baubit.Test.FileSystem.Operations.CopyFile
{
    [Trait("Runtime", "Shared")]
    public class Test
    {
        [Fact]
        public async void CanCopyFiles()
        {
            string fileName = "SomeFile.txt";
            string fileContents = "Some random content";
            File.WriteAllText(fileName, fileContents);
            Assert.True(File.Exists(fileName));

            var destinationDirectory = Path.Combine(Environment.CurrentDirectory, "CopyFileSubFolder");

            var destinationFile = Path.Combine(destinationDirectory, fileName);

            var copyResult = await Baubit.FileSystem
                                         .Operations.DeleteDirectoryIfExistsAsync(new Baubit.FileSystem.DirectoryDeleteContext(destinationDirectory, true))
                                         .Bind(() => Baubit.FileSystem.Operations.CreateDirectoryAsync(new Baubit.FileSystem.DirectoryCreateContext(destinationDirectory)))
                                         .Bind(() => Baubit.FileSystem.Operations.CopyFileAsync(new Baubit.FileSystem.FileCopyContext(fileName, destinationFile, true)));

            Assert.True(copyResult.IsSuccess);
            Assert.True(File.Exists(destinationFile));
        }

        [Fact]
        public async void HandlesExceptionsGracefully()
        {
            string fileName = "SomeFile.txt";
            string fileContents = "Some random content";
            var destinationDirectory = Path.Combine(Environment.CurrentDirectory, "CopyFileSubFolder");
            var destination = Path.Combine(destinationDirectory, fileName);

            File.WriteAllText(fileName, fileContents);
            Assert.True(File.Exists(fileName));

            if (Directory.Exists(destinationDirectory)) Directory.Delete(destinationDirectory, true);

            var copyResult = await Baubit.FileSystem.Operations.CopyFileAsync(new Baubit.FileSystem.FileCopyContext(fileName, destination, true));

            Assert.False(copyResult.IsSuccess);
            Assert.Contains(copyResult.Errors, error => error is ExceptionalError expError && expError.Exception is DirectoryNotFoundException);
        }
    }
}
