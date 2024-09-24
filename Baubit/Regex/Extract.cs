using FluentResults;
using System.Linq;
using System.Linq.Expressions;

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

    public class SingleValueExtractionContext
    {
        public string Input { get; init; }
        public string Pattern { get; init; }
        public Func<IEnumerable<string>, string> Selector { get; init; }
        public SingleValueExtractionContext(string input, string pattern, Func<IEnumerable<string>, string> selector)
        {
            Input = input;
            Pattern = pattern;
            Selector = selector;
        }
    }

    public static class RegexExtensions
    {
        public static Result<string> RunAsync(this SingleValueExtractionContext context)
        {
            return Result.Try(() => context.Selector(System.Text
                                                           .RegularExpressions
                                                           .Regex
                                                           .Match(context.Input, context.Pattern)
                                                           .Groups
                                                           .Values
                                                           .Select(value => value.Value)));
        }
    }
}
