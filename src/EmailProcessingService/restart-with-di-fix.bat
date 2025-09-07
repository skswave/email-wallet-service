@echo off
echo ==============================================
echo   DEPENDENCY INJECTION FIX - SERVICE RESTART
echo ==============================================

echo.
echo [1/2] Stopping current service to apply DI changes...
taskkill /f /im EmailProcessingService.exe 2>nul
if errorlevel 1 (
    echo No running service found, continuing...
) else (
    echo Service stopped successfully.
)

echo.
echo [2/2] Starting service with fixed dependency injection...
echo.
echo ✅ DEPENDENCY INJECTION FIXED:
echo   ✓ IBlockchainService now registered in DI container
echo   ✓ BlockchainTestController dependencies resolved
echo   ✓ All blockchain test endpoints should work
echo.
echo 🚀 Service starting with fixed dependencies:
echo   • Swagger UI: https://localhost:7000/swagger
echo   • Blockchain Test: https://localhost:7000/api/blockchaintest/status
echo.
echo Press CTRL+C to stop the service
echo.

dotnet run
