namespace Baubit.Validation
{
    /// <summary>
    /// To be used on validator classes for a given module / module configuration
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ValidatorAttribute : Attribute
    {
        public string Key { get; init; } = string.Empty;
    }
}
