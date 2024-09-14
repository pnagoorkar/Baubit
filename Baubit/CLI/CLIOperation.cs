using Baubit.Hosting;
using Baubit.Operation;
using Microsoft.Extensions.Configuration;

namespace Baubit.CLI
{
    public class CLIOperation : IOperation<CLIOperation.Context, CLIOperation.Result>
    {
        private CLIOperation()
        {

        }
        private static CLIOperation _singletonInstance = new CLIOperation();
        public static CLIOperation GetInstance()
        {
            return _singletonInstance;
        }
        public async Task<Result> RunAsync(Context context)
        {
            try
            {
                switch (context.OperationType.ToLower())
                {
                    case "host":
                        if (!TryLoadingOperationParameters<HostApplication.Context>(context, out var parameters, out var result)) return result;
                        await Hosting.Operations.HostApplication.RunAsync(parameters);
                        break;
                    default: return new Result(false, $"Undefined operation {context.OperationType}! See usage below..");
                }
                return new Result(true, null);
            }
            catch (Exception exp)
            {
                return new Result(exp);
            }
        }

        private bool TryLoadingOperationParameters<TParameters>(Context context, out TParameters parameters, out Result result)
        {
            result = null;
            parameters = default;
            var loadConfigResult = Configuration.Operations
                                                .LoadFromJsonFile
                                                .RunAsync(new Configuration.LoadFromJsonFile.Context(context.OperationParametersJsonURI))
                                                .GetAwaiter()
                                                .GetResult();
            switch (loadConfigResult.Success)
            {
                default:
                    result = new Result(new Exception("", loadConfigResult.Exception));
                    break;
                case false:
                    result = new Result(false, "", loadConfigResult);
                    break;
                case true:
                    parameters = loadConfigResult.Value!.Get<TParameters>()!;
                    break;
            }
            return loadConfigResult.Success == true;
        }

        public sealed class Context : IContext
        {
            public string[] Args { get; init; }
            public string OperationType { get => Args[0]; }
            public string OperationParametersJsonURI { get => Args[1]; }
            public Context(string[] args)
            {
                Args = args;
            }
        }

        public sealed class Result : AResult
        {
            public Result(Exception? exception) : base(exception)
            {
            }

            public Result(bool? success, object? value) : base(success, value)
            {
            }

            public Result(bool? success, string? failureMessage, object? failureSupplement) : base(success, failureMessage, failureSupplement)
            {
            }
        }
    }
}
