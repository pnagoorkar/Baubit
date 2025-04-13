using FluentResults;
using System.Text;

namespace Baubit.Traceability.Errors
{
    public sealed class CompositeError<TValue> : AError
    {

        public CompositeError(List<IReason> reasons,
                              List<IError> errors,
                              string message, 
                              Dictionary<string, object> metadata) : base(reasons, errors, message, metadata)
        {

        }
        public CompositeError(params Result<TValue>[] results) : this(results.SelectMany(result => result.Reasons).ToList(), 
                                                                      results.SelectMany(result => result.Errors).ToList(), 
                                                                      "Composite error", 
                                                                      default) 
        {

        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(Message);
            stringBuilder.AppendLine("".PadRight(10,'-'));
            if (Reasons.Count > 0 || NonErrorReasons.Count > 0)
            {
                if (Reasons.Count > 0)
                {
                    stringBuilder.AppendLine("Reasons(errors):");
                    stringBuilder.AppendJoin(Environment.NewLine, Reasons.Select((error, i) => $"{i + 1}. {error.ToString()}"));
                    stringBuilder.AppendLine();
                }
                var reportableNonErrors = NonErrorReasons.Where(reason => !Reasons.Contains(reason));
                if (reportableNonErrors.Any())
                {
                    stringBuilder.AppendLine("".PadRight(10, '-'));
                    stringBuilder.AppendLine("Reasons(non-error):");
                    stringBuilder.AppendJoin(Environment.NewLine, reportableNonErrors.Select((nonError, i) => $"{i + 1}. {nonError.ToString()}"));
                    stringBuilder.AppendLine();
                }
            }
            else
            {
                stringBuilder.AppendLine("Details unavailable");
            }
            stringBuilder.AppendLine("".PadRight(10, '-'));
            return stringBuilder.ToString();
        }
    }
}
