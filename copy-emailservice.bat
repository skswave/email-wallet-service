@echo off
echo 🚀 Copying EmailProcessingService to repository structure...
echo.

set "SOURCE=C:\Users\StevenSprague\OneDrive - Rivetz Corp\Rootz\claud project\email-data-wallet-service\implementation\EmailProcessingService"
set "DEST=C:\Users\StevenSprague\OneDrive - Rivetz Corp\Rootz\claud project\email-wallet-service-repo\src\EmailProcessingService"

echo Source: %SOURCE%
echo Destination: %DEST%
echo.

if not exist "%SOURCE%" (
    echo ❌ Source directory not found: %SOURCE%
    pause
    exit /b 1
)

echo Copying files...
robocopy "%SOURCE%" "%DEST%" /E /NP
if %ERRORLEVEL% LSS 8 (
    echo ✅ EmailProcessingService copied successfully
    echo.
    echo 📁 Files copied to: %DEST%
) else (
    echo ❌ Error copying files (Error level: %ERRORLEVEL%)
)

echo.
echo Repository structure ready for Git!
echo.
echo Next steps:
echo 1. Navigate to: cd "C:\Users\StevenSprague\OneDrive - Rivetz Corp\Rootz\claud project\email-wallet-service-repo"
echo 2. Initialize Git: git init
echo 3. Add remote: git remote add origin https://github.com/skswave/email-wallet-service.git
echo 4. Add files: git add .
echo 5. Commit: git commit -m "Initial commit: Complete Email Wallet Service"
echo 6. Push: git push -u origin main
echo.
pause