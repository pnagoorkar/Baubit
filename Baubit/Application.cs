using System.Reflection;

namespace Baubit
{
    public class Application
    {
        public const string PathKey_ExecutingAssemlyLocation = "~ExecutingAssemblyLocation~";

        public static Dictionary<string, string> Paths = new Dictionary<string, string>
        {
            { PathKey_ExecutingAssemlyLocation, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)! },
            {$"~{Environment.SpecialFolder.MyDocuments}~", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) }
        };
    }
}
