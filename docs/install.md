# Install on Linux

```bash
wget https://github.com/Silvenga/Shepherd/releases/download/v0.2.0/shepherd-linux-x64

# Install shepherd under the name "vault-shepherd"
sudo install --owner=root --group=root shepherd-linux-x64 /usr/local/bin/vault-shepherd
```

```bash
# Create a system user that shepherd will execute as.
sudo useradd --system --home /etc/vault-shepherd --shell /bin/false vault-shepherd
```

```bash
mkdir /etc/vault-shepherd

# Create the default configuration file.
# Shepherd will automaticlly source and merge json files under /etc/vault-shepherd.
tee /etc/vault-shepherd/settings.json <<EOF
{
  "Unsealing": {
    "Hostname": "vault",
    "Provider": "Transit",
    "Transit": {
      "Auth": {
        "Provider": "AppRole",
        "AppRole": {
          "RoleId": "...",
          "SecretId": "...",
          "MountPath": "approle"
        }
      },
      "Address": "https://vault:8200",
      "KeyName": "autounseal",
      "MountPath": "transit",
      "Hostname": "vault",
      "WrappedKeys": [
        "vault:v1:....",
      ]
    }
  },
  "Discovery": {
    "Provider": "Consul",
    "Consul": {
      "Address": "http://consul:8500",
      "ServiceName": "vault"
    }
  }
}
EOF
```

```bash
# Setup a rather restrictive service unit.
tee /etc/systemd/system/vault-shepherd.service <<EOF
[Unit]
Description="Shepherd for HashiCorp Vault - securely and automatically unseal Vault."
Documentation=https://github.com/Silvenga/Shepherd
Requires=network-online.target
After=network-online.target
ConditionFileNotEmpty=/etc/vault-shepherd/settings.json
StartLimitIntervalSec=60
StartLimitBurst=3

[Service]
User=vault-shepherd
Group=vault-shepherd
ProtectSystem=full
ProtectHome=read-only
PrivateTmp=yes
PrivateDevices=yes
NoNewPrivileges=yes
ExecStart=/usr/local/bin/vault-shepherd
KillMode=process
KillSignal=SIGINT
Restart=on-failure
RestartSec=5
TimeoutStopSec=30
StartLimitInterval=60
StartLimitIntervalSec=60
StartLimitBurst=3

[Install]
WantedBy=multi-user.target
EOF
```

```bash
# If needed, reload systemd.
systemctl daemon-reload --system
```

```bash
# Enable and start the service.
systemctl enable vault-shepherd.service
systemctl start vault-shepherd.service
```