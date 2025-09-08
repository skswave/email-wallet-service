@echo off
echo 🔧 Testing Build After FileProcessorService Fix
echo ============================================

cd /d "C:\Users\StevenSprague\OneDrive - Rivetz Corp\Rootz\claud project\email-wallet-service-repo\src\EmailProcessingService"

echo.
echo 🔨 Building with fixed FileProcessorService...
dotnet build --configuration Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ BUILD SUCCESS! All compilation errors fixed
    echo.
    echo 🚀 Ready to push to GitHub and deploy!
    echo.
) else (
    echo.
    echo ❌ BUILD STILL FAILING - Check remaining errors
    echo.
)

echo.
pause