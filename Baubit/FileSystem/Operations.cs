using FluentResults;
using FluentResults.Extensions;
namespace Baubit.FileSystem
{
    public static class Operations
    {
        public static CreateDirectory CreateDirectory = CreateDirectory.GetInstance();
        public static DeleteDirectory DeleteDirectory = DeleteDirectory.GetInstance();
        public static CopyFile CopyFile = CopyFile.GetInstance();
        public static ReadFile ReadFile = ReadFile.GetInstance();
    }

    public static class Routines
    {
        /// <summary>
        /// Copy file from source to destination.
        /// Creates destination folder if not already exists.
        /// </summary>
        public static Func<CopyFile2.Context, Task<Result>> CreateDestinationAndCopyFileAsync = async (context) => await CreateDirectory2.RunAsync(new CreateDirectory2.Context(context.Destination)).Bind(() => CopyFile2.RunAsync(context));
        /// <summary>
        /// Delete a given directory recursively and create an empty directory in its place
        /// </summary>
        public static Func<CreateDirectory2.Context, Task<Result>> DeleteDirectoryRecursivelyAndRecreateAsync = async (context) => await DeleteDirectory2.RunAsync(new DeleteDirectory2.Context(context.Path, true)).Bind(() => CreateDirectory2.RunAsync(context));
    }
}
