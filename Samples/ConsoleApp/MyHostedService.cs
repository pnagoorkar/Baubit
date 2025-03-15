using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConsoleApp
{
    public class MyHostedService : IHostedService
    {
        public MyComponent MyComponent { get; set; }
        private readonly ILogger<MyHostedService> _logger;
        public MyHostedService(MyComponent myComponent, ILogger<MyHostedService> logger)
        {
            MyComponent = myComponent;
            _logger = logger;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Component string value: {MyComponent.SomeStringValue}");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
