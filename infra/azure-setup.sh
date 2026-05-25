#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
OUTPUT_ENV="${SCRIPT_DIR}/.azure-output.env"

if [ -f "${OUTPUT_ENV}" ]; then
  echo "Reusing names from previous run: ${OUTPUT_ENV}"
  # shellcheck disable=SC1090
  source "${OUTPUT_ENV}"
fi

RESOURCE_GROUP="${RESOURCE_GROUP:-rg-jpms-test}"
SQL_LOCATION="${SQL_LOCATION:-northeurope}"
SWA_LOCATION="${SWA_LOCATION:-westeurope}"
SQL_SERVER="${SQL_SERVER:-sql-jpms-test-$(openssl rand -hex 3)}"
SQL_DATABASE="${SQL_DATABASE:-jpms}"
SWA_NAME="${SWA_NAME:-swa-jpms-test}"
ENTRA_APP_NAME="${ENTRA_APP_NAME:-entra-jpms-test}"
SQL_ADMIN_USER="${SQL_ADMIN_USER:-jpmsadmin}"
SQL_ADMIN_PASSWORD="${SQL_ADMIN_PASSWORD:-$(openssl rand -base64 24 | tr -d '/+=' | cut -c1-20)Aa1!}"

CLIENT_IP="$(curl -s https://api.ipify.org)"

echo "Resource group:    ${RESOURCE_GROUP}"
echo "SQL server:        ${SQL_SERVER}.database.windows.net"
echo "SQL database:      ${SQL_DATABASE}"
echo "Static Web App:    ${SWA_NAME}"
echo "Entra app:         ${ENTRA_APP_NAME}"
echo "SQL admin user:    ${SQL_ADMIN_USER}"
echo "SQL admin password (save this): ${SQL_ADMIN_PASSWORD}"
echo "Client IP:         ${CLIENT_IP}"
echo

read -r -p "Continue with these values? (y/N) " confirmation
[[ "${confirmation}" == "y" || "${confirmation}" == "Y" ]] || { echo "Aborted."; exit 1; }

echo
echo "Registering required resource providers..."
REQUIRED_PROVIDERS=(Microsoft.Sql Microsoft.Web)
for namespace in "${REQUIRED_PROVIDERS[@]}"; do
  state="$(az provider show --namespace "${namespace}" --query registrationState --output tsv 2>/dev/null || echo NotRegistered)"
  if [ "${state}" != "Registered" ]; then
    echo "  ${namespace}: ${state} — registering"
    az provider register --namespace "${namespace}" --output none
  else
    echo "  ${namespace}: already registered"
  fi
done

echo
echo "Waiting for provider registrations to complete..."
for namespace in "${REQUIRED_PROVIDERS[@]}"; do
  while true; do
    state="$(az provider show --namespace "${namespace}" --query registrationState --output tsv)"
    [ "${state}" == "Registered" ] && break
    echo "  ${namespace}: ${state}"
    sleep 10
  done
  echo "  ${namespace}: Registered"
done

echo
echo "Ensuring resource group exists..."
existing_rg_location="$(az group show \
  --name "${RESOURCE_GROUP}" \
  --query location \
  --output tsv 2>/dev/null || echo "")"
if [ -n "${existing_rg_location}" ]; then
  echo "  resource group already exists in ${existing_rg_location}"
else
  echo "  creating in ${SQL_LOCATION}"
  az group create \
    --name "${RESOURCE_GROUP}" \
    --location "${SQL_LOCATION}" \
    --output none
fi

echo "Creating SQL server (skip if exists)..."
existing_location="$(az sql server show \
  --name "${SQL_SERVER}" \
  --resource-group "${RESOURCE_GROUP}" \
  --query location \
  --output tsv 2>/dev/null || echo "")"

if [ -n "${existing_location}" ]; then
  SQL_LOCATION="${existing_location}"
  echo "  SQL server already exists in ${SQL_LOCATION} — adopting"
