#!/bin/bash
# Complete Database Setup for Email Wallet Service
# Run this script on Ubuntu server at rootz.global

echo "🗄️ Complete PostgreSQL Setup for Email Wallet Service"
echo "======================================================"

# Step 1: Install PostgreSQL
echo "📦 Step 1: Installing PostgreSQL..."
sudo apt update
sudo apt install -y postgresql postgresql-contrib

# Step 2: Start PostgreSQL service
echo "🚀 Step 2: Starting PostgreSQL service..."
sudo systemctl start postgresql
sudo systemctl enable postgresql

# Step 3: Create database and user
echo "👤 Step 3: Creating database and user..."
sudo -u postgres psql << 'EOF'
-- Create database
CREATE DATABASE emailwalletdb;

-- Create user with password
CREATE USER emailwalletuser WITH PASSWORD 'SecurePassword2025!';

-- Grant privileges
GRANT ALL PRIVILEGES ON DATABASE emailwalletdb TO emailwalletuser;

-- Connect to the database to set schema permissions
\c emailwalletdb;

-- Grant schema privileges
GRANT ALL ON SCHEMA public TO emailwalletuser;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO emailwalletuser;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO emailwalletuser;

-- Grant default privileges for future objects
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO emailwalletuser;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO emailwalletuser;

-- Show database info
\l
\du

-- Test connection as new user
\q
EOF

# Step 4: Test connection as the new user
echo "🔧 Step 4: Testing database connection..."
PGPASSWORD='SecurePassword2025!' psql -h localhost -U emailwalletuser -d emailwalletdb -c "SELECT version();"

if [ $? -eq 0 ]; then
    echo "✅ Database connection successful!"
else
    echo "❌ Database connection failed!"
    exit 1
fi

# Step 5: Update service configuration
echo "⚙️ Step 5: Updating service configuration..."
cd /opt/email-wallet-service/src/EmailProcessingService

# Backup current configuration
sudo cp appsettings.Production.json appsettings.Production.json.backup.$(date +%Y%m%d-%H%M%S)

# Update PostgreSQL connection string
sudo tee appsettings.Production.json > /dev/null << 'EOF'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=emailwalletdb;Username=emailwalletuser;Password=SecurePassword2025!;Include Error Detail=true",
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

# Step 6: Test the service with PostgreSQL
echo "🧪 Step 6: Testing service with PostgreSQL..."
cd publish

# Start service temporarily to test database
echo "Starting service for database test..."
timeout 30s dotnet EmailProcessingService.dll --urls="http://0.0.0.0:5000" &
SERVICE_PID=$!

sleep 10

# Test health check
echo "Testing health endpoint..."
curl -s http://localhost:5000/health | jq . || curl -s http://localhost:5000/health

# Stop test service
if kill $SERVICE_PID 2>/dev/null; then
    echo "Service stopped successfully"
fi

echo ""
echo "✅ PostgreSQL setup complete!"
echo ""
echo "Database Configuration:"
echo "  Host: localhost"
echo "  Database: emailwalletdb"
echo "  User: emailwalletuser"
echo "  Password: SecurePassword2025!"
echo "  Port: 5432 (default)"
echo ""
echo "Connection String:"
echo "  Host=localhost;Database=emailwalletdb;Username=emailwalletuser;Password=SecurePassword2025!;Include Error Detail=true"
echo ""
echo "To start the service with PostgreSQL:"
echo "  cd /opt/email-wallet-service/src/EmailProcessingService/publish"
echo "  dotnet EmailProcessingService.dll --urls=\"http://0.0.0.0:5000\""
echo ""
echo "Service URLs:"
echo "  API: http://rootz.global:5000"
echo "  Health: http://rootz.global:5000/health"
echo "  Swagger: http://rootz.global:5000/swagger"
