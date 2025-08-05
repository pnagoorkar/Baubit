namespace Baubit.DI
{
    public class FeatureIdAttribute : Attribute
    {
        public string Value { get; init; }
        public FeatureIdAttribute(string value)
        {
            Value = value;
        }
    }
}
