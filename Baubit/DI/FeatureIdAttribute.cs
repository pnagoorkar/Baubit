namespace Baubit.DI
{
    public class FeatureIdAttribute : Attribute
    {
        public string Function { get; init; }
        public string Variant { get; init; }
        public FeatureIdAttribute(string function, string variant)
        {
            Function = function;
            Variant = variant;
        }
    }
}
