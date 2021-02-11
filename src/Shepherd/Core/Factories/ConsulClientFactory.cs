using System;
using Consul;

namespace Shepherd.Core.Factories
{
    public class ConsulClientFactory
    {
        public IConsulClient Create(Uri address, string? token, string? datacenter)
        {
            return new ConsulClient(configuration =>
            {
                configuration.Address = address;
                configuration.Token = token;
                configuration.Datacenter = datacenter;
            });
        }
    }
}