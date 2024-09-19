using System.Diagnostics;
using System.Reflection;

namespace Baubit.Test.CLI.Host
{
    [Trait("Runtime", "Shared")]
    public class Test
    {
        //private static string baubitExe = string.Empty;
        //static Test()
        //{
        //    var baubitCsProjFile = Path.Combine($"{Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.Parent?.FullName}", "Baubit", "Baubit.csproj");
        //    var buildOutputPath = Path.Combine($"{Application.BaubitRootPath}", "BaubitCli");
        //    if (Directory.Exists(buildOutputPath))
        //    {
        //        Directory.Delete(buildOutputPath, true);
        //    }

        //    var processStartInfo = new ProcessStartInfo
        //    {
        //        FileName = "dotnet",
        //        Arguments = $"publish {baubitCsProjFile} --configuration Release --output {buildOutputPath} -p:OutputType=exe",
        //        RedirectStandardOutput = true,
        //        RedirectStandardError = true,
        //        UseShellExecute = false,
        //        CreateNoWindow = false
        //    };
        //    var buildResult = Process.Operations.RunProcess.RunAsync(new Process.RunProcess.Context(processStartInfo, null, null)).GetAwaiter().GetResult();
        //    if (buildResult.Success == true)
        //    {
        //        baubitExe = Path.Combine(buildOutputPath, "baubit.exe");
        //    }
        //}

        //[Fact]
        //public async void CanHostAnApplicationViaCLI()
        //{
        //    Assert.True(File.Exists(baubitExe));
        //    var hostSettingsFile = "hostSettings.json";
        //    var readResult = await Resource.Operations.ReadEmbeddedResource.RunAsync(new Resource.ReadEmbeddedResource.Context($"{this.GetType().Namespace}.{hostSettingsFile}", Assembly.GetExecutingAssembly()));
        //    File.WriteAllText(hostSettingsFile, readResult.Value);
        //    var processStartInfo = new ProcessStartInfo
        //    {
        //        FileName = baubitExe,
        //        Arguments = $"host {hostSettingsFile}",
        //        RedirectStandardOutput = true,
        //        RedirectStandardError = true,
        //        UseShellExecute = false,
        //        CreateNoWindow = true
        //    };
        //    var runProcessResult = await Process.Operations.RunProcess.RunAsync(new Process.RunProcess.Context(processStartInfo, null, null));

        //}
    }
}
