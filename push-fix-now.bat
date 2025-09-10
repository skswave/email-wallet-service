@echo off
echo 🔧 Pushing compilation fix to GitHub...
echo.

cd /d "C:\Users\StevenSprague\OneDrive - Rivetz Corp\Rootz\claud project\email-wallet-service-repo"

git add src/EmailProcessingService/Models/MissingModels.cs
git commit -m "Fix compilation errors: Add missing model classes

- Add WalletType enum  
- Add VerificationInfo, FileMetadataInfo, VirusScanResult classes
- Add IWalletCreatorService and IFileProcessorService interfaces
- Add complete mock implementations
- Fix all 9 compilation errors for Ubuntu deployment
- Services already registered in Program.cs DI container"

git push origin main

echo.
echo ✅ Fix pushed! Now run on Ubuntu:
echo   cd /opt/email-wallet-service
echo   sudo git pull origin main  
echo   cd src/EmailProcessingService
echo   dotnet build --configuration Release
echo.
pause