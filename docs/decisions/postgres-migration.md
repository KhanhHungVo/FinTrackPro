# PostgreSQL Migration Plan

Migrate FinTrackPro from Azure SQL (hit free-tier limit) to PostgreSQL on Render free tier, while keeping SQL Server as a supported local-dev option. All Render resources are re-provisioned from scratch via Terraform.

---

## Overview

| Concern | Decision |
|---|---|
| Migration strategy | Single provider-agnostic migration set (no folder split) |
| Local dev default | SQL Server (unchanged, `docker compose up -d sqlserver keycloak`) |
| Production | PostgreSQL on Render, auto-provisioned by Terraform |
| DB lifecycle | `ignore_changes = all` + `prevent_destroy = true` — created once, never modified by Terraform |
| Connection string source | `render_postgres.db.connection_info.internal_connection_string` injected into API env vars |

---

## Part A — Backend

### A1. Packages

**`FinTrackPro.Infrastructure.csproj`**
```xml
<!-- Add alongside existing SqlServer package -->
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.*" />
```

**`FinTrackPro.API.csproj`**
```xml
<PackageReference Include="Hangfire.PostgreSql" Version="1.*" />
```

---

### A2. Config key

`appsettings.json` — add `DatabaseProvider` section. Default is `sqlserver` to preserve existing local dev:

```json
"DatabaseProvider": {
  "Provider": "sqlserver"
},
```

Override for production via env var: `DatabaseProvider__Provider=postgresql`.

---

### A3. `DependencyInjection.cs` — conditional EF Core provider

```csharp
var dbProvider = configuration["DatabaseProvider:Provider"] ?? "sqlserver";
services.AddDbContext<ApplicationDbContext>(options =>
{
    if (dbProvider == "postgresql")
        options.UseNpgsql(
            configuration.GetConnectionString("DefaultConnection"),
            b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
    else
        options.UseSqlServer(
            configuration.GetConnectionString("DefaultConnection"),
            b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
});
```

---

### A4. `Program.cs` — conditional Hangfire storage

```csharp
var dbProvider = builder.Configuration["DatabaseProvider:Provider"] ?? "sqlserver";
builder.Services.AddHangfire(config =>
{
    config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings();

    if (dbProvider == "postgresql")
        config.UsePostgreSqlStorage(c =>
            c.UseNpgsqlConnection(
                builder.Configuration.GetConnectionString("DefaultConnection")));
    else
        config.UseSqlServerStorage(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout       = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout   = TimeSpan.FromMinutes(5),
                QueuePollInterval            = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks           = true
            });
});
```

---

### A5. Entity configurations — `HasPrecision` instead of `HasColumnType`

`HasColumnType("decimal(18,2)")` is valid on both SQL Server and PostgreSQL, but `HasPrecision` is the EF Core idiomatic way and avoids snapshot drift when switching providers.

| File | Change |
|---|---|
| `BudgetConfiguration.cs` | `.HasColumnType("decimal(18,2)")` → `.HasPrecision(18, 2)` |
| `TradeConfiguration.cs` | `.HasColumnType("decimal(18,8)")` × 4 → `.HasPrecision(18, 8)` |
| `SignalConfiguration.cs` | `.HasColumnType("decimal(18,8)")` → `.HasPrecision(18, 8)` |
| `TransactionConfiguration.cs` | `.HasColumnType("decimal(18,2)")` → `.HasPrecision(18, 2)` |

---

### A6. Provider-agnostic migration

The existing `InitialCreate.cs` migration has hardcoded SQL Server type strings that fail on PostgreSQL:

| SQL Server string | Correct approach |
|---|---|
| `type: "uniqueidentifier"` | Remove — provider maps `Guid` automatically |
| `type: "nvarchar(X)"` | Remove — `maxLength:` already constrains the column |
| `type: "datetime2"` | Remove — provider maps `DateTime` automatically |
| `type: "bit"` | Remove — provider maps `bool` automatically |
| `type: "int"` | Remove — provider maps `int` automatically |
| `type: "decimal(18,2)"` | Replace with `precision: 18, scale: 2` |
| `type: "decimal(18,8)"` | Replace with `precision: 18, scale: 8` |

Result: EF Core applies the migration using whichever provider is active, generating correct column types for each.

