$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot '../prepare-cowork-plugin.ps1'
$tokens = $null
$parseErrors = $null
$scriptAst = [System.Management.Automation.Language.Parser]::ParseFile($scriptPath, [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count -gt 0) {
    throw "Script has syntax errors: $parseErrors"
}

$urlAssignment = $scriptAst.Find({
    param($node)
    $node -is [System.Management.Automation.Language.AssignmentStatementAst] -and
        $node.Left.Extent.Text -eq '$urls'
}, $true)
if (-not $urlAssignment) {
    throw 'Tunnel URL extraction was not found.'
}
$extractUrls = [scriptblock]::Create($urlAssignment.Extent.Text)
$temporaryLog = [System.IO.Path]::GetTempFileName()

try {
    foreach ($case in @(
        @{ Name = 'Empty log'; Output = ''; Expected = $null },
        @{ Name = 'Startup output without URL'; Output = 'Starting tunnel...'; Expected = $null },
        @{ Name = 'Inspector URL only'; Output = 'https://demo-inspect.euw.devtunnels.ms'; Expected = $null },
        @{ Name = 'Public URL'; Output = 'https://demo-3001.euw.devtunnels.ms/'; Expected = 'https://demo-3001.euw.devtunnels.ms' },
        @{ Name = 'Inspector before public URL'; Output = "https://demo-inspect.euw.devtunnels.ms`nhttps://demo-3001.euw.devtunnels.ms/"; Expected = 'https://demo-3001.euw.devtunnels.ms' }
    )) {
        [System.IO.File]::WriteAllText($temporaryLog, $case.Output)
        $tunnelOutput = Get-Content -Raw $temporaryLog
        $urls = $null
        . $extractUrls
        $actual = $urls | Select-Object -First 1
        if ($actual -ne $case.Expected) {
            throw "$($case.Name): expected '$($case.Expected)', got '$actual'."
        }
        Write-Output "PASS: $($case.Name)"
    }
}
finally {
    Remove-Item $temporaryLog -Force
}