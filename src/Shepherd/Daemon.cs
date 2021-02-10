using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shepherd.Core;

namespace Shepherd
{
    public class Daemon : BackgroundService
    {
        private readonly ILogger<Daemon> _logger;
        private readonly ShepherdConfiguration _configuration;
        private readonly VaultOperator _vaultOperator;

        public Daemon(ILogger<Daemon> logger, ShepherdConfiguration configuration, VaultOperator vaultOperator)
        {
            _logger = logger;
            _configuration = configuration;
            _vaultOperator = vaultOperator;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            IReadOnlyList<Vault> vaultMembers = _configuration.VaultMembers.Select(x => new Vault(x)).ToList();
            _logger.LogInformation($"There are {vaultMembers.Count} members configured ({string.Join(", ", vaultMembers)}).");

            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var vault in vaultMembers)
                {
                    _logger.LogTrace($"Processing vault '{vault}'.");

                    var vaultStatus = await _vaultOperator.Status(vault);
                    if (vaultStatus.IsSealed)
                    {
                        _logger.LogInformation($"Vault '{vaultStatus.Vault}' is sealed, attempting to unseal.");
                        await _vaultOperator.ProvideKeys(vault);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }
    }
}