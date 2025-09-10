@echo off
echo === Clean Production Service Test ===
echo.

echo Step 1: Cleaning and rebuilding...
dotnet clean
dotnet build

if %ERRORLEVEL% NEQ 0 (
    echo Build failed! Check for errors above.
    pause
    exit /b 1
)

echo.
echo Step 2: Starting production service...
echo.
echo ================================
echo PRODUCTION SERVICE ENDPOINTS:
echo ================================
echo.
echo 1. Production Blockchain Status:
echo    GET https://localhost:7000/api/productionblockchain/status
echo.
echo 2. Blockchain Connection Test:
echo    GET https://localhost:7000/api/productionblockchain/test-connection
echo.
echo 3. Wallet Registration Check:
echo    GET https://localhost:7000/api/productionblockchain/wallet/0x107C5655ce50AB9744Fc36A4e9935E30d4923d0b/registered
echo.
echo 4. Test Blockchain Transaction:
echo    POST https://localhost:7000/api/productionblockchain/test-transaction
echo    Body: {"taskId": "prod-test-001", "ipfsHash": "QmTestHash123"}
echo.
echo 5. Full Email Processing Test:
echo    POST https://localhost:7000/api/email/process
echo    Body: {"messageId": "production-test@techcorp.com", "subject": "Production Test", "from": "demo@techcorp.com", "to": "recipient@techcorp.com", "body": "Testing production blockchain service", "receivedDate": "2025-09-06T20:00:00Z", "attachments": []}
echo.
echo ================================
echo.

pause

dotnet run
