using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Shepherd.Core.KeyProviders
{
    public class SimpleKeyProvider : IKeyProvider
    {
        private readonly ShepherdConfiguration _configuration;

        public SimpleKeyProvider(ShepherdConfiguration configuration)
        {
            _configuration = configuration;
        }

#pragma warning disable 1998
        public async IAsyncEnumerable<string> GatherKeys([EnumeratorCancellation] CancellationToken cancellationToken = default)
#pragma warning restore 1998
        {
            var wrappedKeys = _configuration.WrappedUnsealingKeys;
            foreach (var key in wrappedKeys)
            {
                yield return key;
            }
        }
    }
}