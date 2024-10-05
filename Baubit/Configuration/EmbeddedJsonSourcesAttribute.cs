namespace Baubit.Configuration
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class EmbeddedJsonSourcesAttribute : Attribute
    {
        public List<string> Values { get; init; }
        public EmbeddedJsonSourcesAttribute(params string[] values)
        {
            Values = values.ToList();
        }
    }
}
