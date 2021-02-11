using System;
using Shepherd.Core.KeyProviders;
using Shepherd.Core.Models;

namespace Shepherd.Core.Factories
{
    public class KeyProviderFactory
    {
        private readonly ShepherdConfiguration _configuration;
        private readonly Func<TransitKeyProvider> _transitKeyProviderFactory;
        private readonly Func<SimpleKeyProvider> _simpleKeyProviderFactory;

        public KeyProviderFactory(ShepherdConfiguration configuration,
                                  Func<TransitKeyProvider> transitKeyProviderFactory,
                                  Func<SimpleKeyProvider> simpleKeyProviderFactory)
        {
            _configuration = configuration;
            _transitKeyProviderFactory = transitKeyProviderFactory;
            _simpleKeyProviderFactory = simpleKeyProviderFactory;
        }

        public IKeyProvider Create()
        {
            var provider = _configuration.Unsealing.Provider?.ToLower();
            switch (provider)
            {
                case "transit":
                    return _transitKeyProviderFactory.Invoke();
                case "simple":
                    return _simpleKeyProviderFactory.Invoke();
                default:
                    throw new ArgumentException($"Unsealing provider '{provider}' is invalid.");
            }
        }
    }
}