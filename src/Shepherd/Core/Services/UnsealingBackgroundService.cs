using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shepherd.Core.DiscoveryProviders;

namespace Shepherd.Core.Services
{
    public class UnsealingBackgroundService : BackgroundService
    {
        private readonly ILogger<UnsealingBackgroundService> _logger;
        private readonly VaultOperator _vaultOperator;
        private readonly IDiscoveryProvider _discoveryProvider;

        public UnsealingBackgroundService(ILogger<UnsealingBackgroundService> logger,
                                          VaultOperator vaultOperator,
                                          IDiscoveryProvider discoveryProvider)
        {
            _logger = logger;
            _vaultOperator = vaultOperator;
            _discoveryProvider = discoveryProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await foreach (var updates in _discoveryProvider.FetchUpdates(stoppingToken))
                    {
                        var vault = updates.Vault;

                        _logger.LogInformation($"Vault '{vault}' is sealed, attempting to unseal.");
                        await _vaultOperator.ProvideKeys(vault);
                    }
                }
                catch (Exception e) when (e is not TaskCanceledException 
                                          && e is not OperationCanceledException)
                {
                    _logger.LogWarning(e, "An exception occurred while responding to vault updates.");
                }
            }
        }
    }
}