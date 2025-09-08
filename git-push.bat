@echo off
cd /d "C:\Users\StevenSprague\OneDrive - Rivetz Corp\Rootz\claud project\email-wallet-service-repo"

echo Adding and committing missing models fix...
git add .
git commit -m "Fix compilation errors: Complete missing models and interfaces

- Add WalletType enum  
- Add VerificationInfo, FileMetadataInfo, VirusScanResult classes
- Add IWalletCreatorService and IFileProcessorService interfaces
- Add complete mock implementations with proper namespaces
- Fix all 9 compilation errors for Ubuntu deployment
- Services already registered in Program.cs DI container"

echo Pushing to GitHub...
git push origin main

echo.
echo ✅ Done! Now run on Ubuntu:
echo   cd /opt/email-wallet-service
echo   sudo git pull origin main
echo   cd src/EmailProcessingService  
echo   dotnet build --configuration Release
echo.
