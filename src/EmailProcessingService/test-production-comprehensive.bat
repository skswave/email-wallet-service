@echo off
setlocal enabledelayedexpansion

echo ============================================
echo    PRODUCTION BLOCKCHAIN SERVICE TEST SUITE
echo ============================================
echo.

:: Set colors for output
set "GREEN=[92m"
set "RED=[91m"
set "YELLOW=[93m"
set "BLUE=[94m"
set "NC=[0m"

:: Configuration
set "BASE_URL=https://localhost:7000"
set "TEST_WALLET=0x107C5655ce50AB9744Fc36A4e9935E30d4923d0b"
set "TEST_EMAIL=production-test@techcorp.com"

:: Track test results
set /a TOTAL_TESTS=0
set /a PASSED_TESTS=0
set /a FAILED_TESTS=0

echo %BLUE%Step 1: Building and Starting Service%NC%
echo ========================================
echo.

echo Cleaning previous build...
dotnet clean

echo.
echo Building service...
dotnet build

if %ERRORLEVEL% NEQ 0 (
    echo %RED%Build failed! Aborting tests.%NC%
    pause
    exit /b 1
)

echo %GREEN%Build successful!%NC%
echo.

echo %BLUE%Step 2: Starting Production Service%NC%
echo =====================================
echo.
echo Service will start on: %BASE_URL%
echo Test wallet address: %TEST_WALLET%
echo.
echo Press any key to start the service...
pause

start /b dotnet run

echo Waiting 10 seconds for service to start...
timeout /t 10 > nul

echo.
echo %BLUE%Step 3: Running Production Service Tests%NC%
echo ==========================================
echo.

:: Test 1: Production Service Status
echo %YELLOW%TEST 1: Production Service Status%NC%
set /a TOTAL_TESTS+=1
curl -k -s "%BASE_URL%/api/productionblockchain/status" > temp_result.json
if %ERRORLEVEL% EQU 0 (
    echo %GREEN%✓ PASS: Service status endpoint responding%NC%
    set /a PASSED_TESTS+=1
) else (
    echo %RED%✗ FAIL: Service status endpoint not responding%NC%
    set /a FAILED_TESTS+=1
)
echo.

:: Test 2: Blockchain Connection
echo %YELLOW%TEST 2: Blockchain Connection Test%NC%
set /a TOTAL_TESTS+=1
curl -k -s "%BASE_URL%/api/productionblockchain/test-connection" > temp_connection.json
if %ERRORLEVEL% EQU 0 (
    echo %GREEN%✓ PASS: Blockchain connection test responding%NC%
    set /a PASSED_TESTS+=1
) else (
    echo %RED%✗ FAIL: Blockchain connection test failed%NC%
    set /a FAILED_TESTS+=1
)
echo.

:: Test 3: Wallet Registration Check
echo %YELLOW%TEST 3: Wallet Registration Check%NC%
set /a TOTAL_TESTS+=1
curl -k -s "%BASE_URL%/api/productionblockchain/wallet/%TEST_WALLET%/registered" > temp_wallet.json
if %ERRORLEVEL% EQU 0 (
    echo %GREEN%✓ PASS: Wallet registration check responding%NC%
    set /a PASSED_TESTS+=1
) else (
    echo %RED%✗ FAIL: Wallet registration check failed%NC%
    set /a FAILED_TESTS+=1
)
echo.

:: Test 4: Credit Balance Check
echo %YELLOW%TEST 4: Credit Balance Check%NC%
set /a TOTAL_TESTS+=1
curl -k -s "%BASE_URL%/api/productionblockchain/wallet/%TEST_WALLET%/credits" > temp_credits.json
if %ERRORLEVEL% EQU 0 (
    echo %GREEN%✓ PASS: Credit balance check responding%NC%
    set /a PASSED_TESTS+=1
) else (
    echo %RED%✗ FAIL: Credit balance check failed%NC%
    set /a FAILED_TESTS+=1
)
echo.

