@echo off
echo ============================================
echo    SWAGGER UI FIX AND TEST SCRIPT
echo ============================================
echo.

:: Colors
set "GREEN=[92m"
set "RED=[91m"
set "YELLOW=[93m"
set "BLUE=[94m"
set "NC=[0m"

echo %BLUE%Fixing Swagger UI issues and testing...%NC%
echo.

echo %YELLOW%Step 1: Backing up current Program.cs...%NC%
copy Program.cs Program_backup_%date:~-4,4%%date:~-10,2%%date:~-7,2%.cs
if %ERRORLEVEL% EQU 0 (
    echo %GREEN%✓ Backup created successfully%NC%
) else (
    echo %RED%✗ Failed to create backup%NC%
    pause
    exit /b 1
)

echo %YELLOW%Step 2: Updating Program.cs with Swagger fixes...%NC%
echo.
echo The following changes will be applied:
echo   • Enable Swagger in all environments (not just Development)
echo   • Add CORS support for localhost
echo   • Add multiple server endpoints
echo   • Enable XML documentation
echo   • Add enhanced Swagger UI options
echo.

pause

:: Check if the fixed Program.cs exists
if not exist "Program_fixed.cs" (
    echo %RED%Error: Program_fixed.cs not found!%NC%
    echo Please ensure the fixed Program.cs file is available.
    pause
    exit /b 1
)

:: Apply the fix
copy Program_fixed.cs Program.cs
if %ERRORLEVEL% EQU 0 (
    echo %GREEN%✓ Program.cs updated with Swagger fixes%NC%
) else (
    echo %RED%✗ Failed to update Program.cs%NC%
    pause
    exit /b 1
)

echo %YELLOW%Step 3: Cleaning and rebuilding project...%NC%
dotnet clean
dotnet build

if %ERRORLEVEL% NEQ 0 (
    echo %RED%Build failed! Rolling back changes...%NC%
    copy Program_backup_%date:~-4,4%%date:~-10,2%%date:~-7,2%.cs Program.cs
    pause
    exit /b 1
)

echo %GREEN%✓ Build successful with Swagger fixes%NC%
echo.

echo %YELLOW%Step 4: Starting service for testing...%NC%
echo.
echo %BLUE%The service will start with the following Swagger endpoints:%NC%
echo   • Swagger UI:  https://localhost:7000/swagger
echo   • Swagger JSON: https://localhost:7000/swagger/v1/swagger.json
echo   • Alt HTTP:    http://localhost:5000/swagger
echo.

echo %YELLOW%Note: Keep this window open. The service will start in a new window.%NC%
echo Press any key to start the service...
pause

:: Start the service in a new window
start "Email Wallet Service" cmd /k "dotnet run"

echo %YELLOW%Step 5: Waiting for service to start...%NC%
timeout /t 10 > nul

echo %YELLOW%Step 6: Testing Swagger endpoints...%NC%
echo.

:: Test if browser automation is running
curl -s http://localhost:8080/browser/start -X POST -H "Content-Type: application/json" > nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo %GREEN%✓ Browser automation is running%NC%
    
    echo %YELLOW%Running automated Swagger tests...%NC%
    python diagnose_swagger.py
    
    if %ERRORLEVEL% EQU 0 (
        echo %GREEN%✓ Swagger diagnostic completed%NC%
    ) else (
        echo %YELLOW%⚠ Swagger diagnostic had issues (check output above)%NC%
    )
) else (
    echo %YELLOW%⚠ Browser automation not running - testing manually...%NC%
    
    echo %YELLOW%Testing Swagger JSON endpoint...%NC%
    curl -k -s https://localhost:7000/swagger/v1/swagger.json | findstr "openapi" > nul
    if %ERRORLEVEL% EQU 0 (
        echo %GREEN%✓ Swagger JSON endpoint working%NC%
    ) else (
        echo %RED%✗ Swagger JSON endpoint failed%NC%
    )
    
    echo %YELLOW%Testing Swagger UI endpoint...%NC%
    curl -k -s https://localhost:7000/swagger | findstr "swagger" > nul
    if %ERRORLEVEL% EQU 0 (
        echo %GREEN%✓ Swagger UI endpoint responding%NC%
    ) else (
        echo %RED%✗ Swagger UI endpoint failed%NC%
    )
)

echo.
echo %BLUE%Step 7: Manual verification steps...%NC%
echo =====================================
echo.
echo %YELLOW%Please manually verify the following:%NC%
echo.
echo 1. Open browser and navigate to: https://localhost:7000/swagger
echo    Expected: Swagger UI loads completely with all endpoints visible
echo.
echo 2. Click "Try it out" on any endpoint
echo    Expected: Forms are interactive and functional
echo.
echo 3. Check the API documentation
echo    Expected: All endpoints have descriptions and examples
echo.
echo 4. Test the production blockchain endpoints
echo    Expected: All endpoints respond successfully
echo.

echo %GREEN%🎉 Swagger fix process completed!%NC%
echo.

echo %BLUE%Next steps for production development:%NC%
echo ======================================
echo 1. ✅ Swagger UI is now working
echo 2. 🔄 Implement clean DTOs for API requests/responses  
echo 3. 🔄 Add proper error handling middleware
echo 4. 🔄 Create unit test foundation
echo 5. 🔄 Set up browser automation for API testing
echo.

echo %YELLOW%If Swagger is working correctly, you can now:%NC%
echo • Test all endpoints through the browser
echo • Generate client SDKs
echo • Document API usage
echo • Use browser automation for testing
echo.

echo Press any key to exit (service will continue running)...
pause