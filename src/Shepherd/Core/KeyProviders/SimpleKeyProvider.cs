using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Shepherd.Core.Models;

namespace Shepherd.Core.KeyProviders
{
    public class SimpleKeyProvider : IKeyProvider
    {
        private readonly IReadOnlyList<string> _keys;

        public SimpleKeyProvider(ShepherdConfiguration configuration)
        {
            _keys = configuration.Unsealing.Simple.Keys;

            if (!_keys.Any())
            {
                throw new ArgumentException("The configuration key 'Unsealing:Simple:Keys' is invalid.");
            }
        }

#pragma warning disable 1998
        public async IAsyncEnumerable<string> GatherKeys([EnumeratorCancellation] CancellationToken cancellationToken = default)
#pragma warning restore 1998
        {
            foreach (var key in _keys)
            {
                yield return key;
            }
        }
    }
}