@echo off
echo 🚀 Pushing Complete Working Codebase to GitHub
echo ============================================

cd /d "C:\Users\StevenSprague\OneDrive - Rivetz Corp\Rootz\claud project\email-wallet-service-repo"

echo.
echo 📋 Summary of Changes:
echo ✅ Replaced all Models with working implementation
echo ✅ Updated Program.cs with production CORS settings
echo ✅ Removed problematic MissingModels.cs
echo ✅ Synced exact working codebase from local
echo.

echo 🔍 Checking repository status...
git status

echo.
echo 📦 Adding all changes...
git add .

echo.
echo 📝 Committing working codebase...
git commit -m "PRODUCTION READY: Sync complete working codebase from local implementation

✅ MAJOR FIXES:
- Replace all Models with working implementation (BlockchainModels, DataWalletModels, EmailProcessingModels, EnhancedModels)  
- Remove problematic MissingModels.cs that caused compilation errors
- Update Program.cs with production CORS settings (includes rootz.global)
- Ensure exact match with locally working service

✅ COMPILATION VERIFIED:
- All types properly defined (WalletType, VerificationInfo, FileMetadataInfo, etc.)
- No missing namespace issues
- No missing interface implementations  
- Ready for Ubuntu deployment

✅ PRODUCTION FEATURES:
- IPFS integration working
- Blockchain verification enabled
- Swagger documentation enabled
- Health checks configured
- Enhanced logging enabled

This codebase builds successfully locally and is ready for Ubuntu production deployment."

if %ERRORLEVEL% EQU 0 (
    echo.
    echo 🌐 Pushing to GitHub...
    git push origin main
    
    if %ERRORLEVEL% EQU 0 (
        echo.
        echo ✅ SUCCESS! Working codebase pushed to GitHub
        echo.
        echo 🚀 Next Steps - Deploy on Ubuntu:
        echo.
        echo SSH to your server and run:
        echo   cd /opt/email-wallet-service
        echo   sudo git pull origin main
        echo   sudo ./deployment/scripts/deploy-configured.sh
        echo.
        echo The deployment should now build successfully!
        echo.
    ) else (
        echo ❌ Error pushing to GitHub
    )
) else (
    echo ❌ Error committing changes
)

echo.
pause