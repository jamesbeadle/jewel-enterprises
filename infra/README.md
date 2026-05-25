# Infrastructure

Azure provisioning for the JPMS test environment.

## What `azure-setup.sh` creates

| Resource | Tier | Cost |
|---|---|---|
| Resource group `rg-jpms-test` | n/a | free |
| Azure SQL Server `sql-jpms-test-<random>` | n/a | free |
| Azure SQL Database `jpms` | GP_S_Gen5 Serverless, auto-pause 60min, Free-Limit if available | ~£0 while idle, free up to 100k vCore-seconds + 32GB/month |
| Static Web App `swa-jpms-test` | Free | free (250GB/month bandwidth) |
| Entra ID app registration | n/a | free |

UK South for SQL, West Europe for the Static Web App (Free tier is not in UK South).

## Run it

```bash
az login
az account set --subscription 08c5510c-bb27-4da8-b826-a8e76fb270ec
./infra/azure-setup.sh
```

The script writes `infra/.azure-output.env` with every value needed afterwards (SQL connection string, SWA deployment token, Entra client ID). That file is gitignored.

## After it runs

1. Copy `SWA_DEPLOYMENT_TOKEN` from `.azure-output.env`.
2. On GitHub, add it as repository secret `AZURE_STATIC_WEB_APPS_API_TOKEN`.
3. Push to `main` — the workflow in `.github/workflows/jpms-swa.yml` builds and deploys.

## Tearing it all down

```bash
az group delete --name rg-jpms-test --yes --no-wait
az ad app delete --id "$(grep ENTRA_APP_ID infra/.azure-output.env | cut -d= -f2)"
```
