@echo off
cd /d "C:\Users\StevenSprague\OneDrive - Rivetz Corp\Rootz\claud project\email-wallet-service-repo"

echo ✅ CLEAN FIX: Pushing final compilation fix...
git add .
git commit -m "CLEAN FIX: Resolve all compilation errors

- Fix WalletType, VerificationInfo, FileMetadataInfo, VirusScanResult definitions
- Add correct IFileProcessorService interface matching FileProcessorService.cs
- Add correct IWalletCreatorService interface with proper types
- Remove duplicate implementations and circular references
- Use existing model types (IncomingEmailMessage, EmailAttachment)
- Clean, minimal fix with no conflicts"

git push origin main

echo.
echo 🚀 Clean fix deployed! Now run on Ubuntu:
echo   cd /opt/email-wallet-service  
echo   sudo git pull origin main
echo   cd src/EmailProcessingService
echo   dotnet build --configuration Release
echo.
pause
