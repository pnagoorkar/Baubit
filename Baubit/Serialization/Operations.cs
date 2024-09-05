namespace Baubit.Serialization
{
    public static class Operations<T>
    {
        public static DeserializeXMLFromFile<T> DeserializeXMLFromFile = DeserializeXMLFromFile<T>.GetInstance();
        public static DeserializeXMLString<T> DeserializeXMLString = DeserializeXMLString<T>.GetInstance();
    }
}
