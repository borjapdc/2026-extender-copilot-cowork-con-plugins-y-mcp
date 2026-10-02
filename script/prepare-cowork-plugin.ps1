[CmdletBinding()]
param(
    [string]$AuthConfigId,
    [string]$TunnelId,
    [ValidateSet('entra', 'anonymous')]
    [string]$Plugin = 'entra',
    [ValidateSet('true', 'false')]
    [string]$McpAuthEnabled,
    [string]$EntraTenantId,
    [string]$EntraClientId,
    [string]$EntraApplicationIdUri,
    [ValidateSet('true', 'false')]
    [string]$ApiAuthEnabled,
    [string]$AzureOpenAiEndpoint,
    [string]$AzureOpenAiDeploymentName,
    [string]$AzureOpenAiApiKey,
    [switch]$SkipPackageIfUnchanged,
    [switch]$Stop
)

$ErrorActionPreference = 'Stop'
$RootDir = Split-Path -Parent $PSScriptRoot
$DistDir = Join-Path $RootDir 'dist'
$LogDir = Join-Path $DistDir 'logs'
$PluginFolder = if ($Plugin -eq 'entra') { 'cowork-plugin' } else { 'cowork-plugin-anonymous' }
$PluginArchivePrefix = if ($Plugin -eq 'entra') { 'sanctuary-cowork-plugin-entra' } else { 'sanctuary-cowork-plugin-anonymous' }
$PluginDir = Join-Path $RootDir $PluginFolder
$ManifestPath = Join-Path $PluginDir 'manifest.json'
$ApiHealthUrl = 'http://localhost:5100/health'
$McpHealthUrl = 'http://localhost:3001/health'
$McpToolsUrl = 'http://localhost:3001/tools'

function Assert-Command {
    param([string]$Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "'$Name' is required."
    }
}

function Stop-DemoProcess {
    param([string]$Name)

    $pidPath = Join-Path $LogDir "$Name.pid"
    if (Test-Path $pidPath) {
        $processId = Get-Content -Raw $pidPath
        $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
        if ($process) {
            Stop-Process -Id $processId
            Write-Host "Stopped $Name (PID $processId)."
        }
        Remove-Item $pidPath -Force
    }
}

if ($Stop) {
    Stop-DemoProcess 'devtunnel'
    Stop-DemoProcess 'mcp'
    Stop-DemoProcess 'api'
    exit 0
}

foreach ($command in 'dotnet', 'devtunnel') {
    Assert-Command $command
}

if (-not (Test-Path $ManifestPath)) {
    throw "Manifest not found: $ManifestPath"
}
if (-not (Test-Path (Join-Path $PluginDir 'color.png'))) {
    throw "Missing required icon: $PluginFolder/color.png"
}
if (-not (Test-Path (Join-Path $PluginDir 'outline.png'))) {
    throw "Missing required icon: $PluginFolder/outline.png"
}
if (-not $TunnelId) {
    throw 'Provide -TunnelId with a persistent Dev Tunnel ID.'
}

$manifest = Get-Content -Raw $ManifestPath | ConvertFrom-Json
if ($Plugin -eq 'entra') {
    if (-not $AuthConfigId) {
        $AuthConfigId = $manifest.agentConnectors[0].toolSource.remoteMcpServer.authorization.referenceId
    }
    if (-not $AuthConfigId -or $AuthConfigId -like 'REPLACE-WITH-*') {
        throw 'Provide -AuthConfigId with the OAuthPluginVault referenceId before packaging.'
    }
}

New-Item -ItemType Directory -Force -Path $LogDir | Out-Null

function Test-Endpoint {
    param([string]$Url)

    try {
        $response = Invoke-WebRequest -Uri $Url -TimeoutSec 2
        return $response.StatusCode -ge 200 -and $response.StatusCode -lt 300
    }
    catch {
        return $false
    }
}

function Wait-ForEndpoint {
    param(
        [string]$Url,
        [string]$Name
    )

    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        if (Test-Endpoint $Url) {
            Write-Host "$Name is available at $Url"
            return
        }
        Start-Sleep -Seconds 2
    }

    throw "$Name did not become available at $Url. Check $LogDir."
}

function Start-DemoService {
    param(
        [string]$Name,
        [string]$ProjectPath,
        [string]$HealthUrl
    )

    if (Test-Endpoint $HealthUrl) {
        Write-Host "$Name is already running."
        return
    }

    $logPath = Join-Path $LogDir "$Name.log"
    $errorPath = Join-Path $LogDir "$Name.error.log"
    $pidPath = Join-Path $LogDir "$Name.pid"
    $startParameters = @{
        FilePath = 'dotnet'
        ArgumentList = @('run', '--project', $ProjectPath)
        WorkingDirectory = $RootDir
        RedirectStandardOutput = $logPath
        RedirectStandardError = $errorPath
        PassThru = $true
    }
    if ($Name -eq 'api') {
        $startParameters.Environment = @{
            AzureOpenAI__Endpoint = $AzureOpenAiEndpoint
            AzureOpenAI__DeploymentName = $AzureOpenAiDeploymentName
            AzureOpenAI__ApiKey = $AzureOpenAiApiKey
            Authentication__Enabled = $ApiAuthEnabled
            Authentication__Instance = 'https://login.microsoftonline.com/'
            Authentication__TenantId = $EntraTenantId
            Authentication__ClientId = $EntraClientId
            Authentication__Audience = $EntraApplicationIdUri
            Authentication__Audiences__0 = $EntraApplicationIdUri
            Authentication__Audiences__1 = "api://$EntraClientId"
            Authentication__Audiences__2 = $EntraClientId
        }
    }
    elseif ($Name -eq 'mcp' -and $McpAuthEnabled) {
        $startParameters.Environment = @{
            Authentication__Enabled = $McpAuthEnabled
            Authentication__TenantId = $EntraTenantId
            Authentication__ClientId = $EntraClientId
            Authentication__Audience = $EntraApplicationIdUri
            Authentication__Audiences__0 = $EntraApplicationIdUri
            Authentication__Audiences__1 = "api://$EntraClientId"
            Authentication__Audiences__2 = $EntraClientId
        }
    }
    $process = Start-Process @startParameters
    Set-Content -NoNewline -Path $pidPath -Value $process.Id
    Wait-ForEndpoint $HealthUrl $Name
}

