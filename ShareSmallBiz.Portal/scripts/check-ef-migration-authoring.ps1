[CmdletBinding()]
param(
    [string]$Context = "ShareSmallBizUserContext",
    [string]$MigrationName = "MigrationAuthoringCheck",
    [string]$OutputDir = "Migrations",
    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$projectFile = Join-Path $projectRoot "ShareSmallBiz.Portal.csproj"
$migrationsPath = Join-Path $projectRoot $OutputDir
$snapshotPath = Join-Path $migrationsPath "ShareSmallBizUserContextModelSnapshot.cs"
$migrationSearchPattern = "*_${MigrationName}.cs"
$designerSearchPattern = "*_${MigrationName}.Designer.cs"

function Get-ScopedGitStatus {
    param([string]$RelativePath)

    $gitCommand = Get-Command git -ErrorAction SilentlyContinue
    if (-not $gitCommand) {
        return @()
    }

    $statusOutput = & $gitCommand.Source status --porcelain -- $RelativePath
    return @($statusOutput | Where-Object { $_ -and $_.Trim().Length -gt 0 })
}

function Get-FileHashOrNull {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        return $null
    }

    return (Get-FileHash -Algorithm SHA256 -Path $Path).Hash
}

function Set-Utf8BomContent {
    param(
        [string]$Path,
        [string]$Content
    )

    $utf8BomEncoding = [System.Text.UTF8Encoding]::new($true)
    [System.IO.File]::WriteAllText($Path, $Content, $utf8BomEncoding)
}

function Test-EmptyMigrationFile {
    param([string]$Path)

    $content = Get-Content -Raw -Path $Path
    $normalized = $content -replace "`r`n", "`n"
    $upPattern = 'protected override void Up\(MigrationBuilder migrationBuilder\)\s*\{\s*\}'
    $downPattern = 'protected override void Down\(MigrationBuilder migrationBuilder\)\s*\{\s*\}'

    return ($normalized -match $upPattern) -and ($normalized -match $downPattern)
}

function Get-GeneratedMigrationFile {
    param(
        [string]$Path,
        [string]$Filter,
        [bool]$IncludeDesigner
    )

    return Get-ChildItem -Path $Path -File -Filter $Filter |
        Where-Object { $IncludeDesigner -or $_.Name -notlike "*.Designer.cs" } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
}

if (-not (Test-Path $projectFile)) {
    throw "Could not locate ShareSmallBiz.Portal.csproj under $projectRoot."
}

if (-not (Test-Path $migrationsPath)) {
    throw "Could not locate the unified migrations folder at $migrationsPath."
}

Push-Location $projectRoot

$snapshotHashBefore = $null
$snapshotContentBefore = $null
$snapshotHashAfterAdd = $null
$scaffoldedMigration = $false
$failureMessage = $null
$cleanupFailureMessage = $null
$generatedMigrationPath = $null
$generatedDesignerPath = $null

try {
    $pendingMigrationStatus = @(Get-ScopedGitStatus -RelativePath $OutputDir)
    if ($pendingMigrationStatus.Count -gt 0) {
        throw "The unified migrations folder has pending git changes. Run this check from a clean Migrations worktree."
    }

    $preexistingMatches = @(Get-ChildItem -Path $migrationsPath -File -Filter $migrationSearchPattern |
        Where-Object { $_.Name -notlike "*.Designer.cs" })
    if ($preexistingMatches.Count -gt 0) {
        throw "A migration matching '$MigrationName' already exists in $OutputDir. Choose a different migration name for the check."
    }

    & dotnet tool restore

    $snapshotHashBefore = Get-FileHashOrNull -Path $snapshotPath
    $snapshotContentBefore = Get-Content -Raw -Path $snapshotPath
    $addArguments = @("tool", "run", "dotnet-ef", "migrations", "add", $MigrationName, "--output-dir", $OutputDir, "--context", $Context)
    if ($NoBuild) {
        $addArguments += "--no-build"
    }

    & dotnet @addArguments
    $scaffoldedMigration = $true

    $migrationFile = Get-GeneratedMigrationFile -Path $migrationsPath -Filter $migrationSearchPattern -IncludeDesigner:$false
    $designerFile = Get-GeneratedMigrationFile -Path $migrationsPath -Filter $designerSearchPattern -IncludeDesigner:$true

    if ($null -eq $migrationFile -or $null -eq $designerFile) {
        throw "The migration check did not scaffold the expected files into $OutputDir."
    }

    $generatedMigrationPath = $migrationFile.FullName
    $generatedDesignerPath = $designerFile.FullName

    if (-not (Test-EmptyMigrationFile -Path $migrationFile.FullName)) {
        throw "The migration check scaffolded schema operations in $($migrationFile.Name). Review the model changes before authoring the next real migration."
    }

    $snapshotHashAfterAdd = Get-FileHashOrNull -Path $snapshotPath
    if ($snapshotHashBefore -ne $snapshotHashAfterAdd) {
        throw "The model snapshot changed during the migration check. Future migration authoring is not clean yet."
    }
}
catch {
    $failureMessage = $_.Exception.Message
}
finally {
    if ($scaffoldedMigration) {
        try {
            & dotnet tool run dotnet-ef migrations remove --force --no-build

            if ($generatedMigrationPath -and (Test-Path $generatedMigrationPath)) {
                Remove-Item -Path $generatedMigrationPath -Force
            }

            if ($generatedDesignerPath -and (Test-Path $generatedDesignerPath)) {
                Remove-Item -Path $generatedDesignerPath -Force
            }

            if ($generatedMigrationPath -and (Test-Path $generatedMigrationPath)) {
                throw "The migration check cleanup left the generated migration file behind in $OutputDir."
            }

            if ($generatedDesignerPath -and (Test-Path $generatedDesignerPath)) {
                throw "The migration check cleanup left the generated designer file behind in $OutputDir."
            }

            $snapshotHashAfterCleanup = Get-FileHashOrNull -Path $snapshotPath
            if ($snapshotHashBefore -ne $snapshotHashAfterCleanup) {
                Set-Utf8BomContent -Path $snapshotPath -Content $snapshotContentBefore
                $snapshotHashAfterCleanup = Get-FileHashOrNull -Path $snapshotPath
                if ($snapshotHashBefore -ne $snapshotHashAfterCleanup) {
                    throw "The model snapshot did not return to its original state after cleanup."
                }
            }
        }
        catch {
            $cleanupFailureMessage = $_.Exception.Message
        }
    }

    Pop-Location
}

if ($cleanupFailureMessage) {
    if ($failureMessage) {
        throw "$failureMessage Cleanup also failed: $cleanupFailureMessage"
    }

    throw $cleanupFailureMessage
}

if ($failureMessage) {
    throw $failureMessage
}

Write-Host "Migration authoring check passed. A fresh temporary migration scaffolded empty in $OutputDir and cleanup restored the snapshot unchanged."