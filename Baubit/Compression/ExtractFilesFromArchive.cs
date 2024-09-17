using Baubit.Operation;
using FluentResults;
using System.Collections;
using System.IO.Compression;
using System.Linq.Expressions;

namespace Baubit.Compression
{
    public class ExtractFilesFromArchive : IOperation<ExtractFilesFromArchive.Context, ExtractFilesFromArchive.Result>
    {
        private ExtractFilesFromArchive()
        {

        }
        private static ExtractFilesFromArchive _singletonInstance = new ExtractFilesFromArchive();
        public static ExtractFilesFromArchive GetInstance()
        {
            return _singletonInstance;
        }
        public async Task<Result> RunAsync(Context context)
        {
            try
            {
                using (FileStream fileStream = new FileStream(context.Source, FileMode.Open))
                {
                    using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
                    {
                        List<string> extractedFiles = new List<string>();
                        foreach (ZipArchiveEntry entry in archive.Entries.Where(context.Criteria.Compile()))
                        {
                            string destinationFileName = context.RetainPaths ? Path.GetFullPath(entry.FullName, context.Destination) : Path.Combine(context.Destination, entry.Name);
                            if (context.CreateDestinationFolderIfNotExist)
                            {
                                var directoryCreateResult = await FileSystem.Operations.CreateDirectory.RunAsync(new FileSystem.CreateDirectory.Context(Path.GetDirectoryName(destinationFileName)));
                                switch (directoryCreateResult.Success)
                                {
                                    default: return new Result(new Exception("Create directory ended in an exception during file extraction", directoryCreateResult.Exception));
                                    case false: return new Result(false, "Create directory failed during file extraction !", directoryCreateResult);
                                    case true: break;
                                }
                            }
                            entry.ExtractToFile(destinationFileName, overwrite: context.Overwrite);
                            extractedFiles.Add(destinationFileName);
                        }
                        return new Result(true, extractedFiles);
                    }
                }
            }
            catch (Exception ex)
            {
                return new Result(ex);
            }
        }

        public sealed class Context : IContext
        {
            public string Source { get; init; }
            public string Destination { get; init; }
            public Expression<Func<ZipArchiveEntry, bool>> Criteria { get; init; }
            public bool Overwrite { get; init; }
            public bool CreateDestinationFolderIfNotExist { get; init; }
            public bool RetainPaths { get; init; }

            public Context(string source,
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

        public sealed class Result : AResult<List<string>>
        {
            public Result(Exception? exception) : base(exception)
            {
            }

            public Result(bool? success, List<string>? value) : base(success, value)
            {
            }

            public Result(bool? success, string? failureMessage, object? failureSupplement) : base(success, failureMessage, failureSupplement)
            {
            }
        }
    }

    public static class ExtractFilesFromArchive2
    {
        public static async Task<Result<List<string>>> RunAsync(Context context)
        {
            return Result.Try(() =>
            {
                List<string> extractedFiles = new List<string>();
                foreach (var entry in context.Entries)
                {
                    string destinationFileName = context.RetainPaths ? Path.GetFullPath(entry.FullName, context.Destination) : Path.Combine(context.Destination, entry.Name);
                    entry.ExtractToFile(destinationFileName, overwrite: context.Overwrite);
                    extractedFiles.Add(destinationFileName);
                }
                return extractedFiles;
            });
        }

        public sealed class Context : IContext
        {
            public IEnumerable<ZipArchiveEntry> Entries { get; init; }
            public string Destination { get; init; }
            public bool RetainPaths { get; init; }
            public bool Overwrite { get; init; }

            public Context(IEnumerable<ZipArchiveEntry> entries,
                           string destination,
                           bool retainPaths,
                           bool overwrite)
            {
                Entries = entries;
                Destination = destination;
                RetainPaths = retainPaths;
                Overwrite = overwrite;
            }
        }
    }

    public static class ReadZipArchiveEntries
    {
        public static async Task<Result<IEnumerable<ZipArchiveEntry>>> RunAsync(Context context)
        {
            await Task.Yield();
            return Result.Try(() => context.Source.EnumerateAsync(context.Criteria));
        }

        public sealed class Context : IContext
        {
            public string Source { get; init; }
            public Expression<Func<ZipArchiveEntry, bool>> Criteria { get; init; }

            public Context(string source,
                           Expression<Func<ZipArchiveEntry, bool>> criteria)
            {
                Source = source;
                Criteria = criteria;
            }
        }
    }

    public static class ZipArchiveExtensions
    {
        public static IEnumerable<ZipArchiveEntry> EnumerateAsync(this string zipFile, Expression<Func<ZipArchiveEntry, bool>> criteria)
        {
            using (FileStream fileStream = new FileStream(zipFile, FileMode.Open))
            {
                using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries.Where(criteria.Compile()))
                    {
                        yield return entry;
                    }
                }
            }
        }
    }
}
