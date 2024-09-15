Baubit.Application.Initialize();
args = ["host", @"hostSettings.json"];
var result = Baubit.CLI.Operations.CLIOperation.RunAsync(new Baubit.CLI.CLIOperation.Context(args)).GetAwaiter().GetResult();
Environment.Exit(result.Success == true ? 0 : 1);