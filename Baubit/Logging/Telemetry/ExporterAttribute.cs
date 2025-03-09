namespace Baubit.Logging.Telemetry
{
    [AttributeUsage(validOn: AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ExporterAttribute : Attribute
    {
        public required string Id { get; init; }
    }
}
