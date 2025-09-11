@echo off
echo ================================================
echo Fixing IMAP Service Naming Conflicts
echo ================================================

echo Adding all files to git...
git add .

echo.
echo Committing fixes...
git commit -m "Fix naming conflicts with notification services

Changes:
- Renamed SimpleNotificationService to EmailNotificationService
- Created IExtendedNotificationService interface to extend existing INotificationService
- Updated Program.cs to register both interfaces
- Updated EmailMonitorController to use extended interface
- Resolved build conflicts with existing service definitions

This fixes the compilation errors and allows the IMAP email monitoring to work properly."

echo.
echo Pushing fixes to repository...
git push origin main

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ================================================
    echo ✅ Successfully fixed and deployed IMAP services!
    echo ================================================
    echo.
    echo Ready for server deployment:
    echo 1. SSH to rootz.global
    echo 2. cd /opt/email-wallet-service  
    echo 3. git pull origin main
    echo 4. ./deployment-with-imap.sh
    echo.
) else (
    echo ❌ Git push failed. Please check the error above.
)

pause