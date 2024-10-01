using Baubit.Compression;

namespace Baubit.Store
{
    public class NupkgFile : AArchive
    {
        public NupkgFile(string filePath) : base(filePath)
        {
        }
    }

    public static class NupkgFileExtensions
    {
        public static Version GetAssemblyVersion(this NupkgFile nupkgFile, string namePart)
        {
            string versionString = Path.GetFileNameWithoutExtension(nupkgFile.FilePath)
                                        .Trim()
                                        .Substring(namePart.Trim().Length + 1);
            return new Version(versionString);
        }
    }
}