else
  SQL_REGIONS_TO_TRY=("${SQL_LOCATION}" "westeurope" "ukwest")
  SQL_SERVER_CREATED=false
  TRIED_REGIONS=()
  for try_location in "${SQL_REGIONS_TO_TRY[@]}"; do
    if [[ " ${TRIED_REGIONS[*]:-} " == *" ${try_location} "* ]]; then
      continue
    fi
    TRIED_REGIONS+=("${try_location}")
    echo "  attempting ${try_location}..."
    set +e
    create_output="$(az sql server create \
      --name "${SQL_SERVER}" \
      --resource-group "${RESOURCE_GROUP}" \
      --location "${try_location}" \
      --admin-user "${SQL_ADMIN_USER}" \
      --admin-password "${SQL_ADMIN_PASSWORD}" \
      --output none 2>&1)"
    create_exit=$?
    set -e
    if [ "${create_exit}" -eq 0 ]; then
      SQL_LOCATION="${try_location}"
      SQL_SERVER_CREATED=true
      echo "  created in ${try_location}"
      break
    fi

    if [[ "${create_output}" == *"InvalidResourceLocation"* ]] \
        && [[ "${create_output}" == *"already exists in location"* ]]; then
      adopted_location="$(echo "${create_output}" \
        | grep -oE "already exists in location '[a-z0-9]+'" \
        | head -1 \
        | sed -E "s/already exists in location '([a-z0-9]+)'/\1/")"
      if [ -n "${adopted_location}" ]; then
        echo "  Azure claims server exists in ${adopted_location} — verifying with show..."
        adoption_verified=false
        for verify_wait in 10 20 30; do
          sleep "${verify_wait}"
          verify_loc="$(az sql server show \
            --name "${SQL_SERVER}" \
            --resource-group "${RESOURCE_GROUP}" \
            --query location \
            --output tsv 2>/dev/null || echo "")"
          if [ -n "${verify_loc}" ]; then
            SQL_LOCATION="${verify_loc}"
            SQL_SERVER_CREATED=true
            adoption_verified=true
            echo "  verified — server is in ${verify_loc}, adopting"
            break
          fi
          echo "  not visible after ${verify_wait}s, retrying..."
        done
        if [ "${adoption_verified}" == "true" ]; then
          break
        fi
        echo "  phantom record in ${adopted_location} (Azure metadata stale). Cannot reuse this name — aborting."
        echo "  Run: az group delete --name ${RESOURCE_GROUP} --yes  then rerun this script."
        exit 1
      fi
    fi

    echo "  checking with delay for eventual consistency..."
    for wait_seconds in 5 10 15; do
      sleep "${wait_seconds}"
      existing_location="$(az sql server show \
        --name "${SQL_SERVER}" \
        --resource-group "${RESOURCE_GROUP}" \
        --query location \
        --output tsv 2>/dev/null || echo "")"
      if [ -n "${existing_location}" ]; then
        SQL_LOCATION="${existing_location}"
        SQL_SERVER_CREATED=true
        echo "  server materialised in ${existing_location} after ${wait_seconds}s — adopting"
        break 2
      fi
    done

    if [[ "${create_output}" == *"RegionDoesNotAllowProvisioning"* ]] \
        || [[ "${create_output}" == *"LocationCapacityLimitReached"* ]] \
        || [[ "${create_output}" == *"NotAvailableForSubscription"* ]]; then
      echo "  ${try_location}: at capacity for new SQL servers, trying next region..."
      continue
    fi
    echo "${create_output}"
    exit 1
  done
  if [ "${SQL_SERVER_CREATED}" != "true" ]; then
    echo "Could not create SQL server in any of: ${SQL_REGIONS_TO_TRY[*]}"
    exit 1
  fi
fi

echo "Configuring SQL firewall (Azure services + client IP)..."
az sql server firewall-rule create \
  --resource-group "${RESOURCE_GROUP}" \
  --server "${SQL_SERVER}" \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0 \
  --output none 2>/dev/null || true

if ! az sql server firewall-rule create \
    --resource-group "${RESOURCE_GROUP}" \
    --server "${SQL_SERVER}" \
    --name ClientIP \
    --start-ip-address "${CLIENT_IP}" \
    --end-ip-address "${CLIENT_IP}" \
    --output none 2>/dev/null; then
  az sql server firewall-rule update \
    --resource-group "${RESOURCE_GROUP}" \
    --server "${SQL_SERVER}" \
    --name ClientIP \
    --start-ip-address "${CLIENT_IP}" \
    --end-ip-address "${CLIENT_IP}" \
    --output none
fi

echo "Creating SQL database (skip if exists)..."
if ! az sql db show --resource-group "${RESOURCE_GROUP}" --server "${SQL_SERVER}" --name "${SQL_DATABASE}" --output none 2>/dev/null; then
  if az sql db create \
      --resource-group "${RESOURCE_GROUP}" \
      --server "${SQL_SERVER}" \
      --name "${SQL_DATABASE}" \
      --edition GeneralPurpose \
      --family Gen5 \
      --capacity 2 \
      --compute-model Serverless \
      --auto-pause-delay 60 \
      --min-capacity 0.5 \
      --use-free-limit \
      --free-limit-exhaustion-behavior AutoPause \
      --output none 2>/dev/null; then
    echo "  Created with Free-Limit (free up to 100k vCore-seconds + 32GB/month)."
  else
    echo "  Free-Limit not available on this subscription; falling back to standard Serverless."
    az sql db create \
      --resource-group "${RESOURCE_GROUP}" \
      --server "${SQL_SERVER}" \
      --name "${SQL_DATABASE}" \
      --edition GeneralPurpose \
      --family Gen5 \
      --capacity 1 \
      --compute-model Serverless \
      --auto-pause-delay 60 \
      --min-capacity 0.5 \
      --output none
  fi
