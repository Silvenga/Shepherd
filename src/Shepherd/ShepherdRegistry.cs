using Lamar;
using Microsoft.Extensions.Hosting;
using Shepherd.Core.DiscoveryProviders;
using Shepherd.Core.Factories;
using Shepherd.Core.KeyProviders;
using Shepherd.Core.Models;
using Shepherd.Core.Services;

namespace Shepherd
{
    public class ShepherdRegistry : ServiceRegistry
    {
        public ShepherdRegistry()
        {
            For<IHostedService>().Use<DiscoveryBackgroundService>();
            For<IHostedService>().Use<UnsealingBackgroundService>();

            For<IKeyProvider>().Use(context => context.GetInstance<KeyProviderFactory>().Create());
            For<IDiscoveryProvider>().Use(context => context.GetInstance<DiscoveryProviderFactory>().Create());
            For<ShepherdConfiguration>().Use(context => context.GetInstance<ShepherdConfigurationFactory>().Create());

            For<ConsulDiscoveryProvider>().Use<ConsulDiscoveryProvider>().Singleton();
        }
    }
}