#!/usr/bin/env bash
#
# Finishes the Sales "Imagine" feature: gives the worker an image model to render concepts with.
# Everything else (public page, photos to blob, Claude concepts, emails, timeline) is live already;
# this is Azure configuration only — no code changes.
#
#   1. Deploys an image model into the existing Azure OpenAI account (oai-jpms-prod, eastus2):
#      gpt-image-1.5 (2025-12-16) first, gpt-image-1 (2025-04-15) if 1.5 is refused.
#      Limited-access approval: ApplicationID 3796427, 7 Sep 2026. GlobalStandard, capacity 1.
#   2. Sets AzureImage__Endpoint / AzureImage__ApiKey / AzureImage__Deployment on the WORKER
#      (func-jpms-worker-prod — the renderer; the SWA API never calls the image model).
#   3. Deletes the stray single-underscore "Anthropic_ApiKey" from the worker.
#   4. Puts a monthly cost budget with email alerts on the OpenAI account (no hard cap exists in
#      Azure OpenAI; this is the closest thing — an actual-spend alert at 80% and a forecast alert).
#
# The double underscore in the setting names is built from a character code on purpose: pasting
# "__" into James's terminal has arrived as "_" and as "" before. Idempotent — safe to re-run:
#   bash infra/azure-imagine-image-setup.sh
#
set -euo pipefail

RESOURCE_GROUP="${RESOURCE_GROUP:-rg-jpms-prod}"
OPENAI_ACCOUNT="${OPENAI_ACCOUNT:-oai-jpms-prod}"
WORKER_APP="${WORKER_APP:-func-jpms-worker-prod}"
BUDGET_AMOUNT="${BUDGET_AMOUNT:-25}"
BUDGET_EMAIL="${BUDGET_EMAIL:-james.beadle@jewelbb.co.uk}"

UU=$(printf '\137\137')
SECTION="AzureImage${UU}"

command -v az >/dev/null || { echo "az CLI is required."; exit 1; }
az account show --output none 2>/dev/null || { echo "Run 'az login' first."; exit 1; }
echo "Subscription: $(az account show --query name --output tsv)"

# ---- 1. The image model deployment. Reuse one that already exists; otherwise 1.5, then 1. ----
existing=$(az cognitiveservices account deployment list --name "${OPENAI_ACCOUNT}" --resource-group "${RESOURCE_GROUP}" \
  --query "[?starts_with(properties.model.name, 'gpt-image')].name | [0]" --output tsv 2>/dev/null || true)

deploy_image_model() {
  local model_name="$1" model_version="$2"
  echo "Deploying ${model_name} ${model_version} (GlobalStandard, capacity 1)…"
  az cognitiveservices account deployment create \
    --name "${OPENAI_ACCOUNT}" --resource-group "${RESOURCE_GROUP}" \
    --deployment-name "${model_name}" \
    --model-name "${model_name}" --model-version "${model_version}" --model-format OpenAI \
    --sku-name GlobalStandard --sku-capacity 1 --output none
}

if [[ -n "${existing}" ]]; then
  DEPLOYMENT="${existing}"
  echo "Image deployment already present: ${DEPLOYMENT} — reusing it."
elif deploy_image_model gpt-image-1.5 2025-12-16; then
  DEPLOYMENT="gpt-image-1.5"
else
  echo "gpt-image-1.5 was refused — falling back to gpt-image-1."
  deploy_image_model gpt-image-1 2025-04-15
  DEPLOYMENT="gpt-image-1"
fi
echo "Deployment in use: ${DEPLOYMENT}"

# ---- 2. Point the worker at it. ----
ENDPOINT=$(az cognitiveservices account show --name "${OPENAI_ACCOUNT}" --resource-group "${RESOURCE_GROUP}" \
  --query properties.endpoint --output tsv)
