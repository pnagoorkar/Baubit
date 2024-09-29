using Baubit.Compression;
using Baubit.Process;
using FluentResults;
using FluentResults.Extensions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Baubit.Store
{
    public class NugetPackageDownloader : AProcess
    {
        public string DownloadRootDirectory { get; init; }

        private StringBuilder outputBuilder = new StringBuilder();

        private const string NugetAddedPackageLinePattern = @"Added package '(.+?)' to folder '(.+?)'";

        public NugetPackageDownloader(string fileName, IEnumerable<string> arguments, string downloadRoot) : base(fileName, arguments)
        {
            DownloadRootDirectory = downloadRoot;
        }

        public static async Task<Result<NugetPackageDownloader>> BuildAsync(AssemblyName assemblyName)
        {
            await Task.Yield();
            var downloadRootDirectory = Path.Combine(Path.GetTempPath(), $"temp_{assemblyName.Name}");
            string fileName = string.Empty;
            IEnumerable<string> arguments = Enumerable.Empty<string>();

            string[] linArgs = ["/nuget.exe"];

            string[] commonArgs = ["install", assemblyName.Name!,
                                   "-O", downloadRootDirectory,
                                   "-DependencyVersion", "Ignore"];

            string[] versionArgs = ["-Version", assemblyName.Version!.ToString()];

            if (Application.OSPlatform == OSPlatform.Windows)
            {
                fileName = "nuget";
                arguments = commonArgs;
            }
            else if (Application.OSPlatform == OSPlatform.Linux)
            {
                fileName = "mono";
                arguments = linArgs.Concat(commonArgs);
            }
            else
            {
                throw new NotImplementedException("Undefined OS for Nuget install command !");
            }

            if (assemblyName.Version != null) arguments.Concat(versionArgs);

            return Result.Ok(new NugetPackageDownloader(fileName, arguments, downloadRootDirectory));
        }

        public async new Task<Result<NupkgFile>> RunAsync()
        {
            try
            {
                return await ResetTempDownloadPath().Bind(() => base.RunAsync())
                                                    .Bind(() => DownloadedFolderExtractorTCS.Task)
                                                    .Bind(downloadedFolder => Result.Try(() => new NupkgFile(Path.Combine(DownloadRootDirectory, downloadedFolder, $"{downloadedFolder}.nupkg"))));
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }

        private async Task<Result> ResetTempDownloadPath()
        {
            try
            {
                await Task.Yield();
                if (Directory.Exists(DownloadRootDirectory))
                {
                    Directory.Delete(DownloadRootDirectory, true);
                }
                Directory.CreateDirectory(DownloadRootDirectory);
                return Result.Ok();
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }

        protected override async void HandleError(IAsyncEnumerable<char> errorMessage)
        {
            await foreach (char c in errorMessage)
            {

            }
        }

        private TaskCompletionSource<Result<string>> DownloadedFolderExtractorTCS = new TaskCompletionSource<Result<string>>();
        protected override async void HandleOutput(IAsyncEnumerable<char> outputMessage)
        {
            try
            {
                await foreach (char c in outputMessage)
                {
                    outputBuilder.Append(c);
                }
                var downloadedFolder = System.Text
                                             .RegularExpressions
                                             .Regex.Match(outputBuilder.ToString(), NugetAddedPackageLinePattern)
                                             .Groups
                                             .Values
                                             .Select(value => value.Value)
                                             .Skip(1)
                                             .First();
                DownloadedFolderExtractorTCS.SetResult(Result.Ok(downloadedFolder));
            }
            catch (Exception exp)
            {
                DownloadedFolderExtractorTCS.SetResult(Result.Fail(new ExceptionalError(exp)));
            }
        }
    }
}
