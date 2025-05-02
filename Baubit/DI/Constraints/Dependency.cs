using Baubit.DI.Constraints.Reasons;
using Baubit.Traceability;
using FluentResults;
using Microsoft.Extensions.Configuration;

namespace Baubit.DI.Constraints
{
    public class Dependency : AConstraint
    {
        public Type[] ModuleTypes { get; init; }
        public Dependency(params Type[] moduleTypes) : base(string.Empty)
        {
            Result.OkIf(moduleTypes.All(type => type.IsAssignableTo(typeof(IModule))), new Error(string.Empty))
                  .AddReasonIfFailed(new DependencyTypeMustBeAModule())
                  .ThrowIfFailed();
            ModuleTypes = moduleTypes;
        }

        public Dependency(IConfiguration configuration) : this(moduleTypes: [])
        {
            
        }

        public override Result Check(List<IModule> modules)
        {
            return ModuleTypes.Aggregate(Result.Ok(), (seed, next) => seed.Bind(() => Result.OkIf(modules.Any(m => m.GetType().IsAssignableTo(next)), new Error(string.Empty))
                                                                                            .AddReasonIfFailed(new DependencyCheckFailed(next))));
        }
    }
}
