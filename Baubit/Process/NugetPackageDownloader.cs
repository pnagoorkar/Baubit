using Baubit.Compression;
using FluentResults;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Baubit.Process
{
    public class NugetPackageDownloader : AProcess
    {
        public string DownloadedFolder { get; private set; } = string.Empty;

        private StringBuilder outputBuilder = new StringBuilder();

        private const string NugetAddedPackageLinePattern = @"Added package '(.+?)' to folder '(.+?)'";

        public NugetPackageDownloader((string, IEnumerable<string>) args) : base(args.Item1, args.Item2)
        {

        }
        public NugetPackageDownloader(AssemblyName assemblyName) : this(BuildArguments(assemblyName))
        {

        }

        private static (string, IEnumerable<string>) BuildArguments(AssemblyName assemblyName)
        {
            var tempDownloadPath = Path.Combine(Path.GetTempPath(), $"temp_{assemblyName.Name}");
            string fileName = string.Empty;
            IEnumerable<string> arguments = Enumerable.Empty<string>();

            string[] linArgs = ["/nuget.exe"];

            string[] commonArgs = ["install", assemblyName.Name!,
                                   "-O", tempDownloadPath,
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

            return (fileName, arguments);
        }

        public async new Task<Result<NupkgFile>> RunAsync()
        {
            try
            {
                await base.RunAsync();
                //var tempDownloadPath = Path.Combine(Path.GetTempPath(), $"temp_{assemblyName.Name}");
                return Result.Ok(new NupkgFile(Path.Combine("", DownloadedFolder, $"{DownloadedFolder}.nupkg")));
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

        protected override async void HandleOutput(IAsyncEnumerable<char> outputMessage)
        {
            await foreach (char c in outputMessage)
            {
                outputBuilder.Append(c);
            }
            DownloadedFolder = System.Text
                                     .RegularExpressions
                                     .Regex.Match(outputBuilder.ToString(), NugetAddedPackageLinePattern)
                                     .Groups
                                     .Values
                                     .Select(value => value.Value)
                                     .Skip(1)
                                     .First();
        }
    }
}
