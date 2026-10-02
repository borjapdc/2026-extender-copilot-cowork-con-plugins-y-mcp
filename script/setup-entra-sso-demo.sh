#!/usr/bin/env bash
set -euo pipefail

# This script prepares the Entra SSO package and passes its public identity
# settings only to the MCP process. It never stores a client secret.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
ENV_FILE="${SANCTUARY_DEMO_ENV_FILE:-$SCRIPT_DIR/.env}"
MCP_HEALTH_URL="http://localhost:3001/health"

if [[ "${1:-}" == "--stop" ]]; then
    bash "$SCRIPT_DIR/prepare-cowork-plugin.sh" --stop
    exit 0
fi

fail() {
    echo "Error: $1" >&2
    exit 1
}

require_value() {
    local name="$1"
    local value="$2"
    [[ -n "$value" && "$value" != REPLACE-WITH-* ]] || fail "Set $name in $ENV_FILE."
}

# The SSO registration is created manually in Teams Developer Portal because
# Enterprise Token Store owns the resulting reference ID.
[[ -f "$ENV_FILE" ]] || fail "Create $ENV_FILE from $SCRIPT_DIR/.env.example."
set -a
source "$ENV_FILE"
set +a
for variable in TUNNEL_ID ENTRA_TENANT_ID ENTRA_CLIENT_ID ENTRA_APPLICATION_ID_URI ENTRA_SSO_REGISTRATION_ID; do
    require_value "$variable" "${!variable:-}"
done

# A running anonymous MCP must be restarted before it can validate Entra JWTs.
if curl --fail --silent "$MCP_HEALTH_URL" | grep -q '"authentication":"None"'; then
    bash "$SCRIPT_DIR/prepare-cowork-plugin.sh" --stop
fi

# These values override only the MCP configuration for this local process.
export MCP_AUTH_ENABLED=true
export API_AUTH_ENABLED=true
export ENTRA_TENANT_ID
export ENTRA_CLIENT_ID
export ENTRA_APPLICATION_ID_URI
bash "$SCRIPT_DIR/prepare-cowork-plugin.sh" \
    --plugin entra \
    --tunnel-id "$TUNNEL_ID" \
    --auth-config-id "$ENTRA_SSO_REGISTRATION_ID" \
    --skip-package-if-unchanged