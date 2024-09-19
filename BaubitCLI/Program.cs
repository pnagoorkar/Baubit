Baubit.Application.Initialize();
args = ["host", @"hostSettings.json"];
var result = Baubit.CLI.Operations.RunCLIOperationAsync(new Baubit.CLI.CLIOperationContext(args)).GetAwaiter().GetResult();
Environment.Exit(result.IsSuccess == true ? 0 : 1);