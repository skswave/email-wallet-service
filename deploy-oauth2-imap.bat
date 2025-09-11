@echo off
echo ================================================
echo Deploying OAuth2 IMAP Support for Office 365
echo ================================================

echo Adding all files to git...
git add .

echo.
echo Committing OAuth2 IMAP implementation...
git commit -m "Add OAuth2 support for Office 365 IMAP authentication

Features:
- OAuth2 client credentials flow for IMAP authentication
- Support for both basic auth and OAuth2 authentication methods
- Automatic token refresh with proper expiry handling
- Enhanced IMAP service with modern authentication
- Configurable OAuth2 settings for Azure AD integration

Configuration required:
- Azure AD app registration with Mail.Read permissions
- Client ID, Client Secret, and Tenant ID configuration
- Set OAuth2:Enabled to true to use OAuth2 instead of basic auth

This resolves Office 365 modern authentication requirements."

echo.
echo Pushing to repository...
git push origin main

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ================================================
    echo Successfully deployed OAuth2 IMAP support!
    echo ================================================
    echo.
    echo Next Steps - Azure App Registration:
    echo 1. Go to Azure Portal ^> Azure AD ^> App registrations
    echo 2. Create new app registration for "Email Wallet IMAP Service"
    echo 3. Add API permissions: Mail.Read, IMAP.AccessAsUser.All
    echo 4. Generate client secret
    echo 5. Update server configuration with OAuth2 credentials
    echo 6. Set OAuth2:Enabled to true
    echo.
    echo Server deployment will use OAuth2 for modern Office 365 authentication
    echo.
) else (
    echo Git push failed. Please check the error above.
)

pause