# Cleanup orphaned WebView2 processes
# Run this if WebView2 processes are still running after closing the app

Write-Host "Cleaning up WebView2 processes..." -ForegroundColor Cyan

$processes = Get-Process -Name "msedgewebview2" -ErrorAction SilentlyContinue

if ($processes) {
    Write-Host "Found $($processes.Count) WebView2 process(es)" -ForegroundColor Yellow
    
    foreach ($process in $processes) {
        try {
            Write-Host "Stopping process ID: $($process.Id)" -ForegroundColor Gray
            Stop-Process -Id $process.Id -Force
        }
        catch {
            Write-Host "Failed to stop process $($process.Id): $_" -ForegroundColor Red
        }
    }
    
    Write-Host "Cleanup completed!" -ForegroundColor Green
}
else {
    Write-Host "No WebView2 processes found" -ForegroundColor Green
}

# Also check for Contextualizer processes
$appProcesses = Get-Process | Where-Object {$_.ProcessName -like "*Contextualizer*" -or $_.ProcessName -like "*WpfInteractionApp*"} -ErrorAction SilentlyContinue

if ($appProcesses) {
    Write-Host "Found application process(es), stopping..." -ForegroundColor Yellow
    foreach ($process in $appProcesses) {
        try {
            Stop-Process -Id $process.Id -Force
            Write-Host "Stopped process: $($process.ProcessName) (ID: $($process.Id))" -ForegroundColor Gray
        }
        catch {
            Write-Host "Failed to stop process: $_" -ForegroundColor Red
        }
    }
}

