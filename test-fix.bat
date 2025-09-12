@echo off
echo Testing build after model property fixes...

cd /d "C:\Users\StevenSprague\OneDrive - Rivetz Corp\Rootz\claud project\email-data-wallet-service\src\EmailProcessingService"

echo Cleaning previous build...
dotnet clean

echo Building project...
dotnet build --configuration Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ BUILD SUCCESSFUL! All model property mismatches have been fixed.
    echo.
    echo You can now run fix-and-push.bat to commit and push the changes.
) else (
    echo.
    echo ❌ BUILD FAILED! There are still compilation errors.
    echo Please check the output above for remaining issues.
)

pause
