using FluentResults;
using Microsoft.Extensions.Configuration;

namespace Baubit.Configuration
{
    public static partial class Operations
    {
        public static async Task<Result<IConfiguration>> LoadFromJsonFileAsync(ConfiguratonLoadContext context)
        {
            return await Result.Try((Func<Task<IConfiguration>>)(async () =>
            {
                await Task.Yield();
                return new ConfigurationBuilder().AddJsonFile(context.JsonFilePath).Build();
            }));
        }
    }

    public class ConfiguratonLoadContext
    {
        public string JsonFilePath { get; init; }
        public ConfiguratonLoadContext(string jsonFilePath)
        {
            JsonFilePath = jsonFilePath;
        }

    }
}
