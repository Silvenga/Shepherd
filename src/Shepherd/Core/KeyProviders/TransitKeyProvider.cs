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

        private readonly Uri _address;
        private readonly string _keyName;
        private readonly string _mountPath;
        private readonly IReadOnlyList<string> _wrappedKeys;
        private readonly string? _hostname;

        private readonly VaultAuthConfiguration _auth;

        public TransitKeyProvider(ILogger<TransitKeyProvider> logger, ShepherdConfiguration configuration, VaultClientFactory vaultClientFactory)
        {
            _logger = logger;
            _vaultClientFactory = vaultClientFactory;

            var transit = configuration.Unsealing.Transit;

            _address = transit.Address ?? throw new ArgumentException("Key 'Unsealing:Transit:Address' is invalid.");
            _keyName = transit.KeyName ?? throw new ArgumentException("Key 'Unsealing:Transit:KeyName' is invalid.");
            _mountPath = transit.MountPath ?? throw new ArgumentException("Key 'Unsealing:Transit:MountPath' is invalid.");
            _wrappedKeys = transit.WrappedKeys;
            _hostname = transit.Hostname;

            _auth = transit.Auth;
            vaultClientFactory.AssertValidConfiguration(transit.Auth);

            if (!_wrappedKeys.Any())
            {
                throw new ArgumentException("Key 'Unsealing:Transit:WrappedKeys' is invalid.");
            }
        }

        public async IAsyncEnumerable<string> GatherKeys([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var vaultClient = _vaultClientFactory.CreateClient(_address, _auth, _hostname);

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

                var bytes = Convert.FromBase64String(result.Data.Base64EncodedPlainText);
                var decodedString = Encoding.UTF8.GetString(bytes);
                yield return decodedString;
            }
        }
    }
}