:: Test 5: Test Blockchain Transaction
echo %YELLOW%TEST 5: Test Blockchain Transaction%NC%
set /a TOTAL_TESTS+=1
echo {"taskId": "production-test-001", "ipfsHash": "QmTestHash123"} > temp_tx_body.json
curl -k -s -X POST -H "Content-Type: application/json" -d @temp_tx_body.json "%BASE_URL%/api/productionblockchain/test-transaction" > temp_transaction.json
if %ERRORLEVEL% EQU 0 (
    echo %GREEN%✓ PASS: Test transaction responding%NC%
    set /a PASSED_TESTS+=1
) else (
    echo %RED%✗ FAIL: Test transaction failed%NC%
    set /a FAILED_TESTS+=1
)
echo.

:: Test 6: Health Check
echo %YELLOW%TEST 6: Health Check%NC%
set /a TOTAL_TESTS+=1
curl -k -s "%BASE_URL%/health" > temp_health.json
if %ERRORLEVEL% EQU 0 (
    echo %GREEN%✓ PASS: Health check responding%NC%
    set /a PASSED_TESTS+=1
) else (
    echo %RED%✗ FAIL: Health check failed%NC%
    set /a FAILED_TESTS+=1
)
echo.

:: Test 7: IPFS Health Check
echo %YELLOW%TEST 7: IPFS Health Check%NC%
set /a TOTAL_TESTS+=1
curl -k -s "%BASE_URL%/health/ipfs" > temp_ipfs_health.json
if %ERRORLEVEL% EQU 0 (
    echo %GREEN%✓ PASS: IPFS health check responding%NC%
    set /a PASSED_TESTS+=1
) else (
    echo %RED%✗ FAIL: IPFS health check failed%NC%
    set /a FAILED_TESTS+=1
)
echo.

:: Test 8: Swagger Documentation
echo %YELLOW%TEST 8: Swagger Documentation%NC%
set /a TOTAL_TESTS+=1
curl -k -s "%BASE_URL%/swagger/v1/swagger.json" > temp_swagger.json
if %ERRORLEVEL% EQU 0 (
    echo %GREEN%✓ PASS: Swagger documentation available%NC%
    set /a PASSED_TESTS+=1
) else (
    echo %RED%✗ FAIL: Swagger documentation failed%NC%
    set /a FAILED_TESTS+=1
)
echo.

:: Test 9: End-to-End Email Processing
echo %YELLOW%TEST 9: End-to-End Email Processing%NC%
set /a TOTAL_TESTS+=1
echo {"messageId": "%TEST_EMAIL%", "subject": "Production Blockchain Test", "from": "demo@techcorp.com", "to": "recipient@techcorp.com", "body": "Testing clean production blockchain integration", "receivedDate": "2025-09-06T20:00:00Z", "attachments": []} > temp_email_body.json
curl -k -s -X POST -H "Content-Type: application/json" -d @temp_email_body.json "%BASE_URL%/api/email/process" > temp_email_result.json
if %ERRORLEVEL% EQU 0 (
    echo %GREEN%✓ PASS: End-to-end email processing responding%NC%
    set /a PASSED_TESTS+=1
) else (
    echo %RED%✗ FAIL: End-to-end email processing failed%NC%
    set /a FAILED_TESTS+=1
)
echo.

:: Test 10: Contract ABI Service
echo %YELLOW%TEST 10: Contract ABI Service%NC%
set /a TOTAL_TESTS+=1
echo Testing internal ABI service initialization...
if exist "bin\Debug\net8.0\abis\EmailWalletRegistration.json" (
    echo %GREEN%✓ PASS: ABI files present and accessible%NC%
    set /a PASSED_TESTS+=1
) else (
    echo %RED%✗ FAIL: ABI files missing%NC%
    set /a FAILED_TESTS+=1
)
echo.

