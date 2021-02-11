using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shepherd.Core.DiscoveryProviders;

namespace Shepherd.Core.Services
{
    public class DiscoveryBackgroundService : BackgroundService
    {
        private readonly ILogger<DiscoveryBackgroundService> _logger;
        private readonly IDiscoveryProvider _discoveryProvider;

        public DiscoveryBackgroundService(ILogger<DiscoveryBackgroundService> logger, IDiscoveryProvider discoveryProvider)
        {
            _logger = logger;
            _discoveryProvider = discoveryProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _discoveryProvider.Run(stoppingToken);
                }
                catch (Exception e) when (e is not TaskCanceledException)
                {
                    _logger.LogWarning(e, "An exception occurred while monitoring for vault changes.");
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
        }
    }
}