Start-DemoService 'api' (Join-Path $RootDir 'src/SanctuaryIntelligence.Api') $ApiHealthUrl
Start-DemoService 'mcp' (Join-Path $RootDir 'src/SanctuaryIntelligence.Mcp') $McpHealthUrl
Wait-ForEndpoint $McpToolsUrl 'MCP tools endpoint'

& devtunnel user login
if ($LASTEXITCODE -ne 0) {
    throw 'Dev Tunnel sign-in failed.'
}

$tunnelLogPath = Join-Path $LogDir 'devtunnel.log'
$tunnelErrorPath = Join-Path $LogDir 'devtunnel.error.log'
$tunnelPidPath = Join-Path $LogDir 'devtunnel.pid'
if (Test-Path $tunnelPidPath) {
    Stop-DemoProcess 'devtunnel'
}

$tunnelProcess = Start-Process -FilePath 'devtunnel' -ArgumentList @('host', $TunnelId) -WorkingDirectory $RootDir -RedirectStandardOutput $tunnelLogPath -RedirectStandardError $tunnelErrorPath -PassThru
Set-Content -NoNewline -Path $tunnelPidPath -Value $tunnelProcess.Id

$tunnelUrl = $null
for ($attempt = 0; $attempt -lt 30; $attempt++) {
    $tunnelOutput = Get-Content -Raw $tunnelLogPath -ErrorAction SilentlyContinue
    $urls = if ($tunnelOutput) {
        [regex]::Matches($tunnelOutput, 'https://[A-Za-z0-9.:-]+') | ForEach-Object { $_.Value.TrimEnd('/') } | Where-Object { $_ -notmatch '-inspect\.' }
    }
    $tunnelUrl = $urls | Select-Object -First 1
    if ($tunnelUrl) {
        break
    }
    Start-Sleep -Seconds 2
}
if (-not $tunnelUrl) {
    throw "Dev Tunnel did not return a public URL. Check $tunnelLogPath."
}

$versionParts = $manifest.version -split '\.'
if ($versionParts.Count -ne 3 -or ($versionParts | Where-Object { $_ -notmatch '^\d+$' })) {
    throw "Manifest version '$($manifest.version)' is not a three-part semantic version."
}
$mcpServerUrl = "$tunnelUrl/mcp"
$currentMcpServerUrl = $manifest.agentConnectors[0].toolSource.remoteMcpServer.mcpServerUrl
$currentReferenceId = $manifest.agentConnectors[0].toolSource.remoteMcpServer.authorization.referenceId
$existingZipPath = Join-Path $DistDir "$PluginArchivePrefix-$($manifest.version).zip"
$packageHasChanged = $false
if (Test-Path $existingZipPath) {
    $zipLastWriteTime = (Get-Item $existingZipPath).LastWriteTimeUtc
    $packageHasChanged = $null -ne (Get-ChildItem -Path $PluginDir -Recurse -File | Where-Object { $_.LastWriteTimeUtc -gt $zipLastWriteTime } | Select-Object -First 1)
}
if ($SkipPackageIfUnchanged -and $currentMcpServerUrl -eq $mcpServerUrl) {
    if ($Plugin -eq 'anonymous' -or $currentReferenceId -eq $AuthConfigId) {
        if ((Test-Path $existingZipPath) -and -not $packageHasChanged) {
            Write-Host "MCP URL: $mcpServerUrl"
            Write-Host "Plugin mode: $Plugin"
            Write-Host "Plugin ZIP unchanged: $existingZipPath"
            exit 0
        }
    }
}

$nextVersion = "$($versionParts[0]).$($versionParts[1]).$([int]$versionParts[2] + 1)"

$manifest.version = $nextVersion
$manifest.agentConnectors[0].toolSource.remoteMcpServer.mcpServerUrl = $mcpServerUrl
if ($Plugin -eq 'entra') {
    $manifest.agentConnectors[0].toolSource.remoteMcpServer.authorization.referenceId = $AuthConfigId
}
$manifest | ConvertTo-Json -Depth 20 | Set-Content -Path $ManifestPath -Encoding utf8

$zipPath = Join-Path $DistDir "$PluginArchivePrefix-$nextVersion.zip"
Compress-Archive -Path (Join-Path $PluginDir '*') -DestinationPath $zipPath -Force

Write-Host "MCP URL: $mcpServerUrl"
Write-Host "Plugin mode: $Plugin"
Write-Host "Plugin version: $nextVersion"
Write-Host "Plugin ZIP: $zipPath"
Write-Host "Stop local processes with: .\script\prepare-cowork-plugin.ps1 -Stop"