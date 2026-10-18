<#
.SYNOPSIS
    Builds CombatFlow and produces a Thunderstore-ready zip.

.DESCRIPTION
    Builds Release, checks that package/manifest.json agrees with PluginInfo.Version,
    stages the five package files and zips them with the DLLs at the archive root.

    The zip is reopened and verified entry by entry afterwards. Windows Defender
    briefly locks a freshly written DLL while it scans it; Compress-Archive reports
    that as a non-terminating error and still leaves a partial archive behind, which
    is how a zip missing its DLL ends up on Thunderstore.

.EXAMPLE
    pwsh -File build/Package.ps1
#>
[CmdletBinding()]
param(
    [string] $Configuration = 'Release'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$outDir = Join-Path $root 'build/out'
$stageDir = Join-Path $outDir 'stage'

# --- version agreement -----------------------------------------------------

$pluginInfo = Get-Content (Join-Path $root 'src/CombatFlow.Plugin/PluginInfo.cs') -Raw
if ($pluginInfo -notmatch 'Version\s*=\s*"([^"]+)"') {
    throw 'Could not read Version from PluginInfo.cs.'
}
$sourceVersion = $Matches[1]

$manifestPath = Join-Path $root 'package/manifest.json'
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
if ($manifest.version_number -ne $sourceVersion) {
    throw "Version mismatch: PluginInfo.cs says $sourceVersion, manifest.json says $($manifest.version_number)."
}

Write-Host "Packaging CombatFlow $sourceVersion ($Configuration)"

# --- build -----------------------------------------------------------------

# Deploy is left off, so packaging never writes into a play profile.
& dotnet build (Join-Path $root 'CombatFlow.sln') -c $Configuration -v m -nologo
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE."
}

# --- stage -----------------------------------------------------------------

if (Test-Path $stageDir) {
    Remove-Item $stageDir -Recurse -Force
}
New-Item -ItemType Directory -Path $stageDir -Force | Out-Null

$expected = @(
    @{ Source = "src/CombatFlow.Plugin/bin/$Configuration/CombatFlow.dll"; Name = 'CombatFlow.dll' }
    @{ Source = "src/CombatFlow.Plugin/bin/$Configuration/CombatFlow.Core.dll"; Name = 'CombatFlow.Core.dll' }
    @{ Source = 'package/manifest.json'; Name = 'manifest.json' }
    @{ Source = 'package/README.md'; Name = 'README.md' }
    @{ Source = 'package/CHANGELOG.md'; Name = 'CHANGELOG.md' }
    @{ Source = 'package/icon.png'; Name = 'icon.png' }
)

foreach ($item in $expected) {
    $source = Join-Path $root $item.Source
    if (-not (Test-Path $source)) {
        throw "Missing required package file: $($item.Source)"
    }
    Copy-Item $source (Join-Path $stageDir $item.Name) -Force
}

# Thunderstore requires the icon to be exactly 256x256.
Add-Type -AssemblyName System.Drawing
$icon = [System.Drawing.Image]::FromFile((Join-Path $stageDir 'icon.png'))
try {
    if ($icon.Width -ne 256 -or $icon.Height -ne 256) {
        throw "icon.png must be exactly 256x256, found $($icon.Width)x$($icon.Height)."
    }
}
finally {
    $icon.Dispose()
}

# --- zip, then verify ------------------------------------------------------

$zipPath = Join-Path $outDir "CombatFlow-$sourceVersion.zip"

# @() everywhere a pipeline result is counted: Where-Object yields a bare object
# rather than an array when it matches exactly once, and under StrictMode reading
# .Count on that throws instead of returning 1.
$expectedNames = @($expected | ForEach-Object { $_.Name })

$attempt = 0
while ($true) {
    $attempt++

    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }

    Compress-Archive -Path (Join-Path $stageDir '*') -DestinationPath $zipPath -ErrorAction SilentlyContinue

    $missing = @()
    if (Test-Path $zipPath) {
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $archive = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
        try {
            $present = @($archive.Entries | ForEach-Object { $_.FullName })
            $missing = @($expectedNames | Where-Object { $present -notcontains $_ })
        }
        finally {
            $archive.Dispose()
        }
    }
    else {
        $missing = @($expectedNames)
    }

    if ($missing.Count -eq 0) {
        break
    }

    if ($attempt -ge 5) {
        throw "Zip is incomplete after $attempt attempts. Missing: $($missing -join ', ')"
    }

    Write-Warning "Incomplete zip (missing $($missing -join ', ')), retrying in 2s..."
    Start-Sleep -Seconds 2
}

Write-Host "  -> $zipPath"
Write-Host 'Verified all entries present. Ready to upload.'
