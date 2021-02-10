using System.Collections.Generic;
using System.Threading;

namespace Shepherd.Core.KeyProviders
{
    public interface IKeyProvider
    {
        IAsyncEnumerable<string> GatherKeys(CancellationToken cancellationToken = default);
    }
}