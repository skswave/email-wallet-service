@echo off
echo ================================================
echo Deploying Single Email Configuration for IMAP
echo ================================================

echo Adding all files to git...
git add .

echo.
echo Committing single email configuration...
git commit -m "Consolidate to single email account for IMAP and SMTP

Changes:
- Use process@rivetz.com for both receiving (IMAP) and sending (SMTP)
- Update appsettings.json to use single email credentials
- Update production template configuration
- Simplify email management with unified account

Benefits:
- Better user experience with consistent FROM/TO addresses
- Reduced spam risk as users expect replies from same address
- Simplified configuration with single set of credentials
- Natural conversation flow for email wallet process"

echo.
echo Pushing to repository...
git push origin main

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ================================================
    echo ✅ Successfully deployed single email configuration!
    echo ================================================
    echo.
    echo Email Configuration Summary:
    echo - IMAP Monitoring: process@rivetz.com
    echo - SMTP Sending: process@rivetz.com  
    echo - User Journey: Send TO process@rivetz.com, get replies FROM process@rivetz.com
    echo.
    echo Ready for server deployment with single email setup!
    echo.
) else (
    echo ❌ Git push failed. Please check the error above.
)

pause