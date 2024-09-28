//using FluentResults;

//namespace Baubit.FileSystem
//{
//    public static partial class Operations
//    {
//        public static async Task<Result<string>> ReadFileAsync(FileReadContext context)
//        {
//            if (!File.Exists(context.Path))
//            {
//                return Result.Fail(new FileDoesNotExist(context));
//            }
//            return await Result.Try(() => File.ReadAllTextAsync(context.Path));
//        }
//    }

//    public class FileReadContext
//    {
//        public string Path { get; init; }
//        public FileReadContext(string path)
//        {
//            Path = path;
//        }
//    }

//    public class FileDoesNotExist : IError
//    {
//        public List<IError> Reasons { get; }

//        public string Message { get; } = "File does not exist !";

//        public Dictionary<string, object> Metadata { get; }

//        public FileReadContext FileReadContext { get; init; }

//        public FileDoesNotExist(FileReadContext fileReadContext)
//        {
//            FileReadContext  = fileReadContext;
//        }
//    }
//}
