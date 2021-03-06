﻿using System;
using System.Collections.Generic;

namespace Shepherd.Core.Models
{
    public class ShepherdConfiguration
    {
        public UnsealingConfiguration Unsealing { get; set; } = new();

        public DiscoveryConfiguration Discovery { get; set; } = new();
    }

    public class UnsealingConfiguration
    {
        public string? Provider { get; set; } = "transit";
        public string? Hostname { get; set; }
        public TransitUnsealingConfiguration Transit { get; set; } = new();
        public SimpleUnsealingConfiguration Simple { get; set; } = new();
    }

    public class TransitUnsealingConfiguration
    {
        public VaultAuthConfiguration Auth { get; set; } = new();
        public Uri? Address { get; set; }
        public string? KeyName { get; set; } = "autounseal";
        public string? MountPath { get; set; } = "transit";
        public string? Hostname { get; set; }
        public List<string> WrappedKeys { get; set; } = new();
    }

    public class VaultAuthConfiguration
    {
        public string? Provider { get; set; } = "AppRole";

        public AppRoleVaultAuthConfiguration AppRole { get; set; } = new();
    }

    public class AppRoleVaultAuthConfiguration
    {
        public string? RoleId { get; set; }

        public string? SecretId { get; set; }

        public string? MountPath { get; set; } = "approle";
    }

    public class SimpleUnsealingConfiguration
    {
        public List<string> Keys { get; set; } = new();
    }

    public class DiscoveryConfiguration
    {
        public string? Provider { get; set; } = "consul";
        public ConsulDiscoveryConfiguration Consul { get; set; } = new();
    }

    public class ConsulDiscoveryConfiguration
    {
        public Uri? Address { get; set; }
        public string? ServiceName { get; set; }
        public string? Token { get; set; }
        public string? Datacenter { get; set; }
    }
}