ENDPOINT="${ENDPOINT%/}"
API_KEY=$(az cognitiveservices account keys list --name "${OPENAI_ACCOUNT}" --resource-group "${RESOURCE_GROUP}" \
  --query key1 --output tsv)
[[ -n "${ENDPOINT}" && -n "${API_KEY}" ]] || { echo "Could not read the account's endpoint or key1."; exit 1; }

echo "Setting ${SECTION}Endpoint / ${SECTION}ApiKey / ${SECTION}Deployment on ${WORKER_APP}…"
az functionapp config appsettings set --name "${WORKER_APP}" --resource-group "${RESOURCE_GROUP}" \
  --settings "${SECTION}Endpoint=${ENDPOINT}" "${SECTION}ApiKey=${API_KEY}" "${SECTION}Deployment=${DEPLOYMENT}" \
  --output none

# ---- 3. The stray single-underscore key. ----
if az functionapp config appsettings list --name "${WORKER_APP}" --resource-group "${RESOURCE_GROUP}" \
     --query "[?name=='Anthropic_ApiKey'].name" --output tsv | grep -q .; then
  echo "Deleting stray Anthropic_ApiKey…"
  az functionapp config appsettings delete --name "${WORKER_APP}" --resource-group "${RESOURCE_GROUP}" \
    --setting-names Anthropic_ApiKey --output none
fi

# ---- 4. Budget alert on the OpenAI account (best effort — needs Cost Management rights). ----
SUBSCRIPTION_ID=$(az account show --query id --output tsv)
ACCOUNT_ID=$(az cognitiveservices account show --name "${OPENAI_ACCOUNT}" --resource-group "${RESOURCE_GROUP}" \
  --query id --output tsv | tr '[:upper:]' '[:lower:]')
BUDGET_START="$(date -u +%Y-%m)-01T00:00:00Z"
BUDGET_END="$(( $(date -u +%Y) + 3 ))-01-01T00:00:00Z"
BUDGET_BODY=$(cat <<EOF
{"properties":{"category":"Cost","amount":${BUDGET_AMOUNT},"timeGrain":"Monthly",
 "timePeriod":{"startDate":"${BUDGET_START}","endDate":"${BUDGET_END}"},
 "filter":{"dimensions":{"name":"ResourceId","operator":"In","values":["${ACCOUNT_ID}"]}},
 "notifications":{
  "actual80":{"enabled":true,"operator":"GreaterThan","threshold":80,"thresholdType":"Actual","contactEmails":["${BUDGET_EMAIL}"]},
  "forecast100":{"enabled":true,"operator":"GreaterThan","threshold":100,"thresholdType":"Forecasted","contactEmails":["${BUDGET_EMAIL}"]}}}}
EOF
)
if az rest --method put \
     --url "https://management.azure.com/subscriptions/${SUBSCRIPTION_ID}/providers/Microsoft.Consumption/budgets/jpms-imagine-images?api-version=2024-08-01" \
     --body "${BUDGET_BODY}" --output none 2>/dev/null; then
  echo "Budget alert 'jpms-imagine-images': ${BUDGET_AMOUNT}/month on ${OPENAI_ACCOUNT}, emails to ${BUDGET_EMAIL} at 80% actual and 100% forecast."
else
  echo "Budget alert not created (needs Cost Management Contributor on the subscription) — add one in the portal under Cost Management > Budgets."
fi

# ---- Verify: names only, never values. ----
echo
echo "Worker settings now:"
az functionapp config appsettings list --name "${WORKER_APP}" --resource-group "${RESOURCE_GROUP}" \
  --query "[?starts_with(name, 'AzureImage') || starts_with(name, 'Anthropic')].{name:name, set:(length(value) > \`0\`)}" --output table
echo
echo "Done. Give the worker a minute to restart, then Sales > Leads > the lead > Imagine > Retry render."
echo "If the render fails on api-version / unsupported parameter, set ${SECTION}ApiVersion to the version the error names — no code change."
