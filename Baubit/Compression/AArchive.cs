using FluentResults;
using System.IO.Compression;
using System.Linq.Expressions;

namespace Baubit.Compression
{
    public abstract class AArchive
    {
        public string FilePath { get; init; }
        protected AArchive(string filePath)
        {
            this.FilePath = filePath;
        }

        //public async Task<Result<List<string>>> ExtractAsync(Expression<Func<ZipArchiveEntry, bool>> criteria, 
        //                                                     bool retainPaths, 
        //                                                     string destination,
        //                                                     bool createDestinationFolderIfNotExist,
        //                                                     bool overwrite)
        //{
        //    try
        //    {
        //        List<string> extractedFiles = new List<string>();

        //        using (FileStream fileStream = new FileStream(FilePath, FileMode.Open))
        //        {
        //            using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
        //            {
        //                foreach (ZipArchiveEntry entry in archive.Entries.Where(criteria.Compile()))
        //                {
        //                    string destinationFileName = retainPaths ? Path.GetFullPath(entry.FullName, destination) : Path.Combine(destination, entry.Name);
        //                    if (createDestinationFolderIfNotExist)
        //                    {
        //                        var directoryCreateResult = await FileSystem.Operations.CreateDirectoryAsync(new FileSystem.DirectoryCreateContext(Path.GetDirectoryName(destinationFileName)!));
        //                        if (directoryCreateResult.IsFailed)
        //                        {
        //                            return Result.Merge(directoryCreateResult);
        //                        }
        //                    }
        //                    entry.ExtractToFile(destinationFileName, overwrite: overwrite);
        //                    extractedFiles.Add(destinationFileName);
        //                }
        //            }
        //        }
        //        return Result.Ok(extractedFiles);
        //    }
        //    catch (Exception exp)
        //    {
        //        return Result.Fail(new ExceptionalError(exp));
        //    }
        //}

        public async IAsyncEnumerable<ZipArchiveEntry> EnumerateEntriesAsync()
        {
            await Task.Yield();
            using (FileStream fileStream = new FileStream(FilePath, FileMode.Open))
            {
                using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        yield return entry;
                    }
                }
            }
        }
    }
}