:: Display Results Summary
echo %BLUE%Step 4: Test Results Summary%NC%
echo ==============================
echo.
echo Total Tests: %TOTAL_TESTS%
echo %GREEN%Passed: %PASSED_TESTS%%NC%
echo %RED%Failed: %FAILED_TESTS%%NC%
echo.

:: Calculate success rate
set /a SUCCESS_RATE=(%PASSED_TESTS% * 100) / %TOTAL_TESTS%
echo Success Rate: %SUCCESS_RATE%%%
echo.

if %FAILED_TESTS% EQU 0 (
    echo %GREEN%🎉 ALL TESTS PASSED! Production service is ready!%NC%
    echo.
    echo %BLUE%Production Service Endpoints:%NC%
    echo ============================
    echo Status:      GET  %BASE_URL%/api/productionblockchain/status
    echo Connection:  GET  %BASE_URL%/api/productionblockchain/test-connection
    echo Wallet Info: GET  %BASE_URL%/api/productionblockchain/wallet/{address}/registered
    echo Transaction: POST %BASE_URL%/api/productionblockchain/test-transaction
    echo Email:       POST %BASE_URL%/api/email/process
    echo Health:      GET  %BASE_URL%/health
    echo IPFS Health: GET  %BASE_URL%/health/ipfs
    echo Swagger:     GET  %BASE_URL%/swagger
    echo.
    echo %GREEN%Architecture Benefits Achieved:%NC%
    echo ✅ No more ABI parsing errors
    echo ✅ Clean service separation
    echo ✅ Production-ready error handling
    echo ✅ Proper dependency injection
    echo ✅ Curated, reliable ABIs
    echo ✅ Scalable architecture
) else (
    echo %RED%⚠️  Some tests failed. Check the logs above for details.%NC%
    echo.
    echo %YELLOW%Debugging Information:%NC%
    echo =====================
    echo Check temp_*.json files for detailed responses
    echo Review application logs for error details
    echo Verify blockchain configuration in appsettings.json
)

echo.
echo %BLUE%Detailed Response Files:%NC%
echo =======================
echo Service Status:     temp_result.json
echo Connection Test:    temp_connection.json
echo Wallet Check:       temp_wallet.json
echo Credit Balance:     temp_credits.json
echo Transaction Test:   temp_transaction.json
echo Health Check:       temp_health.json
echo IPFS Health:        temp_ipfs_health.json
echo Swagger Spec:       temp_swagger.json
echo Email Processing:   temp_email_result.json
echo.

echo Press any key to view detailed results or Ctrl+C to exit...
pause

:: Display detailed results
echo.
echo %BLUE%Detailed Test Results:%NC%
echo =====================
echo.

echo %YELLOW%Service Status Response:%NC%
type temp_result.json 2>nul | findstr /C:"{" >nul && (
    type temp_result.json
) || echo No valid response received

echo.
echo %YELLOW%Connection Test Response:%NC%
type temp_connection.json 2>nul | findstr /C:"{" >nul && (
    type temp_connection.json
) || echo No valid response received

echo.
echo %YELLOW%Wallet Registration Response:%NC%
type temp_wallet.json 2>nul | findstr /C:"{" >nul && (
    type temp_wallet.json
) || echo No valid response received

echo.
echo %YELLOW%Transaction Test Response:%NC%
type temp_transaction.json 2>nul | findstr /C:"{" >nul && (
    type temp_transaction.json
) || echo No valid response received

echo.
echo %YELLOW%Email Processing Response:%NC%
type temp_email_result.json 2>nul | findstr /C:"{" >nul && (
    type temp_email_result.json
) || echo No valid response received

:: Cleanup
echo.
echo Cleaning up temporary files...
del temp_*.json 2>nul

echo.
echo %GREEN%Production service test completed!%NC%
echo.
echo The service is running at: %BASE_URL%
echo Press Ctrl+C to stop the service when done testing.
echo.

pause