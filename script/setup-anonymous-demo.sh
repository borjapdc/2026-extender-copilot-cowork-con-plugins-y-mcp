#!/usr/bin/env bash
set -euo pipefail

# This script prepares the anonymous local demonstration package and keeps its
# manifest aligned with a persistent Microsoft Dev Tunnel URL.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
ENV_FILE="${SANCTUARY_DEMO_ENV_FILE:-$SCRIPT_DIR/.env}"
MCP_HEALTH_URL="http://localhost:3001/health"

fail() {
    echo "Error: $1" >&2
    exit 1
}

require_value() {
    local name="$1"
    local value="$2"
    [[ -n "$value" && "$value" != REPLACE-WITH-* ]] || fail "Set $name in $ENV_FILE."
}

# The setup data is local and intentionally excluded from source control.
[[ -f "$ENV_FILE" ]] || fail "Create $ENV_FILE from $SCRIPT_DIR/.env.example."
set -a
source "$ENV_FILE"
set +a
require_value "TUNNEL_ID" "${TUNNEL_ID:-}"

# A running MCP in SSO mode must be restarted before the anonymous package is used.
if curl --fail --silent "$MCP_HEALTH_URL" | grep -q 'Entra ID'; then
    bash "$SCRIPT_DIR/prepare-cowork-plugin.sh" --stop
fi

# These variables are consumed only by the MCP process launched from prepare-cowork-plugin.sh.
export MCP_AUTH_ENABLED=false
export API_AUTH_ENABLED=false
bash "$SCRIPT_DIR/prepare-cowork-plugin.sh" \
    --plugin anonymous \
    --tunnel-id "$TUNNEL_ID" \
    --skip-package-if-unchanged