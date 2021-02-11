using System;
using Shepherd.Core.DiscoveryProviders;
using Shepherd.Core.Models;

namespace Shepherd.Core.Factories
{
    public class DiscoveryProviderFactory
    {
        private readonly ShepherdConfiguration _configuration;
        private readonly Func<ConsulDiscoveryProvider> _consulDiscoveryProviderFactory;

        public DiscoveryProviderFactory(ShepherdConfiguration configuration,
                                        Func<ConsulDiscoveryProvider> consulDiscoveryProviderFactory)
        {
            _configuration = configuration;
            _consulDiscoveryProviderFactory = consulDiscoveryProviderFactory;
        }

        public IDiscoveryProvider Create()
        {
            var provider = _configuration.Discovery.Provider?.ToLower();
            switch (provider)
            {
                case "consul":
                    return _consulDiscoveryProviderFactory.Invoke();
                default:
                    throw new ArgumentException($"Discovery provider '{provider}' is invalid.");
            }
        }
    }
}