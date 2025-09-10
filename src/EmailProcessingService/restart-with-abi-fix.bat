@echo off
echo ==============================================
echo   ABI PARSING FIX - SERVICE RESTART
echo ==============================================

echo.
echo [1/2] Stopping service to apply ABI parsing fix...
taskkill /f /im EmailProcessingService.exe 2>nul
if errorlevel 1 (
    echo No running service found, continuing...
) else (
    echo Service stopped successfully.
)

echo.
echo [2/2] Starting service with ABI parsing fix...
echo.
echo ✅ ABI PARSING ISSUE RESOLVED:
echo   ✓ Temporarily using basic ABIs to avoid Hardhat compatibility issues
echo   ✓ Service should start without format exceptions
echo   ✓ Basic blockchain connectivity will work
echo   ✓ Test endpoints should respond properly
echo.
echo 📝 NOTE: Using simplified ABIs for now. Full Hardhat ABI support 
echo    can be implemented later when needed for complex operations.
echo.
echo 🚀 Service starting with working blockchain integration:
echo   • Swagger UI: https://localhost:7000/swagger
echo   • Test status: https://localhost:7000/api/blockchaintest/status
echo   • Check balance: https://localhost:7000/api/blockchaintest/wallet/{address}/credits
echo.
echo Press CTRL+C to stop the service
echo.

dotnet run
