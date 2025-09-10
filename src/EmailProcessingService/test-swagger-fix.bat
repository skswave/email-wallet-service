@echo off
echo ==============================================
echo   SWAGGER UI FIX - REBUILD AND TEST
echo ==============================================

echo.
echo [1/4] Cleaning previous build...
dotnet clean --verbosity minimal

echo.
echo [2/4] Restoring packages...
dotnet restore --verbosity minimal

echo.
echo [3/4] Building project...
dotnet build --verbosity minimal --no-restore

if errorlevel 1 (
    echo.
    echo [ERROR] Build failed. Check compilation errors above.
    pause
    exit /b 1
)

echo.
echo [4/4] Starting service...
echo.
echo ✓ Build successful!
echo ✓ Swagger will be available at: https://localhost:7000/swagger
echo ✓ API endpoints available at: https://localhost:7000
echo.
echo Press CTRL+C to stop the service
echo.

dotnet run
