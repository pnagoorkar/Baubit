using Microsoft.Extensions.Configuration;
using FluentResults;
using Baubit.Hosting;
using Baubit.Configuration;

namespace Baubit.CLI
{
    public class CommandRunner
    {
        public static async Task<Result> RunAsync(CLICommand command)
        {
            try
            {
                switch (command.OperationType.ToLower())
                {
                    case "host":
                        return await Result.Try(command.OperationConfiguration.Get<Hostable>)
                                           .Bind(hostable => hostable!.HostAsync());
                    default: return Result.Fail($"Undefined operation {command.OperationType}! See usage below..");
                }
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }
    }

    public class CLICommand
    {
        public string[] Args { get; init; }
        public string OperationType { get => Args[0]; }
        public IConfiguration OperationConfiguration { get; init; }
        public CLICommand(string[] args)
        {
            Args = args;
            OperationConfiguration = new MetaConfiguration { JsonUriStrings = [Args[1]] }.Load();
        }
    }
}
