$results = [ordered]@{}

$git = Get-Command git -ErrorAction SilentlyContinue
$results["Git"] = if ($git) { $git.Source } else { "MISSING" }

$unityHubCandidates = @(
    "C:\\Program Files\\Unity Hub\\Unity Hub.exe",
    "C:\\Users\\fmied\\AppData\\Local\\Programs\\Unity Hub\\Unity Hub.exe"
)
$unityHub = $unityHubCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
$results["Unity Hub"] = if ($unityHub) { $unityHub } else { "MISSING" }

$hubEditorRoot = "C:\\Program Files\\Unity\\Hub\\Editor"
$directEditorRoots = Get-ChildItem "C:\\Program Files" -Directory -Filter "Unity *" -ErrorAction SilentlyContinue |
    Where-Object { Test-Path (Join-Path $_.FullName "Editor\\Unity.exe") } |
    Select-Object -ExpandProperty FullName

$hubEditors = @()
if (Test-Path $hubEditorRoot) {
    $hubEditors = Get-ChildItem $hubEditorRoot -Directory -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name
}

$allEditors = @()
if ($hubEditors) {
    $allEditors += $hubEditors
}
if ($directEditorRoots) {
    $allEditors += $directEditorRoots
}

$results["Unity Editors"] = if ($allEditors) { ($allEditors -join ", ") } else { "MISSING" }

$vswhere = "C:\\Program Files (x86)\\Microsoft Visual Studio\\Installer\\vswhere.exe"
if (Test-Path $vswhere) {
    $vsPath = & $vswhere -latest -products * -property installationPath
    $results["Visual Studio"] = if ($vsPath) { $vsPath } else { "MISSING" }
} else {
    $results["Visual Studio"] = "MISSING"
}

$openXrKeys = @(
    "HKLM:\\SOFTWARE\\Khronos\\OpenXR\\1",
    "HKLM:\\SOFTWARE\\WOW6432Node\\Khronos\\OpenXR\\1"
)
$activeRuntime = $null
foreach ($key in $openXrKeys) {
    if (Test-Path $key) {
        $value = (Get-ItemProperty -Path $key -ErrorAction SilentlyContinue).ActiveRuntime
        if ($value) {
            $activeRuntime = $value
            break
        }
    }
}
$results["OpenXR Runtime"] = if ($activeRuntime) { $activeRuntime } else { "NOT CONFIGURED" }

$steamVrCandidate = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\SteamVR\\bin\\win64\\vrmonitor.exe"
$results["SteamVR"] = if (Test-Path $steamVrCandidate) { $steamVrCandidate } else { "NOT FOUND" }

Write-Host ""
Write-Host "== VR Chess environment check =="
Write-Host ""

foreach ($entry in $results.GetEnumerator()) {
    Write-Host ("{0,-16}: {1}" -f $entry.Key, $entry.Value)
}

Write-Host ""
Write-Host "Recommended next step:"
if ($results["Unity Hub"] -eq "MISSING") {
    Write-Host "- Install Unity Hub, then Unity 6.3 LTS and Visual Studio 2022."
} elseif ($results["Unity Editors"] -eq "MISSING" -or $results["Unity Editors"] -eq "NONE FOUND") {
    Write-Host "- Install Unity 6.3 LTS editor with Windows Build Support."
} elseif (Test-Path "Assets\\Scenes\\Sandbox.unity") {
    Write-Host "- Open the repo in Unity, let it finish importing, then verify the Sandbox scene."
} else {
    Write-Host "- Open the repo in Unity and follow docs/03-unity-setup.md."
}
