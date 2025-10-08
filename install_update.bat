@echo off
REM ============================================
REM Contextualizer Network Update Script
REM ============================================
REM This script should be placed in the network share:
REM \\server\share\Contextualizer\Updates\install_update.bat
REM
REM Parameters:
REM %1 = Current EXE path (e.g., C:\PortableApps\Contextualizer\Contextualizer.exe)
REM %2 = Temp update path (e.g., C:\Users\...\AppData\Local\Temp\...)
REM %3 = Backup path (e.g., C:\PortableApps\Contextualizer\Contextualizer.exe.backup)
REM ============================================

setlocal

REM Get parameters
set "CURRENT_EXE=%~1"
set "TEMP_UPDATE=%~2"
set "BACKUP_PATH=%~3"

echo ============================================
echo Contextualizer Network Update
echo ============================================
echo.
echo Current EXE: %CURRENT_EXE%
echo Update File: %TEMP_UPDATE%
echo Backup Path: %BACKUP_PATH%
echo.

REM Wait for application to close
echo Waiting for application to close...
timeout /t 2 /nobreak > nul

REM Terminate application if still running
echo Terminating Contextualizer.exe...
taskkill /f /im "Contextualizer.exe" > nul 2>&1
timeout /t 1 /nobreak > nul

REM Create backup
echo Creating backup...
copy "%CURRENT_EXE%" "%BACKUP_PATH%" > nul 2>&1
if errorlevel 1 (
    echo ERROR: Failed to create backup!
    pause
    exit /b 1
)
echo Backup created successfully.

REM Install update
echo Installing update...
copy /Y "%TEMP_UPDATE%" "%CURRENT_EXE%" > nul 2>&1
if errorlevel 1 (
    echo ERROR: Failed to install update!
    echo Restoring backup...
    copy /Y "%BACKUP_PATH%" "%CURRENT_EXE%" > nul 2>&1
    echo Update installation failed.
    pause
    exit /b 1
)
echo Update installed successfully.

REM Cleanup
echo Cleaning up...
del "%BACKUP_PATH%" > nul 2>&1
del "%TEMP_UPDATE%" > nul 2>&1

REM Restart application
echo Restarting Contextualizer...
start "" "%CURRENT_EXE%"

echo.
echo ============================================
echo Update completed successfully!
echo ============================================
timeout /t 2 /nobreak > nul

endlocal
exit /b 0

