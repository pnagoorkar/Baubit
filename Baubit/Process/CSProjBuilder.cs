using FluentResults;

namespace Baubit.Process
{
    public class CSProjBuilder : AProcess
    {
        public CSProjBuilder((string, IEnumerable<string>) args) : base(args.Item1, args.Item2)
        {
        }

        public CSProjBuilder(string csProjFile, string buildOutputFolder) : this(BuildArguments(csProjFile, buildOutputFolder))
        {

        }

        private static (string, IEnumerable<string>) BuildArguments(string csProjFile, string buildOutputFolder)
        {
            string fileName = "dotnet";
            IEnumerable<string> arguments = ["build", csProjFile,
                                             "--configuration", "Debug",
                                             "--output", buildOutputFolder];
            return (fileName, arguments);
        }

        protected override void HandleError(IAsyncEnumerable<char> errorMessage)
        {

        }

        protected override void HandleOutput(IAsyncEnumerable<char> outputMessage)
        {

        }
    }
}
