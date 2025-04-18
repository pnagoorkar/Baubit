using FluentResults;
using System.Data;
using System.Linq.Expressions;
using System.Text;

namespace Baubit.Validation
{
    public abstract class AValidator<T> : IValidator<T>
    {
        public static Dictionary<string, AValidator<T>> CurrentValidators { get; set; } = new Dictionary<string, AValidator<T>>();

        static AValidator()
        {
            var kvps = AppDomain.CurrentDomain
                                .GetAssemblies()
                                .SelectMany(assembly => assembly.GetTypes()
                                                                .Where(type => type.IsClass &&
                                                                               type.IsPublic &&
                                                                               !type.IsAbstract &&
                                                                type.IsSubclassOf(typeof(AValidator<T>)) &&
                                                                               type.CustomAttributes.Any(attribute => attribute.AttributeType.Equals(typeof(ValidatorAttribute))))
                                                                .Select(type => ((AValidator<T>)Activator.CreateInstance(type)).AsValidatorKVP()));
            var redundantKvps = kvps.GroupBy(kvp => kvp.Value.Key)
                                    .Where(group => group.Count() > 1)
                                    .SelectMany(group => group);

            if (redundantKvps.Count() > 0)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("Validators with redundant Keys found !").AppendLine("All validators for a given type are required to have a unique validator key");
                foreach (var redundantKvp in redundantKvps)
                {
                    stringBuilder.AppendLine($"{redundantKvp.Value.Key}:{redundantKvp.Value.Value.GetType().AssemblyQualifiedName}");
                }
                throw new InvalidDataException(stringBuilder.ToString());
            }

            foreach (var kvp in kvps)
            {
                if (kvp != null)
                {
                    CurrentValidators.Add(kvp.Value.Key, kvp.Value.Value);
                }
            }
        }

        private List<Expression<Func<T, Result>>> _rules;
        protected AValidator()
        {
            _rules = GetRules().ToList();
        }

        protected abstract IEnumerable<Expression<Func<T, Result>>> GetRules();

        public Result<T> Validate(T value) => Result.Merge(_rules.Select(rule => rule.Compile()(value)).ToArray()).Bind(() => Result.Ok(value));
    }
    public static class ValidatorExtensions
    {
        public static KeyValuePair<string, AValidator<T>>? AsValidatorKVP<T>(this AValidator<T> validator)
        {
            var validatorAttribute = (ValidatorAttribute)validator.GetType().GetCustomAttributes(typeof(ValidatorAttribute), false).FirstOrDefault();

            if (validatorAttribute != null)
            {
                return new KeyValuePair<string, AValidator<T>>(validatorAttribute.Key, validator);
            }

            return null;
        }
    }
}