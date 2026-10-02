#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DIST_DIR="$ROOT_DIR/dist"
LOG_DIR="$DIST_DIR/logs"
API_HEALTH_URL="http://localhost:5100/health"
MCP_HEALTH_URL="http://localhost:3001/health"
MCP_TOOLS_URL="http://localhost:3001/tools"
AUTH_CONFIG_ID=""
TUNNEL_ID=""
PLUGIN_MODE="entra"
SKIP_PACKAGE_IF_UNCHANGED=false
STOP_DEMO=false

usage() {
    echo "Usage: $0 --plugin <entra|anonymous> --tunnel-id <persistent Dev Tunnel ID> [--auth-config-id <OAuthPluginVault referenceId>] [--skip-package-if-unchanged] [--stop]"
}

fail() {
    echo "Error: $1" >&2
    exit 1
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --auth-config-id)
            [[ $# -ge 2 ]] || fail "--auth-config-id requires a value."
            AUTH_CONFIG_ID="$2"
            shift 2
            ;;
        --tunnel-id)
            [[ $# -ge 2 ]] || fail "--tunnel-id requires a value."
            TUNNEL_ID="$2"
            shift 2
            ;;
        --plugin)
            [[ $# -ge 2 ]] || fail "--plugin requires a value."
            PLUGIN_MODE="$2"
            shift 2
            ;;
        --skip-package-if-unchanged)
            SKIP_PACKAGE_IF_UNCHANGED=true
            shift
            ;;
        --stop)
            STOP_DEMO=true
            shift
            ;;
        --help|-h)
            usage
            exit 0
            ;;
        *)
            usage
            fail "Unknown argument: $1"
            ;;
    esac
done

stop_process() {
    local name="$1"
    local pid_file="$LOG_DIR/$name.pid"

    if [[ -f "$pid_file" ]]; then
        local process_id
        process_id="$(<"$pid_file")"
        if kill -0 "$process_id" 2>/dev/null; then
            kill "$process_id"
            echo "Stopped $name (PID $process_id)."
        fi
        rm -f "$pid_file"
    fi
}

if [[ "$STOP_DEMO" == true ]]; then
    stop_process "devtunnel"
    stop_process "mcp"
    stop_process "api"
    exit 0
fi

case "$PLUGIN_MODE" in
    entra)
        PLUGIN_DIR="$ROOT_DIR/cowork-plugin"
        PLUGIN_ARCHIVE_PREFIX="sanctuary-cowork-plugin-entra"
        ;;
    anonymous)
        PLUGIN_DIR="$ROOT_DIR/cowork-plugin-anonymous"
        PLUGIN_ARCHIVE_PREFIX="sanctuary-cowork-plugin-anonymous"
        ;;
    *)
        fail "--plugin must be 'entra' or 'anonymous'."
        ;;
esac
MANIFEST_PATH="$PLUGIN_DIR/manifest.json"

for command in dotnet curl devtunnel jq zip; do
    command -v "$command" >/dev/null 2>&1 || fail "'$command' is required."
done

[[ -f "$MANIFEST_PATH" ]] || fail "Manifest not found: $MANIFEST_PATH"
[[ -f "$PLUGIN_DIR/color.png" ]] || fail "Missing required icon: $PLUGIN_DIR/color.png"
[[ -f "$PLUGIN_DIR/outline.png" ]] || fail "Missing required icon: $PLUGIN_DIR/outline.png"
[[ -n "$TUNNEL_ID" ]] || fail "Provide --tunnel-id with a persistent Dev Tunnel ID."

if [[ "$PLUGIN_MODE" == "entra" ]]; then
    CURRENT_AUTH_CONFIG_ID="$(jq -r '.agentConnectors[0].toolSource.remoteMcpServer.authorization.referenceId' "$MANIFEST_PATH")"
    if [[ -z "$AUTH_CONFIG_ID" ]]; then
        AUTH_CONFIG_ID="$CURRENT_AUTH_CONFIG_ID"
    fi
    [[ -n "$AUTH_CONFIG_ID" && "$AUTH_CONFIG_ID" != REPLACE-WITH-* ]] || fail "Provide --auth-config-id with the OAuthPluginVault referenceId before packaging."
fi

mkdir -p "$LOG_DIR"

wait_for_url() {
    local url="$1"
    local label="$2"

    for _ in {1..30}; do
        if curl --fail --silent --show-error "$url" >/dev/null; then
            echo "$label is available at $url"
            return
        fi
        sleep 2
    done

    fail "$label did not become available at $url. Check $LOG_DIR."
}

start_service() {
    local name="$1"
    local project="$2"
    local health_url="$3"
    local log_file="$LOG_DIR/$name.log"
    local pid_file="$LOG_DIR/$name.pid"

    if curl --fail --silent "$health_url" >/dev/null; then
        echo "$name is already running."
        return
    fi

    if [[ "$name" == "mcp" && -n "${MCP_AUTH_ENABLED:-}" ]]; then
        nohup env \
            Authentication__Enabled="$MCP_AUTH_ENABLED" \
            Authentication__TenantId="${ENTRA_TENANT_ID:-}" \
            Authentication__ClientId="${ENTRA_CLIENT_ID:-}" \
            Authentication__Audience="${ENTRA_APPLICATION_ID_URI:-}" \
            Authentication__Audiences__0="${ENTRA_APPLICATION_ID_URI:-}" \
            dotnet run --project "$project" >"$log_file" 2>&1 &
    else
        nohup dotnet run --project "$project" >"$log_file" 2>&1 &
    fi
    echo $! >"$pid_file"
    wait_for_url "$health_url" "$name"
}

