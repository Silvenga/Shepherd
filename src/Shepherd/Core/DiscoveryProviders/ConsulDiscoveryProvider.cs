using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Consul;
using Microsoft.Extensions.Logging;
using Shepherd.Core.Factories;
using Shepherd.Core.Models;

namespace Shepherd.Core.DiscoveryProviders
{
    public class ConsulDiscoveryProvider : IDiscoveryProvider
    {
        private readonly ILogger<ConsulDiscoveryProvider> _logger;
        private readonly ConsulClientFactory _consulClientFactory;
        private readonly Channel<VaultEvent> _channel;
        private readonly Uri _consulAddress;
        private readonly string? _consulServiceName;
        private readonly string? _consulToken;
        private readonly string? _consulDatacenter;

        public ConsulDiscoveryProvider(ILogger<ConsulDiscoveryProvider> logger, ShepherdConfiguration configuration, ConsulClientFactory consulClientFactory)
        {
            _logger = logger;
            _consulClientFactory = consulClientFactory;

            _channel = Channel.CreateBounded<VaultEvent>(new BoundedChannelOptions(64)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });

            _consulServiceName = configuration.Discovery.Consul.ServiceName ?? throw new ArgumentException("Key 'Discovery:Consul:ServiceName' is invalid.");
            _consulAddress = configuration.Discovery.Consul.Address ?? throw new ArgumentException("Key 'Discovery:Consul:Address' is invalid.");
            _consulToken = configuration.Discovery.Consul.Token;
            _consulDatacenter = configuration.Discovery.Consul.Datacenter;
        }

        public async Task Run(CancellationToken cancellationToken = default)
        {
            var consulClient = _consulClientFactory.Create(_consulAddress, _consulToken, _consulDatacenter);

            _logger.LogDebug($"Watching for unhealthy Vault services named '{_consulServiceName}'.");

            var writer = _channel.Writer;

            ulong index = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                var queryOptions = new QueryOptions
                {
                    Consistency = ConsistencyMode.Consistent,
                    WaitIndex = index
                };

                _logger.LogTrace($"Watching for updates after version {index}.");
                var result = await consulClient.Health.Service(_consulServiceName, null, false, queryOptions, cancellationToken);
                if (result == null)
                {
                    continue;
                }

                var unsealedVaults = result.Response
                                           .Where(x => x.Checks.Any(c => c.CheckID.EndsWith(":vault-sealed-check") && c.Status.Equals(HealthStatus.Critical)))
                                           .ToList();
                var hasUnsealedVaults = unsealedVaults.Any();
                if (hasUnsealedVaults)
                {
                    foreach (var service in unsealedVaults.Select(x => x.Service))
                    {
                        var vault = new Vault(new UriBuilder("https", service.Address, service.Port).Uri);

                        _logger.LogInformation($"Vault '{vault}' has been detected as sealed.");

                        var vaultEvent = new UnsealedVaultEvent(vault);
                        await writer.WriteAsync(vaultEvent, cancellationToken);
                    }
                }

                index = result.LastIndex;
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        public IAsyncEnumerable<VaultEvent> FetchUpdates(CancellationToken cancellationToken = default)
        {
            return _channel.Reader.ReadAllAsync(cancellationToken);
        }
    }
}