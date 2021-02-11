using Lamar;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shepherd.Core.DiscoveryProviders;
using Shepherd.Core.Factories;

namespace Shepherd
{
    public class ShepherdRegistry : ServiceRegistry
    {
        public ShepherdRegistry()
        {
            Scan(scanner =>
            {
                scanner.TheCallingAssembly();
                scanner.AddAllTypesOf<IHostedService>();
            });

            this.AddTransient(provider => provider.GetRequiredService<KeyProviderFactory>().Create());
            this.AddTransient(provider => provider.GetRequiredService<DiscoveryProviderFactory>().Create());
            this.AddTransient(provider => provider.GetRequiredService<ShepherdConfigurationFactory>().Create());

            For<ConsulDiscoveryProvider>().Use<ConsulDiscoveryProvider>().Singleton();
        }
    }
}