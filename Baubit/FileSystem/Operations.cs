using FluentResults;
using FluentResults.Extensions;
namespace Baubit.FileSystem
{
    public static partial class Operations
    {
        /// <summary>
        /// Copy file from source to destination.
        /// Creates destination folder if not already exists.
        /// </summary>
        public static Func<FileCopyContext, Task<Result>> CreateDestinationAndCopyFileAsync = 
            async (context) => await CreateDirectoryAsync(new DirectoryCreateContext(context.Destination)).Bind(() => CopyFileAsync(context));
        
        /// <summary>
        /// Delete a given directory recursively and create an empty directory in its place
        /// </summary>
        public static Func<DirectoryCreateContext, bool, Task<Result>> DeleteDirectoryRecursivelyAndRecreateAsync = 
            async (context, deleteRecursively) => await DeleteDirectoryAsync(new DirectoryDeleteContext(context.Path, deleteRecursively)).Bind(() => CreateDirectoryAsync(context));
    }
}
