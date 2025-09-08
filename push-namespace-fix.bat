@echo off
echo 🔧 Fixing namespace resolution issues...
echo.

cd /d "C:\Users\StevenSprague\OneDrive - Rivetz Corp\Rootz\claud project\email-wallet-service-repo"

git add src/EmailProcessingService/Models/MissingModels.cs
git commit -m "Fix namespace resolution for missing types and interfaces

- Fixed namespace resolution issues in MissingModels.cs
- Added explicit using EmailProcessingService.Models; in the Services namespace
- Simplified the implementation to avoid complex logic that might cause additional compilation issues
- Ensured the interface signature exactly matches what FileProcessorService expects

This should resolve the type resolution errors."

git push origin main

echo.
echo ✅ Namespace fix pushed! Now run on Ubuntu:
echo   cd /opt/email-wallet-service
echo   sudo git pull origin main  
echo   sudo ./deployment/scripts/deploy-configured.sh
echo.
pause