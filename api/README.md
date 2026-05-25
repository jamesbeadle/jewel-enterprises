# JPMS API

Azure Functions Isolated worker project that hosts the JPMS HTTP endpoints and persists to Azure SQL via Entity Framework Core.

## Stack

| Layer | Tech |
|---|---|
| Runtime | .NET 8 Isolated worker |
| ORM | EF Core 8 (SQL Server provider) |
| Hosting | Azure Static Web Apps managed Functions (`/api/*` on the same domain as the SPA) |
| Schema | `Database.EnsureCreatedAsync()` on cold start (replace with migrations when the schema stabilises) |

## Folder layout

```
api/
├── JpmsApi.csproj              .NET 8 Isolated worker + EF Core
├── host.json                   Functions runtime config (routes under /api)
├── local.settings.json.example Local dev secrets template — copy to local.settings.json
├── Program.cs                  Host startup + DI + EnsureCreated runner
├── Data/
│   ├── JpmsContext.cs          DbContext with every DbSet
│   ├── DatabaseInitialiser.cs  EnsureCreated wrapper
│   └── Entities/               POCO entities (one file per concept group)
└── Functions/
    └── ProjectsApi.cs          Worked example: GET /api/projects, GET /api/projects/{id}, POST /api/projects
```

## Local development

```bash
cd api
cp local.settings.json.example local.settings.json
# Paste your SQL connection string into local.settings.json
func start
```

Then in another terminal, run the Blazor app pointing at the local API:

```bash
cd jpms
dotnet watch
```

The Blazor `HttpProjectStore` issues requests to `/api/projects`. Static Web Apps proxies `/api/*` to the Functions app in production; locally the Blazor dev server proxies via `staticwebapp.config.json`.

## Production secrets

`SqlConnectionString` is the only required application setting. Add it via:

```bash
az staticwebapp appsettings set \
  --name swa-jpms-test --resource-group rg-jpms-test \
  --setting-names "SqlConnectionString=$(grep ^SQL_CONNECTION_STRING infra/.azure-output.env | cut -d= -f2- | tr -d '\"')"
```

The connection string is in `infra/.azure-output.env` from the original Azure provisioning run.

## Adding a new resource endpoint

The Projects path demonstrates the pattern. To add (e.g.) Leads:

1. Already done in `Data/JpmsContext.cs` — DbSet<LeadEntity> exists.
2. Add `Functions/LeadsApi.cs` modelled on `ProjectsApi.cs` (List, Get, Upsert).
3. In the Blazor app, add `Services/HttpLeadStore.cs` modelled on `HttpProjectStore.cs`.
4. In `jpms/Program.cs`, swap the registration:
   ```diff
   - builder.Services.AddScoped<ILeadStore, InMemoryLeadStore>();
   + builder.Services.AddScoped<ILeadStore, HttpLeadStore>();
   ```

For each store, the swap is the same three-file pattern: server controller, client HTTP store, DI registration change.

## Schema migrations

The current approach is `EnsureCreated` — it creates tables on first run but does not migrate schema changes. When the schema starts changing in production, switch to migrations:

```bash
cd api
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Then replace `EnsureCreatedAsync` in `Program.cs` with `MigrateAsync`.

## Cost note

The SQL database is configured as Serverless with auto-pause after 60 minutes idle. The first request after pause takes ~30 seconds to warm up. Cost while paused: storage only (a few pounds/month, or free if the Free-Limit flag stuck). Active compute is billed by the vCore-second; ~£0.07/hour for a single vCore.
