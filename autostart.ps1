function Test-Port {
    param([int]$port)
    $conns = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
    if ($conns) { return $true }
    return $false
}

# 1. Kiem tra va khoi dong Backend API (.NET 8)
if (-not (Test-Port -port 5029)) {
    $dllPath = "C:\Project_Vibe\Cinema\bin\Debug\net8.0\MovieStreaming.dll"
    if (Test-Path $dllPath) {
        Start-Process -FilePath "dotnet" -ArgumentList "`"$dllPath`" --environment Development --contentRoot `"C:\Project_Vibe\Cinema`" --urls `"http://localhost:5029`"" -WorkingDirectory "C:\Project_Vibe\Cinema" -WindowStyle Hidden
    } else {
        Start-Process -FilePath "dotnet" -ArgumentList "run --launch-profile http" -WorkingDirectory "C:\Project_Vibe\Cinema" -WindowStyle Hidden
    }
}

# 2. Kiem tra va khoi dong Frontend (Vite)
if (-not (Test-Port -port 5173)) {
    Start-Process -FilePath "cmd.exe" -ArgumentList "/c npm run dev" -WorkingDirectory "C:\Project_Vibe\Cinema\client" -WindowStyle Hidden
}
