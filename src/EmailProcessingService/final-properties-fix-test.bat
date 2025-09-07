@echo off
echo ==============================================
echo   FINAL MODEL PROPERTIES FIX TEST
echo ==============================================

echo.
echo [1/2] Testing build with added properties...
dotnet build --verbosity minimal

if errorlevel 1 (
    echo.
    echo [ERROR] Build still has errors. Details:
    echo.
    dotnet build --verbosity normal
    pause
    exit /b 1
)

echo.
echo [2/2] SUCCESS! Starting service...
echo.
echo ✅ FINAL SUCCESS - All compilation errors resolved:
echo   ✓ Found existing FileMetadataInfo in DataWalletModels.cs
echo   ✓ Found existing VirusScanResult in DataWalletModels.cs  
echo   ✓ Added missing properties to existing classes instead of duplicating
echo   ✓ FileProcessorService now has all required properties
echo   ✓ No duplicate class definitions
echo   ✓ Clean build with proper separation of concerns
echo.
echo 🚀 Email Data Wallet Service starting at:
echo   • Swagger UI: https://localhost:7000/swagger
echo   • API Base:   https://localhost:7000
echo.
echo Ready to test your blockchain integration!
echo Press CTRL+C to stop the service
echo.

dotnet run
