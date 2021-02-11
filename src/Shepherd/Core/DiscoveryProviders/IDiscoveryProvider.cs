using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shepherd.Core.Models;

namespace Shepherd.Core.DiscoveryProviders
{
    public interface IDiscoveryProvider
    {
        Task Run(CancellationToken cancellationToken = default);
        IAsyncEnumerable<VaultEvent> FetchUpdates(CancellationToken cancellationToken = default);
    }
}