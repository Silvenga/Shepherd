using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using Shepherd.Core.Factories;
using Shepherd.Core.Models;
using VaultSharp.V1.SecretsEngines.Transit;

namespace Shepherd.Core.KeyProviders
{
    public class TransitKeyProvider : IKeyProvider
    {
        private readonly ILogger<TransitKeyProvider> _logger;
        private readonly VaultClientFactory _vaultClientFactory;

        private readonly IReadOnlyList<string> _wrappedKeys;
        private readonly string _token;
        private readonly string _mountPath;
        private readonly string _keyName;
        private readonly string? _hostname;
        private readonly Uri _address;

        public TransitKeyProvider(ILogger<TransitKeyProvider> logger, ShepherdConfiguration configuration, VaultClientFactory vaultClientFactory)
        {
            _logger = logger;
            _vaultClientFactory = vaultClientFactory;

            _address = configuration.Unsealing.Transit.Address ?? throw new ArgumentException("Key 'Unsealing:Transit:Address' is invalid.");
            _keyName = configuration.Unsealing.Transit.KeyName ?? throw new ArgumentException("Key 'Unsealing:Transit:KeyName' is invalid.");
            _mountPath = configuration.Unsealing.Transit.MountPath ?? throw new ArgumentException("Key 'Unsealing:Transit:MountPath' is invalid.");
            _token = configuration.Unsealing.Transit.Token ?? throw new ArgumentException("Key 'Unsealing:Transit:Token' is invalid.");
            _wrappedKeys = configuration.Unsealing.Transit.WrappedKeys;
            _hostname = configuration.Unsealing.Transit.Hostname;

            if (!_wrappedKeys.Any())
            {
                throw new ArgumentException("Key 'Unsealing:Transit:WrappedKeys' is invalid.");
            }
        }

        public async IAsyncEnumerable<string> GatherKeys([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var vaultClient = _vaultClientFactory.CreateClient(_address, _token, _hostname);

            var index = 0;
            foreach (var wrappedKey in _wrappedKeys)
            {
                index++;
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug($"Decrypting key {index} using the Transit provider.");
                var result = await vaultClient.V1.Secrets.Transit.DecryptAsync(_keyName, new DecryptRequestOptions
                {
                    CipherText = wrappedKey
                }, _mountPath);

                foreach (var warning in result.Warnings ?? Enumerable.Empty<string>())
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