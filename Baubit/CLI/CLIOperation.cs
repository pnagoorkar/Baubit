using Baubit.Hosting;
using Microsoft.Extensions.Configuration;
using FluentResults;
using FluentResults.Extensions;

namespace Baubit.CLI
{
    public static partial class Operations
    {
        public static async Task<Result> RunCLIOperationAsync(CLIOperationContext context)
        {
            try
            {
                switch (context.OperationType.ToLower())
                {
                    case "host":
                        return await Configuration.Operations
                                                  .LoadFromJsonFileAsync(new Configuration.ConfiguratonLoadContext(context.OperationParametersJsonURI))
                                                  .Bind(operationParametersConfiguration => Result.Try((Func<Task<ApplicationHostingContext>>)(async () => { await Task.Yield(); return operationParametersConfiguration.Get<ApplicationHostingContext>(); })))
                                                  .Bind(Hosting.Operations.HostApplicationAsync);
                    default: return Result.Fail($"Undefined operation {context.OperationType}! See usage below..");
                }
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }
    }

    public class CLIOperationContext
    {
        public string[] Args { get; init; }
        public string OperationType { get => Args[0]; }
        public string OperationParametersJsonURI { get => Args[1]; }
        public CLIOperationContext(string[] args)
        {
            Args = args;
        }

    }
}