start_service "api" "$ROOT_DIR/src/SanctuaryIntelligence.Api" "$API_HEALTH_URL"
start_service "mcp" "$ROOT_DIR/src/SanctuaryIntelligence.Mcp" "$MCP_HEALTH_URL"
wait_for_url "$MCP_TOOLS_URL" "MCP tools endpoint"

devtunnel user login

TUNNEL_LOG="$LOG_DIR/devtunnel.log"
TUNNEL_PID_FILE="$LOG_DIR/devtunnel.pid"
if [[ -f "$TUNNEL_PID_FILE" ]] && kill -0 "$(<"$TUNNEL_PID_FILE")" 2>/dev/null; then
    stop_process "devtunnel"
fi

nohup devtunnel host "$TUNNEL_ID" >"$TUNNEL_LOG" 2>&1 &
echo $! >"$TUNNEL_PID_FILE"

TUNNEL_URL=""
for _ in {1..30}; do
    TUNNEL_URL="$(grep -Eo 'https://[^[:space:]]+' "$TUNNEL_LOG" 2>/dev/null | grep -v -- '-inspect\.' | head -n 1 | sed 's:/*$::' || true)"
    [[ -n "$TUNNEL_URL" ]] && break
    sleep 2
done
[[ -n "$TUNNEL_URL" ]] || fail "Dev Tunnel did not return a public URL. Check $TUNNEL_LOG."

CURRENT_VERSION="$(jq -r '.version' "$MANIFEST_PATH")"
if [[ ! "$CURRENT_VERSION" =~ ^([0-9]+)\.([0-9]+)\.([0-9]+)$ ]]; then
    fail "Manifest version '$CURRENT_VERSION' is not a three-part semantic version."
fi
MCP_SERVER_URL="$TUNNEL_URL/mcp"

CURRENT_MCP_SERVER_URL="$(jq -r '.agentConnectors[0].toolSource.remoteMcpServer.mcpServerUrl' "$MANIFEST_PATH")"
CURRENT_REFERENCE_ID="$(jq -r '.agentConnectors[0].toolSource.remoteMcpServer.authorization.referenceId // empty' "$MANIFEST_PATH")"
EXISTING_ZIP="$DIST_DIR/$PLUGIN_ARCHIVE_PREFIX-$CURRENT_VERSION.zip"
if [[ "$SKIP_PACKAGE_IF_UNCHANGED" == true && "$CURRENT_MCP_SERVER_URL" == "$MCP_SERVER_URL" ]]; then
    if [[ "$PLUGIN_MODE" == "anonymous" || "$CURRENT_REFERENCE_ID" == "$AUTH_CONFIG_ID" ]]; then
        if [[ -f "$EXISTING_ZIP" ]]; then
            echo "MCP URL: $MCP_SERVER_URL"
            echo "Plugin mode: $PLUGIN_MODE"
            echo "Plugin ZIP unchanged: $EXISTING_ZIP"
            exit 0
        fi
    fi
fi

NEXT_VERSION="${BASH_REMATCH[1]}.${BASH_REMATCH[2]}.$((BASH_REMATCH[3] + 1))"

TEMP_MANIFEST="$(mktemp)"
if [[ "$PLUGIN_MODE" == "entra" ]]; then
    jq --arg version "$NEXT_VERSION" --arg mcpServerUrl "$MCP_SERVER_URL" --arg referenceId "$AUTH_CONFIG_ID" '
        .version = $version |
        .agentConnectors[0].toolSource.remoteMcpServer.mcpServerUrl = $mcpServerUrl |
        .agentConnectors[0].toolSource.remoteMcpServer.authorization.referenceId = $referenceId
    ' "$MANIFEST_PATH" >"$TEMP_MANIFEST"
else
    jq --arg version "$NEXT_VERSION" --arg mcpServerUrl "$MCP_SERVER_URL" '
        .version = $version |
        .agentConnectors[0].toolSource.remoteMcpServer.mcpServerUrl = $mcpServerUrl
    ' "$MANIFEST_PATH" >"$TEMP_MANIFEST"
fi
mv "$TEMP_MANIFEST" "$MANIFEST_PATH"

ZIP_PATH="$DIST_DIR/$PLUGIN_ARCHIVE_PREFIX-$NEXT_VERSION.zip"
(
    cd "$PLUGIN_DIR"
    zip -qr "$ZIP_PATH" . -x '*.DS_Store'
)

echo "MCP URL: $MCP_SERVER_URL"
echo "Plugin mode: $PLUGIN_MODE"
echo "Plugin version: $NEXT_VERSION"
echo "Plugin ZIP: $ZIP_PATH"
echo "Stop local processes with: $0 --stop"