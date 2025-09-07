@echo off
echo ==============================================
echo   FINAL FIX - SWAGGER UI REBUILD AND TEST
echo ==============================================

echo.
echo [INFO] The file access warnings are normal - they occur when the service is running.
echo [INFO] You can safely ignore the "Access is denied" messages during cleanup.
echo.

echo [1/4] Stopping any running services...
echo Please stop any running EmailProcessingService instances (Ctrl+C in terminals)
echo Press any key when all services are stopped...
pause

echo.
echo [2/4] Force cleaning build artifacts...
if exist "bin" rmdir /s /q "bin" 2>nul
if exist "obj" rmdir /s /q "obj" 2>nul
echo Build artifacts cleared.

echo.
echo [3/4] Building project with verbose output...
dotnet build --verbosity normal

if errorlevel 1 (
    echo.
    echo [ERROR] Build failed. Please check the errors above.
    echo [INFO] Common issues and fixes:
    echo   - File access errors: Stop all running services first
    echo   - Missing services: Check that new service files were created
    echo   - Swagger errors: Check Program.cs syntax
    pause
    exit /b 1
)

echo.
echo [4/4] Starting service...
echo.
echo ✅ FIXED ISSUES:
echo   ✓ Created missing UserRegistrationService.cs
echo   ✓ Created missing WhitelistService.cs  
echo   ✓ Created missing FileProcessorService.cs
echo   ✓ Fixed Swagger UI method call syntax
echo   ✓ Removed duplicate class definitions
echo   ✓ Created proper Data/EmailProcessingDbContext.cs
echo   ✓ Created proper HealthChecks/IpfsHealthCheck.cs
echo.
echo 🚀 Service starting at:
echo   • Swagger UI: https://localhost:7000/swagger
echo   • API Base:   https://localhost:7000
echo.
echo Press CTRL+C to stop the service
echo.

dotnet run
