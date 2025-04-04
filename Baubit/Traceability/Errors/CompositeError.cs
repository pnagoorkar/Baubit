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
            stringBuilder.AppendLine("Reasons(non-error):");
            for (int i = 0; i < NonErrorReasons?.Count; i++)
            {
                stringBuilder.AppendLine($"{i + 1}. {NonErrorReasons[i].ToString()}");
            }
            stringBuilder.AppendLine("Reasons(errors):");
            for (int i = 0; i < Reasons?.Count; i++)
            {
                stringBuilder.AppendLine($"{i + 1}. {Reasons[i].ToString()}");
            }
            return stringBuilder.ToString();
        }
    }
}
