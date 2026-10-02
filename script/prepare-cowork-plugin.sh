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

stop_listener() {
    local port="$1"
    local process_name="$2"
    local process_ids

    process_ids="$(ss -ltnp "sport = :$port" 2>/dev/null | grep -oE 'pid=[0-9]+' | cut -d= -f2 | sort -u || true)"
    for process_id in $process_ids; do
        local command_line
        command_line="$(tr '\0' ' ' < "/proc/$process_id/cmdline" 2>/dev/null || true)"
        if [[ "$command_line" == *"$process_name"* ]]; then
            kill "$process_id" 2>/dev/null || true
            echo "Stopped $process_name listener on port $port (PID $process_id)."
        fi
    done
}

if [[ "$STOP_DEMO" == true ]]; then
    stop_process "devtunnel"
    stop_process "mcp"
    stop_process "api"
    stop_listener 3001 "SanctuaryIntelligence.Mcp"
    stop_listener 5100 "SanctuaryIntelligence.Api"
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

for command in dotnet curl devtunnel; do
    command -v "$command" >/dev/null 2>&1 || fail "'$command' is required."
done

if command -v zip >/dev/null 2>&1; then
    ZIP_PROCESSOR="zip"
elif command -v python3 >/dev/null 2>&1; then
    ZIP_PROCESSOR="python3"
else
    fail "'zip' or 'python3' is required to create the plugin package."
fi

if command -v jq >/dev/null 2>&1; then
    JSON_PROCESSOR="jq"
elif command -v node >/dev/null 2>&1; then
    JSON_PROCESSOR="node"
else
    fail "'jq' or 'node' is required to update the plugin manifest."
fi

manifest_value() {
    local value_name="$1"

    if [[ "$JSON_PROCESSOR" == "jq" ]]; then
        case "$value_name" in
            version)
                jq -r '.version' "$MANIFEST_PATH"
                ;;
            mcpServerUrl)
                jq -r '.agentConnectors[0].toolSource.remoteMcpServer.mcpServerUrl' "$MANIFEST_PATH"
                ;;
            referenceId)
                jq -r '.agentConnectors[0].toolSource.remoteMcpServer.authorization.referenceId // empty' "$MANIFEST_PATH"
                ;;
        esac
        return
    fi

    node - "$MANIFEST_PATH" "$value_name" <<'NODE'
const fs = require('fs');
const [manifestPath, valueName] = process.argv.slice(2);
const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));
const remoteMcpServer = manifest.agentConnectors?.[0]?.toolSource?.remoteMcpServer;
const values = {
  version: manifest.version,
  mcpServerUrl: remoteMcpServer?.mcpServerUrl,
  referenceId: remoteMcpServer?.authorization?.referenceId
};
process.stdout.write(values[valueName] ?? '');
NODE
}

update_manifest() {
    local output_path="$1"

    if [[ "$JSON_PROCESSOR" == "jq" ]]; then
        if [[ "$PLUGIN_MODE" == "entra" ]]; then
            jq --arg version "$NEXT_VERSION" --arg mcpServerUrl "$MCP_SERVER_URL" --arg referenceId "$AUTH_CONFIG_ID" '
                .version = $version |
                .agentConnectors[0].toolSource.remoteMcpServer.mcpServerUrl = $mcpServerUrl |
                .agentConnectors[0].toolSource.remoteMcpServer.authorization.referenceId = $referenceId
            ' "$MANIFEST_PATH" >"$output_path"
        else
            jq --arg version "$NEXT_VERSION" --arg mcpServerUrl "$MCP_SERVER_URL" '
                .version = $version |
                .agentConnectors[0].toolSource.remoteMcpServer.mcpServerUrl = $mcpServerUrl
            ' "$MANIFEST_PATH" >"$output_path"
        fi
        return
    fi

    node - "$MANIFEST_PATH" "$output_path" "$NEXT_VERSION" "$MCP_SERVER_URL" "$PLUGIN_MODE" "$AUTH_CONFIG_ID" <<'NODE'
const fs = require('fs');
const [manifestPath, outputPath, version, mcpServerUrl, pluginMode, authConfigId] = process.argv.slice(2);
const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));
const remoteMcpServer = manifest.agentConnectors[0].toolSource.remoteMcpServer;
manifest.version = version;
remoteMcpServer.mcpServerUrl = mcpServerUrl;
if (pluginMode === 'entra') {
  remoteMcpServer.authorization.referenceId = authConfigId;
}
fs.writeFileSync(outputPath, `${JSON.stringify(manifest, null, 2)}\n`);
NODE
}

create_plugin_zip() {
    local zip_path="$1"

    if [[ "$ZIP_PROCESSOR" == "zip" ]]; then
        (
            cd "$PLUGIN_DIR"
            zip -qr "$zip_path" . -x '*.DS_Store'
        )
        return
    fi

    python3 - "$PLUGIN_DIR" "$zip_path" <<'PYTHON'
import os
import sys
import zipfile

plugin_dir, zip_path = sys.argv[1:]
with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED) as package:
    for root, _, files in os.walk(plugin_dir):
        for file_name in files:
            if file_name == '.DS_Store':
                continue
            file_path = os.path.join(root, file_name)
            package.write(file_path, os.path.relpath(file_path, plugin_dir))
