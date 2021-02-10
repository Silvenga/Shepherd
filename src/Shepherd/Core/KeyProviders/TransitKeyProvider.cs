using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using VaultSharp;
using VaultSharp.V1.SecretsEngines.Transit;

namespace Shepherd.Core.KeyProviders
{
    public class TransitKeyProvider : IKeyProvider
    {
        private readonly ILogger<TransitKeyProvider> _logger;
        private readonly ShepherdConfiguration _configuration;
        private readonly IVaultClient _vaultClient;

        public TransitKeyProvider(ILogger<TransitKeyProvider> logger, ShepherdConfiguration configuration, IVaultClient vaultClient)
        {
            _logger = logger;
            _configuration = configuration;
            _vaultClient = vaultClient;
        }

        public async IAsyncEnumerable<string> GatherKeys([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Gathering keys using the Transit provider.");

            var index = 0;
            var wrappedKeys = _configuration.WrappedUnsealingKeys;
            foreach (var wrappedKey in wrappedKeys)
            {
                index++;
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug($"Decrypting key {index}.");
                var result = await _vaultClient.V1.Secrets.Transit.DecryptAsync(_configuration.TransitKeyName, new DecryptRequestOptions
                {
                    CipherText = wrappedKey
                });

                foreach (var warning in result?.Warnings ?? Enumerable.Empty<string>())
                {
                    _logger.LogWarning($"Got warning '{warning}' from Vault during Transit decryption.");
                }

                byte[] bytes = Convert.FromBase64String(result.Data.Base64EncodedPlainText);
                string decodedString = Encoding.UTF8.GetString(bytes);
                yield return decodedString;
            }
        }
    }
}