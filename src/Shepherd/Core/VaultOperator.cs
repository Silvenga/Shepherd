using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shepherd.Core.Factories;
using Shepherd.Core.KeyProviders;

namespace Shepherd.Core
{
    public record Vault(Uri Address)
    {
        public override string ToString()
        {
            return Address.ToString();
        }
    }

    public record VaultStatus(Vault Vault, bool IsSealed);

    public class VaultOperator
    {
        private readonly ILogger<VaultOperator> _logger;
        private readonly IKeyProvider _keyProvider;
        private readonly VaultClientFactory _vaultClientFactory;

        public VaultOperator(ILogger<VaultOperator> logger, IKeyProvider keyProvider, VaultClientFactory vaultClientFactory)
        {
            _logger = logger;
            _keyProvider = keyProvider;
            _vaultClientFactory = vaultClientFactory;
        }

        public async Task<VaultStatus> Status(Vault vault)
        {
            var client = _vaultClientFactory.CreateSpecificClient(vault.Address);
            var status = await client.V1.System.GetSealStatusAsync();
            return new VaultStatus(vault, status.Sealed);
        }

        public async Task ProvideKeys(Vault vault)
        {
            _logger.LogInformation($"Attempting to unseal vault '{vault}' using known keys.");
            var client = _vaultClientFactory.CreateSpecificClient(vault.Address);

            var status = await client.V1.System.GetSealStatusAsync();
            var index = 0;

            await foreach (var key in _keyProvider.GatherKeys())
            {
                index++;

                if (!status.Sealed)
                {
                    continue;
                }

                _logger.LogInformation($"Providing key {index}.");
                status = await client.V1.System.UnsealAsync(key);
                _logger.LogInformation($"Vault now has {status.Progress}/{status.SecretThreshold} keys to unseal.");
            }

            _logger.LogInformation(status.Sealed
                ? $"All known keys were attempted. Vault still needs {status.SecretThreshold - status.Progress} keys to unseal."
                : "Vault is now unsealed.");
        }
    }
}