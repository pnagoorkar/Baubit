namespace Baubit.FileSystem
{
    public static class Operations
    {
        public static CreateDirectory CreateDirectory = CreateDirectory.GetInstance();
        public static DeleteDirectory DeleteDirectory = DeleteDirectory.GetInstance();
        public static CopyFile CopyFile = CopyFile.GetInstance();
        public static ReadFile ReadFile = ReadFile.GetInstance();
    }
}