fi

echo "Creating Static Web App (skip if exists)..."
if ! az staticwebapp show --name "${SWA_NAME}" --resource-group "${RESOURCE_GROUP}" --output none 2>/dev/null; then
  az staticwebapp create \
    --name "${SWA_NAME}" \
    --resource-group "${RESOURCE_GROUP}" \
    --location "${SWA_LOCATION}" \
    --sku Free \
    --output none
fi

SWA_HOSTNAME="$(az staticwebapp show \
  --name "${SWA_NAME}" \
  --resource-group "${RESOURCE_GROUP}" \
  --query defaultHostname \
  --output tsv)"

echo "Creating Entra ID app registration (skip if exists)..."
ENTRA_APP_ID="$(az ad app list --display-name "${ENTRA_APP_NAME}" --query '[0].appId' --output tsv 2>/dev/null || true)"
if [ -z "${ENTRA_APP_ID}" ] || [ "${ENTRA_APP_ID}" == "null" ]; then
  ENTRA_APP_ID="$(az ad app create \
    --display-name "${ENTRA_APP_NAME}" \
    --sign-in-audience AzureADMyOrg \
    --web-redirect-uris "https://${SWA_HOSTNAME}/.auth/login/aad/callback" \
    --enable-id-token-issuance true \
    --query appId \
    --output tsv)"
else
  az ad app update \
    --id "${ENTRA_APP_ID}" \
    --web-redirect-uris "https://${SWA_HOSTNAME}/.auth/login/aad/callback" \
    --enable-id-token-issuance true \
    --output none
fi

if [ -z "${ENTRA_SECRET:-}" ]; then
  echo "Generating Entra ID client secret..."
  ENTRA_SECRET="$(az ad app credential reset \
    --id "${ENTRA_APP_ID}" \
    --append \
    --display-name "swa-secret" \
    --years 2 \
    --query password \
    --output tsv)"
fi

echo "Wiring AAD client ID + secret into the Static Web App settings..."
az staticwebapp appsettings set \
  --name "${SWA_NAME}" \
  --resource-group "${RESOURCE_GROUP}" \
  --setting-names \
    "AAD_CLIENT_ID=${ENTRA_APP_ID}" \
    "AAD_CLIENT_SECRET=${ENTRA_SECRET}" \
  --output none

SWA_DEPLOYMENT_TOKEN="$(az staticwebapp secrets list \
  --name "${SWA_NAME}" \
  --resource-group "${RESOURCE_GROUP}" \
  --query properties.apiKey \
  --output tsv)"

cat > "${OUTPUT_ENV}" <<ENVFILE
RESOURCE_GROUP=${RESOURCE_GROUP}
SQL_LOCATION=${SQL_LOCATION}
SWA_LOCATION=${SWA_LOCATION}
SQL_SERVER=${SQL_SERVER}
SQL_DATABASE=${SQL_DATABASE}
SQL_ADMIN_USER=${SQL_ADMIN_USER}
SQL_ADMIN_PASSWORD=${SQL_ADMIN_PASSWORD}
SQL_CONNECTION_STRING="Server=tcp:${SQL_SERVER}.database.windows.net,1433;Database=${SQL_DATABASE};User ID=${SQL_ADMIN_USER};Password=${SQL_ADMIN_PASSWORD};Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;"
SWA_NAME=${SWA_NAME}
SWA_HOSTNAME=${SWA_HOSTNAME}
SWA_DEPLOYMENT_TOKEN=${SWA_DEPLOYMENT_TOKEN}
ENTRA_APP_NAME=${ENTRA_APP_NAME}
ENTRA_APP_ID=${ENTRA_APP_ID}
ENTRA_SECRET=${ENTRA_SECRET}
ENVFILE
chmod 600 "${OUTPUT_ENV}"

echo
echo "Done."
echo
echo "Public URL:           https://${SWA_HOSTNAME}"
echo "SQL server:           ${SQL_SERVER}.database.windows.net"
echo "Entra App (client) ID: ${ENTRA_APP_ID}"
echo
echo "Next: add the SWA_DEPLOYMENT_TOKEN from infra/.azure-output.env as"
echo "      GitHub repository secret AZURE_STATIC_WEB_APPS_API_TOKEN, then push to main."
