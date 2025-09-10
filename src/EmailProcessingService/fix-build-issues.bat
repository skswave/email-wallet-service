@echo off
echo ============================================
echo    BUILD FIX AND CLEAN REBUILD
echo ============================================
echo.

:: Colors
set "GREEN=[92m"
set "RED=[91m"
set "YELLOW=[93m"
set "BLUE=[94m"
set "NC=[0m"

echo %YELLOW%Step 1: Stopping any running processes...%NC%
taskkill /f /im dotnet.exe 2>nul
timeout /t 2 > nul

echo %YELLOW%Step 2: Cleaning locked files and build artifacts...%NC%

:: Remove bin and obj directories completely
if exist "bin\" (
    echo Removing bin directory...
    rmdir /s /q "bin" 2>nul
)

if exist "obj\" (
    echo Removing obj directory...
    rmdir /s /q "obj" 2>nul
)

:: Clean NuGet cache for this project
echo Cleaning NuGet cache...
dotnet nuget locals all --clear

:: Remove any temp files
del temp_*.json 2>nul

echo %YELLOW%Step 3: Restoring packages...%NC%
dotnet restore --force --no-cache

if %ERRORLEVEL% NEQ 0 (
    echo %RED%Package restore failed!%NC%
    pause
    exit /b 1
)

echo %YELLOW%Step 4: Clean build...%NC%
dotnet clean

echo %YELLOW%Step 5: Building project...%NC%
dotnet build --no-restore

if %ERRORLEVEL% NEQ 0 (
    echo %RED%Build still failing. Let's check for common issues...%NC%
    echo.
    echo %YELLOW%Troubleshooting Steps:%NC%
    echo 1. Close Visual Studio if open
    echo 2. Close any other command prompts
    echo 3. Restart Windows if file locks persist
    echo 4. Check if antivirus is blocking files
    echo.
    pause
    exit /b 1
)

echo %GREEN%✓ Build successful!%NC%
echo.

echo %YELLOW%Step 6: Verifying ABI files were created...%NC%
if exist "bin\Debug\net8.0\abis\" (
    echo %GREEN%✓ ABI directory created%NC%
    dir "bin\Debug\net8.0\abis\*.json" /b 2>nul && (
        echo %GREEN%✓ ABI files present:%NC%
        for %%f in ("bin\Debug\net8.0\abis\*.json") do echo   - %%~nxf
    ) || (
        echo %YELLOW%ⓘ ABI files will be created on first run%NC%
    )
) else (
    echo %YELLOW%ⓘ ABI directory will be created on first run%NC%
)

echo.
echo %GREEN%🎉 Build fix completed successfully!%NC%
echo.
echo %BLUE%Next Steps:%NC%
echo 1. Run: test-production-comprehensive.bat
echo 2. Or start service manually: dotnet run
echo.

pause