using Baubit.DI;
namespace WebApp
{
    public record MyConfiguration : AConfiguration
    {
        public string MyStringProperty { get; set; }
    }
}