**Why a single migration folder works:** EF Core's migration runner reads the migration history table from the actual database. A fresh PostgreSQL DB has no history — all migrations are applied using the Npgsql type mapper. An existing SQL Server DB's history already records `InitialCreate` as applied — it won't re-run.

---

### A7. Model snapshot

`ApplicationDbContextModelSnapshot.cs` has SQL Server-specific annotations that cause spurious migrations when running `dotnet ef migrations add` under PostgreSQL.

Remove:
- `SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder)`
- `.HasAnnotation("Relational:MaxIdentifierLength", 128)` — SQL Server = 128, PostgreSQL = 63; let the provider set this
- All `.HasColumnType("uniqueidentifier")`, `.HasColumnType("nvarchar(X)")`, `.HasColumnType("datetime2")`, `.HasColumnType("bit")`, `.HasColumnType("int")`

Change:
- `.HasColumnType("decimal(18,2)")` → `.HasPrecision(18, 2)`
- `.HasColumnType("decimal(18,8)")` → `.HasPrecision(18, 8)`

---

### A8. `docker-compose.yml` — add PostgreSQL service

Add a `postgres` service alongside the existing `sqlserver` service for developers who prefer local PostgreSQL:

```yaml
postgres:
  image: postgres:17-alpine
  container_name: fintrackpro-postgres
  environment:
    POSTGRES_DB: FinTrackPro
    POSTGRES_USER: postgres
    POSTGRES_PASSWORD: YourStrong@Passw0rd
  ports:
    - "5432:5432"
  volumes:
    - postgres-data:/var/lib/postgresql/data
  healthcheck:
    test: ["CMD-SHELL", "pg_isready -U postgres"]
    interval: 10s
    timeout: 5s
    retries: 5
    start_period: 10s
```

Add explicit `DatabaseProvider__Provider: sqlserver` to the existing `api` and `migrator` services.

To run locally with PostgreSQL instead of SQL Server:
```bash
docker compose up -d postgres keycloak
```
Then set env vars:
```
DatabaseProvider__Provider=postgresql
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=FinTrackPro;Username=postgres;Password=YourStrong@Passw0rd;
```

---

## Part B — Terraform

### Strategy

Delete all existing Render services and the `fintrackpro` project manually from the Render dashboard, then run `terraform apply` from scratch. No `terraform import` required.

---

### B1. `infra/terraform/render.tf` — new resources + updated services

**Add `render_project`:**
```hcl
resource "render_project" "fintrackpro" {
  name = "fintrackpro"
  environments = {
    production = {
      name             = "Production"
      protected_status = "protected"
    }
  }
}
```

**Add `render_postgres` (lifecycle-guarded — created once, never modified):**
```hcl
resource "render_postgres" "db" {
  name           = "fintrackpro-db"
  plan           = "free"
  region         = "oregon"
  version        = "18"
  environment_id = render_project.fintrackpro.environments["production"].id

  lifecycle {
    ignore_changes  = all    # never updated after initial creation
    prevent_destroy = true   # blocks accidental terraform destroy
  }
}
```

**Update `render_web_service.api`:**
```hcl
environment_id = render_project.fintrackpro.environments["production"].id

env_vars = {
  # ...existing vars...
  DatabaseProvider__Provider           = { value = "postgresql" }
  ConnectionStrings__DefaultConnection = { value = render_postgres.db.connection_info.internal_connection_string }
  # remove: db_connection_string reference
}
```

**Update `render_static_site.frontend`:**
```hcl
environment_id = render_project.fintrackpro.environments["production"].id
```

---

### B2. `infra/terraform/variables.tf`

Remove the `db_connection_string` variable — the connection string is now sourced directly from `render_postgres.db`.

---

### B3. `infra/terraform/outputs.tf`

Add:
```hcl
output "db_internal_url" {
  description = "PostgreSQL internal URL — accessible only within Render's network"
  value       = render_postgres.db.connection_info.internal_connection_string
  sensitive   = true
}

output "db_external_url" {
  description = "PostgreSQL external URL — use for local migrations via user-secrets"
  value       = render_postgres.db.connection_info.external_connection_string
  sensitive   = true
}
```

---

### B4. `infra/terraform/terraform.tfvars.example`

Remove `db_connection_string` line. Add comment:
```hcl
# Database connection string is no longer required — the PostgreSQL instance
# is provisioned by Terraform and the internal URL is auto-injected into the API.
```

