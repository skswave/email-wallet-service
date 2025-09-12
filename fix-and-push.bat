@echo off
echo Fixing model property mismatches and pushing...

cd /d "C:\Users\StevenSprague\OneDrive - Rivetz Corp\Rootz\claud project\email-data-wallet-service"

echo Checking git status...
git status

echo Adding changes...
git add .

echo Committing changes...
git commit -m "Fix: Update model property names to match new BlockchainRegistrationModels structure

- Fixed RegistrationController.cs to use new property names:
  * WalletAddress -> UserRegistrationWalletAddress
  * PrimaryEmail -> PrimaryEmailAddress  
  * Success -> IsRegistrationSuccessful
  * ErrorMessage -> RegistrationErrorMessage
  * TransactionHash -> PolygonTransactionHash

- Fixed BlockchainService.cs to use new property names:
  * Same property updates as controller
  * ServiceWalletPrivateKey -> TestWallet.PrivateKey

This resolves compilation errors after git pull introduced new model structure."

echo Pushing to remote...
git push origin main

echo Done! Changes pushed successfully.
pause