PYTHON
}

[[ -f "$MANIFEST_PATH" ]] || fail "Manifest not found: $MANIFEST_PATH"
[[ -f "$PLUGIN_DIR/color.png" ]] || fail "Missing required icon: $PLUGIN_DIR/color.png"
[[ -f "$PLUGIN_DIR/outline.png" ]] || fail "Missing required icon: $PLUGIN_DIR/outline.png"
[[ -n "$TUNNEL_ID" ]] || fail "Provide --tunnel-id with a persistent Dev Tunnel ID."

if [[ "$PLUGIN_MODE" == "entra" ]]; then
    CURRENT_AUTH_CONFIG_ID="$(manifest_value referenceId)"
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

    if [[ "$name" == "api" ]]; then
        nohup env \
            AzureOpenAI__Endpoint="$AZURE_OPENAI_ENDPOINT" \
            AzureOpenAI__DeploymentName="${AZURE_OPENAI_DEPLOYMENT_NAME:-}" \
            AzureOpenAI__ApiKey="${AZURE_OPENAI_API_KEY:-}" \
            Authentication__Enabled="${API_AUTH_ENABLED:-false}" \
            Authentication__Instance="https://login.microsoftonline.com/" \
            Authentication__TenantId="${ENTRA_TENANT_ID:-}" \
            Authentication__ClientId="${ENTRA_CLIENT_ID:-}" \
            Authentication__Audience="${ENTRA_APPLICATION_ID_URI:-}" \
            Authentication__Audiences__0="${ENTRA_APPLICATION_ID_URI:-}" \
            Authentication__Audiences__1="api://${ENTRA_CLIENT_ID:-}" \
            Authentication__Audiences__2="${ENTRA_CLIENT_ID:-}" \
            dotnet run --project "$project" >"$log_file" 2>&1 &
    elif [[ "$name" == "mcp" && -n "${MCP_AUTH_ENABLED:-}" ]]; then
        nohup env \
            Authentication__Enabled="$MCP_AUTH_ENABLED" \
            Authentication__TenantId="${ENTRA_TENANT_ID:-}" \
            Authentication__ClientId="${ENTRA_CLIENT_ID:-}" \
            Authentication__Audience="${ENTRA_APPLICATION_ID_URI:-}" \
            Authentication__Audiences__0="${ENTRA_APPLICATION_ID_URI:-}" \
            Authentication__Audiences__1="api://${ENTRA_CLIENT_ID:-}" \
            Authentication__Audiences__2="${ENTRA_CLIENT_ID:-}" \
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
    TUNNEL_URL="$(grep -Eo 'https://[[:alnum:].:-]+' "$TUNNEL_LOG" 2>/dev/null | grep -v -- '-inspect\.' | head -n 1 | sed 's:/*$::' || true)"
    [[ -n "$TUNNEL_URL" ]] && break
    sleep 2
done
[[ -n "$TUNNEL_URL" ]] || fail "Dev Tunnel did not return a public URL. Check $TUNNEL_LOG."

CURRENT_VERSION="$(manifest_value version)"
if [[ ! "$CURRENT_VERSION" =~ ^([0-9]+)\.([0-9]+)\.([0-9]+)$ ]]; then
    fail "Manifest version '$CURRENT_VERSION' is not a three-part semantic version."
fi
MCP_SERVER_URL="$TUNNEL_URL/mcp"

CURRENT_MCP_SERVER_URL="$(manifest_value mcpServerUrl)"
CURRENT_REFERENCE_ID="$(manifest_value referenceId)"
EXISTING_ZIP="$DIST_DIR/$PLUGIN_ARCHIVE_PREFIX-$CURRENT_VERSION.zip"
if [[ "$SKIP_PACKAGE_IF_UNCHANGED" == true && "$CURRENT_MCP_SERVER_URL" == "$MCP_SERVER_URL" ]]; then
    if [[ "$PLUGIN_MODE" == "anonymous" || "$CURRENT_REFERENCE_ID" == "$AUTH_CONFIG_ID" ]]; then
        if [[ -f "$EXISTING_ZIP" && -z "$(find "$PLUGIN_DIR" -type f -newer "$EXISTING_ZIP" -print -quit)" ]]; then
            echo "MCP URL: $MCP_SERVER_URL"
            echo "Plugin mode: $PLUGIN_MODE"
            echo "Plugin ZIP unchanged: $EXISTING_ZIP"
            exit 0
        fi
    fi
fi

NEXT_VERSION="${BASH_REMATCH[1]}.${BASH_REMATCH[2]}.$((BASH_REMATCH[3] + 1))"

TEMP_MANIFEST="$(mktemp)"
update_manifest "$TEMP_MANIFEST"
mv "$TEMP_MANIFEST" "$MANIFEST_PATH"

ZIP_PATH="$DIST_DIR/$PLUGIN_ARCHIVE_PREFIX-$NEXT_VERSION.zip"
create_plugin_zip "$ZIP_PATH"

echo "MCP URL: $MCP_SERVER_URL"
echo "Plugin mode: $PLUGIN_MODE"
echo "Plugin version: $NEXT_VERSION"
echo "Plugin ZIP: $ZIP_PATH"
echo "Stop local processes with: $0 --stop"