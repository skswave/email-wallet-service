@echo off
echo ============================================
echo    PRE-TEST VERIFICATION SCRIPT
echo ============================================
echo.

:: Colors
set "GREEN=[92m"
set "RED=[91m"
set "YELLOW=[93m"
set "BLUE=[94m"
set "NC=[0m"

echo %BLUE%Checking Production Service Components...%NC%
echo.

:: Check 1: ProductionBlockchainService exists
echo %YELLOW%1. Checking ProductionBlockchainService...%NC%
if exist "Services\ProductionBlockchainService.cs" (
    echo %GREEN%✓ ProductionBlockchainService.cs found%NC%
) else (
    echo %RED%✗ ProductionBlockchainService.cs missing%NC%
)

:: Check 2: ProductionBlockchainController exists
echo %YELLOW%2. Checking ProductionBlockchainController...%NC%
if exist "Controllers\ProductionBlockchainController.cs" (
    echo %GREEN%✓ ProductionBlockchainController.cs found%NC%
) else (
    echo %RED%✗ ProductionBlockchainController.cs missing%NC%
)

:: Check 3: ContractAbiService exists
echo %YELLOW%3. Checking ContractAbiService...%NC%
if exist "Contracts\ContractAbiService.cs" (
    echo %GREEN%✓ ContractAbiService.cs found%NC%
) else (
    echo %RED%✗ ContractAbiService.cs missing%NC%
)

:: Check 4: ABI files directory
echo %YELLOW%4. Checking ABI files...%NC%
if exist "bin\Debug\net8.0\abis\" (
    echo %GREEN%✓ ABI directory exists%NC%
    dir /b "bin\Debug\net8.0\abis\" 2>nul | findstr /C:".json" >nul && (
        echo %GREEN%✓ ABI JSON files found%NC%
        echo   Files:
        for %%f in ("bin\Debug\net8.0\abis\*.json") do echo     - %%~nxf
    ) || (
        echo %YELLOW%⚠ ABI directory exists but no JSON files found%NC%
        echo   This is normal if service hasn't been built yet
    )
) else (
    echo %YELLOW%⚠ ABI directory not found (created during build)%NC%
)

:: Check 5: Program.cs service registration
echo %YELLOW%5. Checking service registration in Program.cs...%NC%
findstr /C:"IProductionBlockchainService" "Program.cs" >nul && (
    echo %GREEN%✓ ProductionBlockchainService registered in DI%NC%
) || (
    echo %RED%✗ ProductionBlockchainService not registered in Program.cs%NC%
)

:: Check 6: Old BlockchainService commented out
echo %YELLOW%6. Checking legacy service is disabled...%NC%
findstr /C:"// builder.Services.AddScoped<IBlockchainService, BlockchainService>" "Program.cs" >nul && (
    echo %GREEN%✓ Legacy BlockchainService properly disabled%NC%
) || (
    echo %YELLOW%⚠ Legacy service registration status unclear%NC%
)

:: Check 7: Configuration files
echo %YELLOW%7. Checking configuration...%NC%
if exist "appsettings.json" (
    echo %GREEN%✓ appsettings.json found%NC%
    findstr /C:"Blockchain" "appsettings.json" >nul && (
        echo %GREEN%✓ Blockchain configuration section exists%NC%
    ) || (
        echo %RED%✗ Blockchain configuration missing%NC%
    )
) else (
    echo %RED%✗ appsettings.json missing%NC%
)

:: Check 8: Test wallet configuration
echo %YELLOW%8. Checking test wallet configuration...%NC%
findstr /C:"0x107C5655ce50AB9744Fc36A4e9935E30d4923d0b" "appsettings.json" >nul && (
    echo %GREEN%✓ Test wallet address configured%NC%
) || (
    echo %YELLOW%⚠ Test wallet address not found in config%NC%
)

:: Check 9: IPFS integration
echo %YELLOW%9. Checking IPFS integration...%NC%
if exist "Services\IpfsService.cs" (
    echo %GREEN%✓ IpfsService.cs found%NC%
) else (
    echo %RED%✗ IpfsService.cs missing%NC%
)

:: Check 10: Build status
echo %YELLOW%10. Checking project can build...%NC%
echo Building project (quick check)...
dotnet build --verbosity quiet --nologo >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo %GREEN%✓ Project builds successfully%NC%
) else (
    echo %RED%✗ Project has build errors%NC%
    echo Running detailed build to show errors:
    dotnet build
)

echo.
echo %BLUE%Pre-Test Verification Complete%NC%
echo ================================
echo.

:: Summary
set /a checks_passed=0
if exist "Services\ProductionBlockchainService.cs" set /a checks_passed+=1
if exist "Controllers\ProductionBlockchainController.cs" set /a checks_passed+=1
if exist "Contracts\ContractAbiService.cs" set /a checks_passed+=1
if exist "appsettings.json" set /a checks_passed+=1

if %checks_passed% GEQ 4 (
    echo %GREEN%✅ Core components verified! Ready to run production tests.%NC%
    echo.
    echo %BLUE%Next Steps:%NC%
    echo 1. Run test-production-comprehensive.bat for full test suite
    echo 2. Or run test-production-clean.bat for original test script
    echo 3. Check appsettings.json for correct RPC URL and contract addresses
    echo.
) else (
    echo %RED%❌ Some components missing. Please check the errors above.%NC%
    echo.
    echo %YELLOW%Troubleshooting:%NC%
    echo - Ensure you're in the EmailProcessingService directory
    echo - Run the blockchain fix implementation script if files are missing
    echo - Check that all file paths are correct
    echo.
)

echo Press any key to continue...
pause