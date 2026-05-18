param(
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $scriptDir "chessVR-headset-test.zip"
}

$projectRootFull = [System.IO.Path]::GetFullPath($projectRoot).TrimEnd('\', '/')
$outputFull = [System.IO.Path]::GetFullPath($OutputPath)
$outputParent = Split-Path -Parent $outputFull

if (-not (Test-Path -LiteralPath $outputParent)) {
    New-Item -ItemType Directory -Path $outputParent | Out-Null
}

if (Test-Path -LiteralPath $outputFull) {
    Remove-Item -LiteralPath $outputFull -Force
}

$excludedTopLevelDirs = @(
    ".git",
    ".vs",
    ".vscode",
    ".idea",
    "Library",
    "Temp",
    "Logs",
    "UserSettings",
    "Build",
    "Builds",
    "Obj"
)

$excludedExtensions = @(
    ".csproj",
    ".sln",
    ".suo",
    ".tmp",
    ".user",
    ".userprefs",
    ".pidb",
    ".booproj",
    ".svd",
    ".pdb",
    ".mdb",
    ".opendb",
    ".VC.db"
)

$excludedFileNames = @(
    "Thumbs.db",
    "Desktop.ini",
    ".DS_Store",
    "packages-lock.json"
)

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$zip = [System.IO.Compression.ZipFile]::Open($outputFull, [System.IO.Compression.ZipArchiveMode]::Create)
$addedCount = 0

try {
    Get-ChildItem -LiteralPath $projectRootFull -Recurse -Force -File | ForEach-Object {
        $fullName = [System.IO.Path]::GetFullPath($_.FullName)

        if ($fullName -ieq $outputFull) {
            return
        }

        $relative = $fullName.Substring($projectRootFull.Length).TrimStart('\', '/')
        $relativeZip = $relative.Replace('\', '/')
        $topLevel = ($relativeZip -split '/')[0]

        if ($excludedTopLevelDirs -icontains $topLevel) {
            return
        }

        if ($relativeZip -like "VR_Headset_Handoff/*.zip") {
            return
        }

        if ($excludedFileNames -icontains $_.Name) {
            return
        }

        foreach ($extension in $excludedExtensions) {
            if ($_.Name.EndsWith($extension, [System.StringComparison]::OrdinalIgnoreCase)) {
                return
            }
        }

        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $zip,
            $fullName,
            $relativeZip,
            [System.IO.Compression.CompressionLevel]::Optimal
        ) | Out-Null

        $addedCount++
    }
}
finally {
    $zip.Dispose()
}

$sizeMb = [Math]::Round((Get-Item -LiteralPath $outputFull).Length / 1MB, 2)

Write-Host "Created zip:"
Write-Host $outputFull
Write-Host "Files included: $addedCount"
Write-Host "Size: $sizeMb MB"
Write-Host ""
Write-Host "Copy this zip to the computer with the headset and follow README_START_TUTAJ.md."

