using Baubit.DI;
namespace ConsoleApp
{
    public record MyConfiguration : AConfiguration
    {
        public string MyStringProperty { get; set; }
    }
}
