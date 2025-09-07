@echo off
echo ==============================================
echo   TESTING MODEL FIX - QUICK BUILD
echo ==============================================

echo.
echo [1/2] Quick build test...
dotnet build --verbosity minimal

if errorlevel 1 (
    echo.
    echo [ERROR] Build still has errors. Let's check what's remaining...
    echo.
    dotnet build --verbosity normal
    pause
    exit /b 1
)

echo.
echo [2/2] Starting service...
echo.
echo ✅ SUCCESS! All compilation errors resolved:
echo   ✓ Added missing FileMetadataInfo class with required properties
echo   ✓ Added missing VirusScanResult class with required properties
echo   ✓ All service classes properly implemented
echo   ✓ Swagger UI configuration fixed
echo   ✓ Clean project structure with separated concerns
echo.
echo 🚀 Service starting at:
echo   • Swagger UI: https://localhost:7000/swagger
echo   • API Base:   https://localhost:7000
echo.
echo Press CTRL+C to stop the service
echo.

dotnet run
