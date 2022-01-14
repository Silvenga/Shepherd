using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shepherd.Core.DiscoveryProviders;
using Shepherd.Core.Models;

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
                    await foreach (var update in _discoveryProvider.FetchUpdates(stoppingToken))
                    {
                        var success = false;
                        for (var i = 1; !success && i <= 5; i++)
                        {
                            _logger.LogInformation($"Vault '{update.Vault}' is sealed, attempting to unseal (Attempt: {i}/5).");
                            success = await TryUnseal(update.Vault);
                            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                        }
                    }
                }
                catch (Exception e) when (e is not TaskCanceledException
                                          && e is not OperationCanceledException)
                {
                    _logger.LogWarning(e, "An exception occurred while responding to vault updates.");
                }
            }
        }

        private async Task<bool> TryUnseal(Vault vault)
        {
            try
            {
                await _vaultOperator.ProvideKeys(vault);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "An exception occurred while responding to vault updates.");
                return false;
            }
        }
    }
}