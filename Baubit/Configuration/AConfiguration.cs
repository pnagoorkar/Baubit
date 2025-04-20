using Baubit.Reflection;
using Baubit.Traceability;
using Baubit.Validation;
using Baubit.Validation.Reasons;
using FluentResults;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Baubit.Configuration
{
    public abstract class AConfiguration : IValidatable
    {
        public List<string> ValidatorKeys { get; init; } = new List<string>();

        private List<Type> validatorTypes;

        [JsonIgnore]
        public List<Type> ValidatorTypes 
        {
            get
            {
                if (validatorTypes == null)
                {
                    validatorTypes = ValidatorKeys.Select(key => TypeResolver.TryResolveTypeAsync(key).ThrowIfFailed().Value).ToList();
                }
                return validatorTypes;
            }
        }
    }

    public static class ConfigurationExtensions
    {
        public static Result<TConfiguration> Load<TConfiguration>(this IConfiguration iConfiguration) where TConfiguration : AConfiguration
        {
            return Result.Try(() => iConfiguration.Get<TConfiguration>() ?? Activator.CreateInstance<TConfiguration>()!)
                         .Bind(config => config.ExpandURIs())
                         .Bind(config => config.CheckIfValidatorsExist())
                         .Bind(config => Result.Try(() => config.ValidatorTypes.Aggregate(Result.Ok(config), (seed, next) => seed.Bind(cfg => cfg.TryValidate(next)).WithReasons(seed.Reasons).ThrowIfFailed()).ThrowIfFailed()))
                         .Bind(res => res);
        }

        public static Result<TConfiguration> CheckIfValidatorsExist<TConfiguration>(this TConfiguration config) where TConfiguration : AConfiguration
        {
            var retVal = Result.Ok(config);
            if(config.ValidatorTypes.Count == 0)
            {
                retVal.WithReason(new NoValidatorsDefined());
            }
            return retVal;
        }

        // convert AConfiguration to its most specific type and then serialize
        public static string SerializeJson(this AConfiguration configuration, JsonSerializerOptions options)
        {
            var specificConfiguration = Convert.ChangeType(configuration, configuration.GetType());
            return JsonSerializer.Serialize(specificConfiguration, options);
        }
    }
}
