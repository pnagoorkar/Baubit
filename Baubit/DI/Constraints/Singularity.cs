using Baubit.DI.Constraints.Reasons;
using Baubit.Traceability;
using FluentResults;
using Microsoft.Extensions.Configuration;

namespace Baubit.DI.Constraints
{

    public class Singularity<TModule> : AConstraint
    {
        public string ReadableName => string.Empty;

        public Singularity(IConfiguration configuration) : base(string.Empty)
        {
            
        }

        public Singularity() : this(null)
        {

        }

        public override Result Check(List<IModule> modules)
        {
            return modules.Count(mod => mod is TModule) == 1 ? Result.Ok() : Result.Fail(string.Empty).AddReasonIfFailed(new SingularityCheckFailed());
        }
    }
}
