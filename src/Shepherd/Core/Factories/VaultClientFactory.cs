using System;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

namespace Shepherd.Core.Factories
{
    public class VaultClientFactory
    {
        private readonly ShepherdConfiguration _configuration;

        public VaultClientFactory(ShepherdConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IVaultClient CreateHaClient()
        {
            var token = new TokenAuthMethodInfo(_configuration.VaultToken);
            var settings = new VaultClientSettings(_configuration.VaultHaAddress.ToString(), token);
            return new VaultClient(settings);
        }

        public IVaultClient CreateSpecificClient(Uri uri)
        {
            var token = new TokenAuthMethodInfo(_configuration.VaultToken);
            var settings = new VaultClientSettings(uri.ToString(), token);
            return new VaultClient(settings);
        }
    }
}