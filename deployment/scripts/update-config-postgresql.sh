#!/bin/bash
# Update Email Wallet Service Configuration for PostgreSQL
# Run on Ubuntu server after PostgreSQL setup

echo "⚙️ Updating Email Wallet Service Configuration"
echo "=============================================="

cd /opt/email-wallet-service/src/EmailProcessingService

# Backup current configuration
echo "💾 Creating configuration backup..."
sudo cp appsettings.json appsettings.json.backup.$(date +%Y%m%d-%H%M%S)
sudo cp appsettings.Production.json appsettings.Production.json.backup.$(date +%Y%m%d-%H%M%S)

# Update appsettings.Production.json with PostgreSQL connection
echo "🔧 Updating PostgreSQL connection string..."
sudo tee appsettings.Production.json > /dev/null << 'EOF'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=emailwalletdb;Username=emailwalletuser;Password=SecurePassword2025!",
    "Redis": "localhost:6379"
  },
  "AllowedHosts": "*",
  "Blockchain": {
    "PolygonRpcUrl": "https://rpc-amoy.polygon.technology/",
    "ChainId": 80002,
    "ServiceWalletPrivateKey": "YOUR_PRIVATE_KEY_HERE",
    "ContractRegistration": "0x71C1d6a0DAB73b25dE970E032bafD42a29dC010F",
    "ContractEmailDataWallet": "0x52eBB3761D36496c29FB6A3D5354C449928A4048",
    "ContractAttachmentWallet": "0x5e0e2d3FE611e4FA319ceD3f2CF1fe7EdBb5Dbb7",
    "ContractAuthorization": "0xcC2a65A8870289B1d33bA741069cC2CEEA219573"
  },
  "IPFS": {
    "PinataApiKey": "YOUR_PINATA_API_KEY",
    "PinataSecretApiKey": "YOUR_PINATA_SECRET"
  }
}
EOF

# Also update main appsettings.json for consistency
echo "🔧 Updating main configuration..."
sudo tee appsettings.json > /dev/null << 'EOF'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=emailwalletdb;Username=emailwalletuser;Password=SecurePassword2025!",
    "Redis": "localhost:6379"
  },
  "AllowedHosts": "*"
}
EOF

echo "📄 Configuration files updated!"
echo ""
echo "Updated files:"
echo "  - appsettings.json"
echo "  - appsettings.Production.json"
echo ""
echo "Backup files created:"
echo "  - appsettings.json.backup.$(date +%Y%m%d-%H%M%S)"
echo "  - appsettings.Production.json.backup.$(date +%Y%m%d-%H%M%S)"
