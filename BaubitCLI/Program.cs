
args = ["host", @"hostSettings.json"];

Environment.Exit(Baubit.CLI.CommandRunner.RunAsync(new Baubit.CLI.CLICommand(args)).GetAwaiter().GetResult().IsSuccess == true ? 0 : 1);