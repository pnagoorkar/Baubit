using Microsoft.Extensions.Hosting;

namespace ConsoleApp
{
    public class MyHostedService : IHostedService
    {
        public MyComponent MyComponent { get; set; }
        public MyHostedService(MyComponent myComponent)
        {
            MyComponent = myComponent;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine($"Component string value: {MyComponent.SomeStringValue}");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
