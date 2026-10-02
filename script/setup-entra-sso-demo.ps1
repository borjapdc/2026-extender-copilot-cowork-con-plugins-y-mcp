[CmdletBinding()]
param(
    [string]$EnvFile = (Join-Path $PSScriptRoot '.env'),
    [switch]$Stop
)

$ErrorActionPreference = 'Stop'

if ($Stop) {
    & (Join-Path $PSScriptRoot 'prepare-cowork-plugin.ps1') -Stop
    exit 0
}

# This script prepares the Entra SSO package and passes its public identity
# settings only to the MCP process. It never stores a client secret.
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
$tenantId = Require-DemoValue $settings 'ENTRA_TENANT_ID'
$clientId = Require-DemoValue $settings 'ENTRA_CLIENT_ID'
$applicationIdUri = Require-DemoValue $settings 'ENTRA_APPLICATION_ID_URI'
$ssoRegistrationId = Require-DemoValue $settings 'ENTRA_SSO_REGISTRATION_ID'
$azureOpenAiEndpoint = Require-DemoValue $settings 'AZURE_OPENAI_ENDPOINT'
$azureOpenAiDeploymentName = Require-DemoValue $settings 'AZURE_OPENAI_DEPLOYMENT_NAME'
$azureOpenAiApiKey = Require-DemoValue $settings 'AZURE_OPENAI_API_KEY'
$healthUrl = 'http://localhost:3001/health'

# Restart an anonymous MCP before it can validate Entra JWTs.
try {
    $health = (Invoke-WebRequest -Uri $healthUrl -TimeoutSec 2).Content
    if ($health -match '"authentication":"None"') {
        & (Join-Path $PSScriptRoot 'prepare-cowork-plugin.ps1') -Stop
    }
}
catch {
}

& (Join-Path $PSScriptRoot 'prepare-cowork-plugin.ps1') `
    -Plugin entra `
    -TunnelId $tunnelId `
    -AuthConfigId $ssoRegistrationId `
    -McpAuthEnabled true `
    -EntraTenantId $tenantId `
    -EntraClientId $clientId `
    -EntraApplicationIdUri $applicationIdUri `
    -ApiAuthEnabled true `
    -AzureOpenAiEndpoint $azureOpenAiEndpoint `
    -AzureOpenAiDeploymentName $azureOpenAiDeploymentName `
    -AzureOpenAiApiKey $azureOpenAiApiKey `
    -SkipPackageIfUnchanged