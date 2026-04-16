$hub = "C:\\Program Files\\Unity Hub\\Unity Hub.exe"

if (-not (Test-Path $hub)) {
    Write-Error "Unity Hub is not installed in the expected path."
    exit 1
}

Start-Process -FilePath $hub
Write-Host "Unity Hub launched. Sign in and activate your license, then continue with docs/03-unity-setup.md."
