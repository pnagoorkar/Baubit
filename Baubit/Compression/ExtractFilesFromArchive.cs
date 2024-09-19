using FluentResults;
using System.IO.Compression;
using System.Linq.Expressions;

namespace Baubit.Compression
{
    public static partial class Operations
    {
        public static async Task<Result<List<string>>> ExtractFilesFromZipArchive(ZipExtractFilesContext context)
        {
            try
            {
                List<string> extractedFiles = new List<string>();

                using (FileStream fileStream = new FileStream(context.Source, FileMode.Open))
                {
                    using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries.Where(context.Criteria.Compile()))
                        {
                            string destinationFileName = context.RetainPaths ? Path.GetFullPath(entry.FullName, context.Destination) : Path.Combine(context.Destination, entry.Name);
                            if (context.CreateDestinationFolderIfNotExist)
                            {
                                var directoryCreateResult = await FileSystem.Operations.CreateDirectoryAsync(new FileSystem.DirectoryCreateContext(Path.GetDirectoryName(destinationFileName)!));
                                if(directoryCreateResult.IsFailed)
                                {
                                    return Result.Merge(directoryCreateResult);
                                }
                            }
                            entry.ExtractToFile(destinationFileName, overwrite: context.Overwrite);
                            extractedFiles.Add(destinationFileName);
                        }
                    }
                }
                return Result.Ok(extractedFiles);
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }
    }

    public class ZipExtractFilesContext
    {
        public string Source { get; init; }
        public string Destination { get; init; }
        public Expression<Func<ZipArchiveEntry, bool>> Criteria { get; init; }
        public bool Overwrite { get; init; }
        public bool CreateDestinationFolderIfNotExist { get; init; }
        public bool RetainPaths { get; init; }

        public ZipExtractFilesContext(string source,
                                      string destination,
                                      Expression<Func<ZipArchiveEntry, bool>> criteria,
                                      bool retainPaths = true,
                                      bool overwrite = false,
                                      bool createDestinationFolderIfNotExist = true)
        {
            Source = source;
            Destination = destination;
            Criteria = criteria;
            RetainPaths = retainPaths;
            Overwrite = overwrite;
            CreateDestinationFolderIfNotExist = createDestinationFolderIfNotExist;
        }
    }
}
