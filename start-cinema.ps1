[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$Host.UI.RawUI.WindowTitle = "SHIN Movie Launcher"

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "             KHOI DONG SHIN MOVIE                      " -ForegroundColor Yellow
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

function Test-Port {
    param([int]$port)
    $conns = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
    if ($conns) {
        return $true
    }
    return $false
}

# 1. Kiem tra Backend (Port 5029)
$backendOk = Test-Port -port 5029
if (-not $backendOk) {
    Write-Host "[1/3] Dang khoi dong Backend API (.NET 8)..." -ForegroundColor White
    Start-Process -FilePath "dotnet" -ArgumentList "run --launch-profile http" -WorkingDirectory "C:\Project_Vibe\Cinema" -WindowStyle Minimized
} else {
    Write-Host "[1/3] Backend API da san sang (Port 5029)." -ForegroundColor Green
}

# 2. Kiem tra Frontend (Port 5173)
$frontendOk = Test-Port -port 5173
if (-not $frontendOk) {
    Write-Host "[2/3] Dang khoi dong Frontend (Vite React)..." -ForegroundColor White
    Start-Process -FilePath "cmd.exe" -ArgumentList "/c npm run dev" -WorkingDirectory "C:\Project_Vibe\Cinema\client" -WindowStyle Minimized
} else {
    Write-Host "[2/3] Frontend da san sang (Port 5173)." -ForegroundColor Green
}

# 3. Doi may chu san sang
Write-Host "[3/3] Dang ket noi may chu..." -ForegroundColor Yellow
$timeout = 25
$elapsed = 0
while ($elapsed -lt $timeout) {
    $bReady = Test-Port -port 5029
    $fReady = Test-Port -port 5173
    if ($bReady -and $fReady) {
        break
    }
    Start-Sleep -Seconds 1
    $elapsed++
}

# 4. Mo trinh duyet
Write-Host ""
Write-Host "========================================================" -ForegroundColor Green
Write-Host "  DA SAN SANG! DANG MO TRINH DUYET..." -ForegroundColor Green
Write-Host "  Dia chi: http://localhost:5173" -ForegroundColor White
Write-Host "========================================================" -ForegroundColor Green

Start-Process "http://localhost:5173"
Start-Sleep -Seconds 2
