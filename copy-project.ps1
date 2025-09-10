# Copy EmailProcessingService to repository structure
$sourcePath = "C:\Users\StevenSprague\OneDrive - Rivetz Corp\Rootz\claud project\email-data-wallet-service\implementation\EmailProcessingService"
$destPath = "C:\Users\StevenSprague\OneDrive - Rivetz Corp\Rootz\claud project\email-wallet-service-repo\src\EmailProcessingService"

Write-Host "Copying EmailProcessingService from:"
Write-Host "  Source: $sourcePath"
Write-Host "  Destination: $destPath"

if (Test-Path $sourcePath) {
    robocopy $sourcePath $destPath /E /NP /NDL /NFL
    Write-Host "✅ EmailProcessingService copied successfully"
} else {
    Write-Host "❌ Source path not found: $sourcePath"
}
