using Baubit.IO;
using FluentResults;
using System.Diagnostics;

namespace Baubit.Process
{
    public abstract class AProcess : System.Diagnostics.Process
    {
        protected ProcessStartInfo startInfo;

        protected AProcess(string fileName,
                           IEnumerable<string> arguments)
        {
            startInfo = new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }
        public virtual async Task<Result> RunAsync()
        {
            try
            {
                using (System.Diagnostics.Process process = Start(startInfo))
                {
                    if (process == null) return Result.Fail(new ProcessFailedToStart());

                    HandleOutput(process.StandardOutput.EnumerateAsync());
                    HandleError(process.StandardError.EnumerateAsync());

                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0) return Result.Ok();
                    else return Result.Fail(new ProcessExitedWithNonZeroReturnCodeError(startInfo, process.ExitCode));
                }
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }

        protected abstract void HandleOutput(IAsyncEnumerable<char> outputMessage);
        protected abstract void HandleError(IAsyncEnumerable<char> errorMessage);
    }

    public class ProcessFailedToStart : IError
    {
        public List<IError> Reasons { get; }

        public string Message => "Failed to start process..";

        public Dictionary<string, object> Metadata { get; }
    }

    public class ProcessExitedWithNonZeroReturnCodeError : IError
    {
        public List<IError> Reasons { get; }

        public string Message => "Process exited with non zero code";

        public Dictionary<string, object> Metadata { get; }

        public ProcessStartInfo StartInfo { get; init; }
        public int ReturnCode { get; init; }

        public ProcessExitedWithNonZeroReturnCodeError(ProcessStartInfo startInfo, int returnCode)
        {
            StartInfo = startInfo;
            ReturnCode = returnCode;
        }
    }
}
