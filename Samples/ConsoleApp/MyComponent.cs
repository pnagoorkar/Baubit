using Microsoft.Extensions.Logging;

namespace ConsoleApp
{
    public class MyComponent
    {
        public string SomeStringValue { get; set; }
        public MyComponent(string someStringValue, ILogger<MyComponent> logger)
        {
            SomeStringValue = someStringValue;
        }
    }
}
