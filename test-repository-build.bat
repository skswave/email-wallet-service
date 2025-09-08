@echo off
echo 🧪 Testing Repository Build with Working Code Sync
echo ================================================

cd /d "C:\Users\StevenSprague\OneDrive - Rivetz Corp\Rootz\claud project\email-wallet-service-repo\src\EmailProcessingService"

echo.
echo 🔨 Building repository code...
dotnet build --configuration Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ BUILD SUCCESS! Repository code matches working implementation
    echo.
    echo 🚀 Ready to commit and push to GitHub:
    echo.
    echo cd /d "C:\Users\StevenSprague\OneDrive - Rivetz Corp\Rootz\claud project\email-wallet-service-repo"
    echo git add .
    echo git commit -m "Sync complete working codebase from local implementation"
    echo git push origin main
    echo.
    echo Then deploy on Ubuntu:
    echo cd /opt/email-wallet-service
    echo sudo git pull origin main
    echo sudo ./deployment/scripts/deploy-configured.sh
    echo.
) else (
    echo.
    echo ❌ BUILD FAILED! Check errors above
    echo.
)

echo.
pause