using FluentResults;
using System.Diagnostics;

namespace Baubit.Process
{
    public static partial class Operations
    {
        public static async Task<Result> RunProcessAsync(ProcessRunContext context)
        {
            try
            {
                using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(context.StartInfo))
                {
                    if (process == null) return Result.Fail(new ProcessFailedToStart());

                    if (context.OutputDataEventHandler != null)
                    {
                        process.OutputDataReceived += context.OutputDataEventHandler;
                        process.BeginOutputReadLine();
                    }
                    if (context.ErrorDataEventHandler != null)
                    {
                        process.ErrorDataReceived += context.ErrorDataEventHandler;
                        process.BeginErrorReadLine();
                    }

                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0) return Result.Ok();
                    else return Result.Fail(new ProcessExitedWithNonZeroReturnCodeError(context.StartInfo, process.ExitCode));
                }
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }
    }

    public class ProcessRunContext
    {
        public ProcessStartInfo StartInfo { get; init; }
        public DataReceivedEventHandler? OutputDataEventHandler { get; init; }
        public DataReceivedEventHandler? ErrorDataEventHandler { get; init; }
        public ProcessRunContext(ProcessStartInfo startInfo, DataReceivedEventHandler? outputDataEventHandler, DataReceivedEventHandler? errorDataEventHandler)
        {
            StartInfo = startInfo;
            OutputDataEventHandler = outputDataEventHandler;
            ErrorDataEventHandler = errorDataEventHandler;
        }
    }

    public class ProcessFailedToStart :IError
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

    //public class ProcessRunResult
    //{
    //    public string Output { get; init; }
    //    public string Error { get; init; }
    //    public int? ReturnCode { get; init; }
    //    public ProcessRunResult(string output, string error, int? returnCode = null)
    //    {
    //        Output = output;
    //        Error = error;
    //        ReturnCode = returnCode;
    //    }
    //}
}
