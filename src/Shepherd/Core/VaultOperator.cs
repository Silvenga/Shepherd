using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shepherd.Core.Factories;
using Shepherd.Core.KeyProviders;
using Shepherd.Core.Models;

namespace Shepherd.Core
{
    public class VaultOperator
    {
        private readonly ILogger<VaultOperator> _logger;
        private readonly IKeyProvider _keyProvider;
        private readonly VaultClientFactory _vaultClientFactory;
        private readonly string? _hostname;

        public VaultOperator(ILogger<VaultOperator> logger, IKeyProvider keyProvider, VaultClientFactory vaultClientFactory,
                             ShepherdConfiguration configuration)
        {
            _logger = logger;
            _keyProvider = keyProvider;
            _vaultClientFactory = vaultClientFactory;

            _hostname = configuration.Unsealing.Hostname;
        }

        public async Task ProvideKeys(Vault vault)
        {
            _logger.LogInformation($"Attempting to unseal vault '{vault}' using known keys.");
            var client = _vaultClientFactory.CreateClient(vault.Address, _hostname);

            var status = await client.V1.System.GetSealStatusAsync();
            var index = 0;

            await foreach (var key in _keyProvider.GatherKeys())
            {
                index++;

                if (!status.Sealed)
                {
                    continue;
                }

                _logger.LogInformation($"Providing key {index}. "
                                       + $"Vault needs {status.Progress}/{status.SecretThreshold} keys to unseal.");
                status = await client.V1.System.UnsealAsync(key);
            }

            _logger.LogInformation(status.Sealed
                ? $"All known keys were attempted. Vault still needs {status.SecretThreshold - status.Progress} keys to unseal."
                : "Vault is now unsealed.");
        }
    }
}