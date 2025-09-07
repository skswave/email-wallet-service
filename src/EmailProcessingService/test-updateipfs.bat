@echo off
echo === Building Updated Email Processing Service ===
dotnet build --configuration Debug

if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo === Build successful, testing new blockchain approach ===
echo.

echo Testing the updateIPFSHash approach instead of complex createEmailWallet...
echo This should resolve the tuple parsing issues.

echo.
echo Press any key to start the service for testing...
pause

dotnet run
