using Microsoft.Extensions.DependencyInjection;
using Shepherd.Core;
using Shepherd.Core.Factories;
using Shepherd.Core.KeyProviders;

namespace Shepherd
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddHostedService<Daemon>();

            services.AddSingleton(provider => provider.GetRequiredService<ShepherdConfigurationFactory>().Create());
            services.AddSingleton(provider => provider.GetRequiredService<VaultClientFactory>().CreateHaClient());

            services.AddTransient<ShepherdConfigurationFactory>();
            services.AddTransient<VaultClientFactory>();
            services.AddTransient<VaultOperator>();
            services.AddTransient<IKeyProvider, TransitKeyProvider>();
        }
    }
}