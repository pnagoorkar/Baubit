using FluentResults;

namespace Baubit.Regex
{
    public static partial class Operations
    {
        public static async Task<Result<string[]>> ExtractAsync(RegexExtractContext context)
        {
            return await Result.Try((Func<Task<string[]>>)(async () => 
            { 
                await Task.Yield(); 
                return System.Text
                             .RegularExpressions
                             .Regex
                             .Match(context.Input, context.Pattern)
                             .Groups
                             .Values
                             .Select(value => value.Value)
                             .ToArray(); 
            }));
        }
    }

    public class RegexExtractContext
    {
        public string Input { get; init; }
        public string Pattern { get; init; }
        public RegexExtractContext(string input, string pattern)
        {
            Input = input;
            Pattern = pattern;
        }
    }
}
