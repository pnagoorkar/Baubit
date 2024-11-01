using Microsoft.Extensions.Hosting;

namespace Baubit.Hosting
{
    public static class HostedServiceExtensions
    {
        public static void SwitchState(this IHostedService hostedService, bool start, CancellationToken cancellationToken)
        {
            switch (start)
            {
                case true:
                    hostedService.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
                    break;
                case false:
                    hostedService.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
                    break;
            }
        }
    }
}
