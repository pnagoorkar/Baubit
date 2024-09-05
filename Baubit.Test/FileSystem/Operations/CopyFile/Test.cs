namespace Baubit.Test.FileSystem.Operations.CopyFile
{
    public class Test
    {
        [Fact]
        public async void SuccessfulCopyResultsInASuccessfulResult()
        {
            string fileName = "SomeFile.txt";
            string fileContents = "Some random content";
            File.WriteAllText(fileName, fileContents);
            Assert.True(File.Exists(fileName));
            var destination = Path.Combine(Environment.CurrentDirectory, "CopyFileSubFolder", fileName);
            var copyResult = await Baubit.FileSystem.Operations.CopyFile.RunAsync(new Baubit.FileSystem.CopyFile.Context(fileName, destination, true));
            Assert.True(copyResult.Success);
            Assert.True(File.Exists(destination));
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

            var copyResult = await Baubit.FileSystem.Operations.CopyFile.RunAsync(new Baubit.FileSystem.CopyFile.Context(fileName, destination, true, false));

            Assert.Null(copyResult.Success);
            Assert.IsType<DirectoryNotFoundException>(copyResult.Exception);
        }
    }
}
