[CmdletBinding()]
param(
    [string]$EnvFile = (Join-Path $PSScriptRoot '.env')
)

$ErrorActionPreference = 'Stop'

# This script prepares the anonymous local demonstration package and keeps its
# manifest aligned with a persistent Microsoft Dev Tunnel URL.
function Import-DemoEnvironment {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        throw "Create $Path from $PSScriptRoot/.env.example."
    }

    $values = @{}
    foreach ($line in Get-Content $Path) {
        if ($line -match '^\s*$' -or $line -match '^\s*#') {
            continue
        }
        $parts = $line -split '=', 2
        if ($parts.Count -ne 2) {
            throw "Invalid environment entry: $line"
        }
        $values[$parts[0].Trim()] = $parts[1].Trim()
    }
    return $values
}

function Require-DemoValue {
    param(
        [hashtable]$Values,
        [string]$Name
    )

    $value = $Values[$Name]
    if (-not $value -or $value -like 'REPLACE-WITH-*') {
        throw "Set $Name in $EnvFile."
    }
    return $value
}

$settings = Import-DemoEnvironment $EnvFile
$tunnelId = Require-DemoValue $settings 'TUNNEL_ID'
$azureOpenAiEndpoint = Require-DemoValue $settings 'AZURE_OPENAI_ENDPOINT'
$azureOpenAiDeploymentName = Require-DemoValue $settings 'AZURE_OPENAI_DEPLOYMENT_NAME'
$azureOpenAiApiKey = Require-DemoValue $settings 'AZURE_OPENAI_API_KEY'
$healthUrl = 'http://localhost:3001/health'

# Restart an SSO MCP before running the anonymous package.
try {
    $health = (Invoke-WebRequest -Uri $healthUrl -TimeoutSec 2).Content
    if ($health -match 'Entra ID') {
        & (Join-Path $PSScriptRoot 'prepare-cowork-plugin.ps1') -Stop
    }
}
catch {
}

& (Join-Path $PSScriptRoot 'prepare-cowork-plugin.ps1') `
    -Plugin anonymous `
    -TunnelId $tunnelId `
    -McpAuthEnabled false `
    -ApiAuthEnabled false `
    -AzureOpenAiEndpoint $azureOpenAiEndpoint `
    -AzureOpenAiDeploymentName $azureOpenAiDeploymentName `
    -AzureOpenAiApiKey $azureOpenAiApiKey `
    -SkipPackageIfUnchanged