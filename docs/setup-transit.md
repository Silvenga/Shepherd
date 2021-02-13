# Setup the key provider via Transit

## Setup Trasit

```bash
# Enable the engine.
vault secrets enable transit

# Create a new key called "autounseal". This key will be used later by name.
vault write -f transit/keys/autounseal
```

## Setup the Policy

```bash
# Create a policy with the transit key created above.
# Allow decrypting secrets.
tee autounseal.hcl <<EOF
path "transit/decrypt/autounseal" {
   capabilities = [ "update" ]
}
EOF
vault policy write autounseal autounseal.hcl
```

## Setup AppRole

```bash
# Enable the auth provider.
vault auth enable approle

# Create a new AppRole with the autounseal policy.
vault write auth/approle/role/shepherd \
    token_ttl=5m \
    token_max_ttl=5m \
    token_policies=autounseal

# Get the role-id.
vault read auth/approle/role/shepherd/role-id

# And finally, get the secret-id.
vault write -f auth/approle/role/shepherd/secret-id

# Shepherd is stateless and cannot store secrets.
# Therefore, it must operate in AppRole push mode.
```
