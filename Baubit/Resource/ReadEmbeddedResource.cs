using FluentResults;
using System.Reflection;

namespace Baubit.Resource
{
    public static partial class  Operations
    {
        public static async Task<Result<string>> ReadEmbeddedResourceAsync(EmbeddedResourceReadContext context)
        {
            try
            {
                using (Stream stream = context.Assembly.GetManifestResourceStream(context.FullyQualifiedResourceName)!)
                {
                    if (stream == null)
                    {
                        return Result.Fail(new ResourceNotFound());
                    }

                    using (StreamReader reader = new StreamReader(stream))
                    {
                        return Result.Ok(await reader.ReadToEndAsync());
                    }
                }
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }
    }

    public class EmbeddedResourceReadContext
    {
        public string FullyQualifiedResourceName { get; init; }
        public Assembly Assembly { get; init; }

        public EmbeddedResourceReadContext(string fullyQualifiedResourceName, Assembly assembly)
        {
            FullyQualifiedResourceName = fullyQualifiedResourceName;
            Assembly = assembly;
        }
    }

    public class ResourceNotFound : IError
    {
        public List<IError> Reasons { get; }

        public string Message => "Resource not found !";

        public Dictionary<string, object> Metadata { get; }
    }
}
