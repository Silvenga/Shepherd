using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Shepherd.Core.Factories
{
    public class ShepherdConfigurationFactory
    {
        private readonly IConfiguration _configuration;

        public ShepherdConfigurationFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public ShepherdConfiguration Create()
        {
            return new(
                _configuration.GetValue<string>("vault_token"),
                _configuration.GetValue<Uri>("vault_ha_address"),
                _configuration.GetSection("vault_member_addresses").Get<List<Uri>>(),
                _configuration.GetSection("wrapped_unsealing_keys").Get<List<string>>(),
                _configuration.GetValue<string>("transit_key_name"),
                _configuration.GetValue<string?>("expected_vault_common_name")
            );
        }
    }
}