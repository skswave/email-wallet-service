@echo off
echo ================================================
echo Deploying Microsoft Graph API Email Monitoring
echo ================================================

echo Adding all files to git...
git add .

echo.
echo Committing Microsoft Graph API implementation...
git commit -m "Implement Microsoft Graph API email monitoring as IMAP alternative

Features:
- Microsoft Graph API-based email monitoring service
- Uses same OAuth2 credentials as IMAP (reuses existing tokens)
- Supports reading unread emails, marking as read
- Compatible with existing email processing pipeline
- More reliable than IMAP OAuth2 for Office 365
- Automatic conversion from Graph API to MimeMessage format

Configuration:
- Disables IMAP monitoring (Enabled: false)
- Enables Graph API monitoring (MicrosoftGraph:Enabled: true)
- Uses existing Entra ID app registration credentials
- Requires Mail.Read and Mail.ReadWrite Graph API permissions

This resolves Office 365 IMAP OAuth2 authentication issues by using Microsoft's recommended Graph API instead."

echo.
echo Pushing to repository...
git push origin main

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ================================================
    echo Successfully deployed Microsoft Graph API solution!
    echo ================================================
    echo.
    echo Next Steps:
    echo 1. Verify Entra ID app has Mail.Read ^& Mail.ReadWrite permissions
    echo 2. Deploy to server and update configuration
    echo 3. Test Graph API email monitoring
    echo.
    echo This approach bypasses IMAP OAuth2 issues entirely!
    echo.
) else (
    echo Git push failed. Please check the error above.
)

pause