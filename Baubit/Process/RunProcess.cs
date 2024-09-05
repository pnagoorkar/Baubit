using Baubit.Operation;
using System.Diagnostics;

namespace Baubit.Process
{
    public sealed class RunProcess : IOperation<RunProcess.Context, RunProcess.Result>
    {
        private RunProcess()
        {

        }
        private static RunProcess _singletonInstance = new RunProcess();
        public static RunProcess GetInstance()
        {
            return _singletonInstance;
        }
        public async Task<Result> RunAsync(Context context)
        {
            try
            {
                using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(context.StartInfo))
                {
                    if (process == null) return new Result(new Exception("Failed to start process.."));

                    if(context.OutputDataEventHandler != null)
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

                    if (process.ExitCode == 0) return new Result(true, null);
                    else return new Result(false, null, null);
                }
            }
            catch (Exception e)
            {
                return new Result(e);
            }
        }

        public sealed class Context : IContext
        {
            public ProcessStartInfo StartInfo { get; init; }
            public DataReceivedEventHandler? OutputDataEventHandler { get; init; }
            public DataReceivedEventHandler? ErrorDataEventHandler { get; init; }
            public Context(ProcessStartInfo startInfo, DataReceivedEventHandler? outputDataEventHandler, DataReceivedEventHandler? errorDataEventHandler)
            {
                StartInfo = startInfo;
                OutputDataEventHandler = outputDataEventHandler;
                ErrorDataEventHandler = errorDataEventHandler;
            }
        }

        public sealed class Result : AResult<string>
        {
            public Result(Exception? exception) : base(exception)
            {
            }

            public Result(bool? success, string? value) : base(success, value)
            {
            }

            public Result(bool? success, string? failureMessage, object? failureSupplement) : base(success, failureMessage, failureSupplement)
            {
            }
        }
    }
}
