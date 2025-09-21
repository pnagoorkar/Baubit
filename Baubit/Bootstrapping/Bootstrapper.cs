using Microsoft.Extensions.Hosting;

namespace Baubit.Bootstrapping
{
    public class Bootstrapper : IHostedService
    {
        public Bootstrapper(IEnumerable<IBootstrap> bootstraps)
        {
            
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public virtual void Bootstrap()
        {

        }
    }
}
