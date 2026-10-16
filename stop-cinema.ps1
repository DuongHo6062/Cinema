[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$Host.UI.RawUI.WindowTitle = "SHIN Movie Stopper"

Write-Host "========================================================" -ForegroundColor Red
Write-Host "                 DỪNG SHIN MOVIE                        " -ForegroundColor Yellow
Write-Host "========================================================" -ForegroundColor Red
Write-Host ""

function Stop-PortProcess {
    param([int]$port, [string]$name)
    Write-Host "Dang kiem tra va tat $name (cong $port)..." -ForegroundColor White
    try {
        $conns = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
        if ($conns) {
            foreach ($conn in $conns) {
                $pidToKill = $conn.OwningProcess
                if ($pidToKill -and $pidToKill -ne 0) {
                    Write-Host "  -> Dung PID $pidToKill" -ForegroundColor Yellow
                    Stop-Process -Id $pidToKill -Force -ErrorAction SilentlyContinue
                }
            }
        } else {
            Write-Host "  -> Cong $port khong co tien trinh nao chay." -ForegroundColor Gray
        }
    } catch {
        Write-Host "  -> Khong the dung tien trinh cong $port" -ForegroundColor Red
    }
}

Stop-PortProcess -port 5029 -name "Backend API"
Stop-PortProcess -port 5173 -name "Frontend"

Write-Host ""
Write-Host "========================================================" -ForegroundColor Green
Write-Host "            DA TAT XONG SHIN MOVIE!                     " -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
Start-Sleep -Seconds 2
