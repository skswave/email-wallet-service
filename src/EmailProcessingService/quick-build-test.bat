@echo off
echo ============================================
echo    QUICK BUILD TEST
echo ============================================
echo.

:: Colors
set "GREEN=[92m"
set "RED=[91m"
set "YELLOW=[93m"
set "BLUE=[94m"
set "NC=[0m"

echo %BLUE%Testing clean build after fixing testing package issues...%NC%
echo.

echo %YELLOW%1. Killing any running dotnet processes...%NC%
taskkill /f /im dotnet.exe 2>nul
timeout /t 2 > nul

echo %YELLOW%2. Quick clean...%NC%
if exist "obj\" rmdir /s /q "obj" 2>nul
if exist "bin\" rmdir /s /q "bin" 2>nul

echo %YELLOW%3. Restore packages...%NC%
dotnet restore --nologo

if %ERRORLEVEL% NEQ 0 (
    echo %RED%✗ Package restore failed%NC%
    exit /b 1
)

echo %YELLOW%4. Build project...%NC%
dotnet build --nologo --verbosity minimal

if %ERRORLEVEL% EQU 0 (
    echo.
    echo %GREEN%✅ BUILD SUCCESSFUL!%NC%
    echo %GREEN%✓ Testing packages conflict resolved%NC%
    echo %GREEN%✓ Project compiles without errors%NC%
    echo.
    echo %BLUE%Ready to run production tests:%NC%
    echo   test-production-comprehensive.bat
    echo.
) else (
    echo.
    echo %RED%❌ BUILD FAILED%NC%
    echo %YELLOW%Try running: fix-build-issues.bat%NC%
    echo.
)

pause