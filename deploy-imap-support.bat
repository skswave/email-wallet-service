@echo off
echo ================================================
echo Deploying Email Wallet Service with IMAP Support
echo ================================================

echo Adding all files to git...
git add .

echo.
echo Committing changes...
git commit -m "Add IMAP email monitoring support for process@rivetz.com

Features added:
- ImapEmailMonitorService for automated email monitoring
- SimpleNotificationService for user notifications 
- EmailMonitorController for API management
- Updated configuration for IMAP and SMTP settings
- Enhanced Program.cs with IMAP service registration
- Production deployment scripts and templates

This completes the email wallet system by adding:
1. Automated monitoring of process@rivetz.com inbox
2. Real-time email processing with IMAP
3. User notification system via email
4. API endpoints for monitoring and testing
5. Background service for continuous email monitoring

Configuration required:
- Set Email:Imap:Password for process@rivetz.com
- Set Email:SmtpSettings:Password for notifications
- Configure user notification email mappings"

echo.
echo Pushing to repository...
git push origin main

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ================================================
    echo ✅ Successfully deployed IMAP email monitoring!
    echo ================================================
    echo.
    echo Next steps:
    echo 1. Update server configuration with email passwords
    echo 2. Run deployment script on rootz.global server
    echo 3. Test IMAP connectivity and email processing
    echo 4. Configure email forwarding to process@rivetz.com
    echo.
    echo Key endpoints to test:
    echo - http://rootz.global:5000/api/emailmonitor/status
    echo - http://rootz.global:5000/api/emailmonitor/test-connection
    echo - http://rootz.global:5000/swagger
    echo.
) else (
    echo ❌ Git push failed. Please check the error above.
)

pause