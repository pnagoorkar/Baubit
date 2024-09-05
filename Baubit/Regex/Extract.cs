using Baubit.Operation;

namespace Baubit.Regex
{
    public sealed class Extract : IOperation<Extract.Context, Extract.Result>
    {
        private Extract()
        {

        }
        private static Extract _singletonInstance = new Extract();
        public static Extract GetInstance()
        {
            return _singletonInstance;
        }
        public async Task<Result> RunAsync(Context context)
        {
            try
            {
                var match = System.Text.RegularExpressions.Regex.Match(context.Input, context.Pattern);
                var values = match.Groups.Values.Select(value => value.Value);
                return new Result(match.Success, values.ToArray());
            }
            catch (Exception ex)
            {
                return new Result(ex);
            }
        }

        public sealed class Context : IContext
        {
            public string Input { get; init; }
            public string Pattern { get; init; }
            public Context(string input, string pattern)
            {
                Input = input;
                Pattern = pattern;
            }
        }

        public sealed class Result : AResult<string[]>
        {
            public Result(Exception? exception) : base(exception)
            {
            }

            public Result(bool? success, string[]? value) : base(success, value)
            {
            }

            public Result(bool? success, string? failureMessage, object? failureSupplement) : base(success, failureMessage, failureSupplement)
            {
            }
        }
    }
}
