# PaddyWise.Api — configuration

`appsettings.json` is tracked by git and holds **no secrets**. Two values are deliberately
left as empty strings so the file still documents the configuration shape:

```jsonc
"Jwt":               { "Key": "" }
"ConnectionStrings": { "DefaultConnection": "" }
```

Supply both at runtime. The app throws on startup if `Jwt:Key` is missing or shorter than
32 characters.

## Local development — .NET user-secrets

Secrets live outside the repo, in the per-user secret store keyed by the `UserSecretsId`
in `PaddyWise.Api.csproj`. Run from this directory:

```bash
dotnet user-secrets set "Jwt:Key" "<your-64-char-secret>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your-npgsql-connection-string>"
```

Useful companions: `dotnet user-secrets list`, `dotnet user-secrets remove "Jwt:Key"`,
`dotnet user-secrets clear`.

## Deployment — environment variables

The configuration provider maps a nested key to an environment variable by replacing `:`
with a double underscore `__`:

| Configuration key                     | Environment variable                    |
|---------------------------------------|-----------------------------------------|
| `Jwt:Key`                             | `Jwt__Key`                              |
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection`  |

```bash
export Jwt__Key="<your-64-char-secret>"
export ConnectionStrings__DefaultConnection="Host=...;Database=...;Username=...;Password=...;SslMode=Require"
```

Set these through the host's secret manager (App Service application settings, container
secrets, CI/CD variables) rather than a checked-in file. Precedence is
appsettings.json → user-secrets (Development only) → environment variables, so an
environment variable always wins.

`Jwt:Issuer`, `Jwt:Audience`, `Jwt:AccessTokenExpiryMinutes` and `Jwt:RefreshTokenExpiryDays`
are not secrets and stay in `appsettings.json`.
