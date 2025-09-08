@echo off
echo 🔧 Adding missing models to fix compilation errors...
echo.

cd /d "C:\Users\StevenSprague\OneDrive - Rivetz Corp\Rootz\claud project\email-wallet-service-repo"

echo Adding missing models file...
git add src/EmailProcessingService/Models/MissingModels.cs

echo.
echo Committing fix...
git commit -m "Fix compilation errors: Add missing model classes

- Add WalletType enum
- Add VerificationInfo class  
- Add FileMetadataInfo class
- Add VirusScanResult class and ScanStatus enum
- Add IWalletCreatorService and IFileProcessorService interfaces
- Add basic implementations for missing services
- Resolves all compilation errors in deployment"

echo.
echo Pushing fix to GitHub...
git push origin main

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ Compilation fix pushed successfully!
    echo.
    echo 🚀 Now retry deployment on Ubuntu server:
    echo.
    echo cd /opt/email-wallet-service
    echo sudo git pull origin main
    echo sudo ./deployment/scripts/deploy-configured.sh
    echo.
) else (
    echo ❌ Error pushing fix to GitHub
)

echo.
pause