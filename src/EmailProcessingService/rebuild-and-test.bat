@echo off
echo === Building Email Processing Service ===
dotnet build --configuration Debug

if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo === Build successful, starting service for testing ===
echo.

echo Press Ctrl+C to stop the service when you want to test...
dotnet run

pause