Also remove `db_connection_string` from the workspace variables table in `docs/render-terraform-deploy.md`.

---

## Part C — `render.yaml` (Blueprint fallback)

Add `databases:` section and wire the connection string:

```yaml
databases:
  - name: fintrackpro-db
    plan: free
    ipAllowList: []

services:
  - type: web
    name: fintrackpro-api
    envVars:
      - key: ConnectionStrings__DefaultConnection
        fromDatabase:
          name: fintrackpro-db
          property: connectionString
      - key: DatabaseProvider__Provider
        value: postgresql
      # remove the old sync: false entry for ConnectionStrings__DefaultConnection
```

---

## Deployment Runbook (post-implementation)

### Step 1 — Clean Render slate
Manually delete all services (`fintrackpro-db`, `fintrackpro-api`, `fintrackpro-ui`) and the `fintrackpro` project from the Render dashboard.

### Step 2 — Apply Terraform
```bash
cd infra/terraform
terraform init
terraform plan   # verify: 4 resources to create, 0 to destroy
terraform apply
```

Terraform provisions: project → DB → API service → UI service.

### Step 3 — Run database migrations
The Render DB is empty. Apply migrations from your local machine using the external URL:

```bash
# Get the external DB URL
terraform output -raw db_external_url

export DatabaseProvider__Provider=postgresql
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<external-url>" \
  --project backend/src/FinTrackPro.API

# Run migrations
cd backend
dotnet ef database update \
  --project src/FinTrackPro.Infrastructure \
  --startup-project src/FinTrackPro.API
```

### Step 4 — Trigger API redeploy
Push to `main` or manually trigger a deploy in the Render dashboard to pick up the latest image with the new Npgsql packages.

### Step 5 — Post-deploy wiring (same as before)
Update `cors_origins` and `vite_api_base_url` workspace variables, then `terraform apply` again.

---

## Render Free-Tier PostgreSQL Constraints

| Constraint | Impact |
|---|---|
| DB expires after 90 days unless upgraded | Render emails a warning; recreate or upgrade before expiry |
| 256 MB RAM, 1 GB storage | Suitable for dev / low-traffic prod |
| No high availability | Acceptable for free tier |
| `internal_connection_string` only reachable from Render network | Use `external_connection_string` for local `dotnet ef` migrations |

---

## Files Modified (18 total)

| File | Change |
|---|---|
| `backend/src/FinTrackPro.Infrastructure/FinTrackPro.Infrastructure.csproj` | Add `Npgsql.EntityFrameworkCore.PostgreSQL` |
| `backend/src/FinTrackPro.API/FinTrackPro.API.csproj` | Add `Hangfire.PostgreSql` |
| `backend/src/FinTrackPro.Infrastructure/DependencyInjection.cs` | Conditional `UseNpgsql`/`UseSqlServer` |
| `backend/src/FinTrackPro.API/Program.cs` | Conditional Hangfire storage + `using Hangfire.PostgreSql` |
| `backend/src/FinTrackPro.API/appsettings.json` | Add `DatabaseProvider` section |
| `backend/.../Configurations/BudgetConfiguration.cs` | `HasPrecision(18, 2)` |
| `backend/.../Configurations/TradeConfiguration.cs` | `HasPrecision(18, 8)` × 4 |
| `backend/.../Configurations/SignalConfiguration.cs` | `HasPrecision(18, 8)` |
| `backend/.../Configurations/TransactionConfiguration.cs` | `HasPrecision(18, 2)` |
| `backend/.../Migrations/20260320060128_InitialCreate.cs` | Remove SQL Server type strings |
| `backend/.../Migrations/ApplicationDbContextModelSnapshot.cs` | Remove SQL Server annotations |
| `docker-compose.yml` | Add `postgres` service |
| `infra/terraform/render.tf` | Add project + DB resources, link services to project |
| `infra/terraform/variables.tf` | Remove `db_connection_string` |
| `infra/terraform/outputs.tf` | Add `db_internal_url` + `db_external_url` outputs |
| `infra/terraform/terraform.tfvars.example` | Remove `db_connection_string` |
| `render.yaml` | Add `databases:` section, wire connection string |
| `docs/render-terraform-deploy.md` | Remove Azure SQL refs, add PostgreSQL + migration steps |
