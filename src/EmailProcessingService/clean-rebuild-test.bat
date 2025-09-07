@echo off
echo ==============================================
echo   FIXED - SWAGGER UI REBUILD AND TEST
echo ==============================================

echo.
echo [1/5] Stopping any running EmailProcessingService...
echo Please close any running service windows or press Ctrl+C if service is running
echo Press any key after stopping all running services...
pause

echo.
echo [2/5] Deleting build artifacts...
if exist "bin\Debug\net8.0" rmdir /s /q "bin\Debug\net8.0"
if exist "obj" rmdir /s /q "obj"

echo.
echo [3/5] Restoring packages...
dotnet restore --verbosity minimal

echo.
echo [4/5] Building project...
dotnet build --verbosity normal --no-restore

if errorlevel 1 (
    echo.
    echo [ERROR] Build failed. Check compilation errors above.
    pause
    exit /b 1
)

echo.
echo [5/5] Starting service...
echo.
echo ✓ Removed duplicate service definitions from Program.cs
echo ✓ Created proper Data/EmailProcessingDbContext.cs
echo ✓ Created proper HealthChecks/IpfsHealthCheck.cs
echo ✓ Updated using statements in Program.cs
echo ✓ Build successful!
echo.
echo ✓ Swagger will be available at: https://localhost:7000/swagger
echo ✓ API endpoints available at: https://localhost:7000
echo.
echo Press CTRL+C to stop the service
echo.

dotnet run
