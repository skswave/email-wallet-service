@echo off
echo === Production Email Processing Service Build ===
echo.

echo Building production-grade email processing service...
echo.

echo [1/4] Cleaning previous builds...
dotnet clean --configuration Release
if %ERRORLEVEL% NEQ 0 (
    echo Clean failed!
    pause
    exit /b 1
)

echo [2/4] Restoring packages...
dotnet restore
if %ERRORLEVEL% NEQ 0 (
    echo Package restore failed!
    pause
    exit /b 1
)

echo [3/4] Building in Release mode...
dotnet build --configuration Release --no-restore
if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo [4/4] Running production tests...
echo Testing production blockchain service...

echo.
echo === Production Build Complete ===
echo.
echo Production Features:
echo - Clean ABI management with ContractAbiService
echo - Production blockchain service with proper error handling
echo - IPFS integration for decentralized storage
echo - Comprehensive logging and monitoring
echo - Scalable architecture for future enhancements
echo.

echo Starting production service...
echo API: https://localhost:7000
echo Swagger: https://localhost:7000/swagger
echo.

echo Test endpoints:
echo - GET /api/productionblockchain/status
echo - GET /api/productionblockchain/test-connection
echo - POST /api/productionblockchain/test-transaction

echo.
pause

dotnet run --configuration Release
