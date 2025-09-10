@echo off
echo Testing production build...

echo.
echo Checking for build issues...
dotnet build --verbosity detailed > build-log.txt 2>&1

echo.
echo Build log saved to build-log.txt
echo.

echo Checking if build succeeded...
findstr /C:"Build succeeded" build-log.txt > nul
if %ERRORLEVEL% EQU 0 (
    echo Build SUCCEEDED!
    echo Starting production service...
    dotnet run
) else (
    echo Build FAILED. Checking errors...
    findstr /C:"error" build-log.txt
    echo.
    echo Full build log:
    type build-log.txt
)

pause
