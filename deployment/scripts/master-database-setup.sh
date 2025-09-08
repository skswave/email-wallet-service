#!/bin/bash
# Master Database Setup Script for Email Wallet Service
# This script orchestrates the complete database setup process

echo "🚀 Email Wallet Service - Complete Database Setup"
echo "=================================================="
echo "This script will:"
echo "1. Install and configure PostgreSQL"
echo "2. Create database and user"
echo "3. Create database schema"
echo "4. Update service configuration"
echo "5. Setup systemd service"
echo "6. Test the complete setup"
echo ""

# Check if running as root
if [ "$EUID" -ne 0 ]; then
    echo "⚠️  This script needs sudo privileges. Please run with sudo or as root."
    exit 1
fi

# Get the actual user (in case running with sudo)
ACTUAL_USER=${SUDO_USER:-$USER}
echo "Running as: $ACTUAL_USER"
echo ""

read -p "Do you want to continue with the database setup? (y/N): " -n 1 -r
echo ""
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Setup cancelled."
    exit 0
fi

# Set variables
DB_NAME="emailwalletdb"
DB_USER="emailwalletuser"
DB_PASSWORD="SecurePassword2025!"
SERVICE_DIR="/opt/email-wallet-service/src/EmailProcessingService"

echo "📋 Setup Configuration:"
echo "  Database: $DB_NAME"
echo "  User: $DB_USER"
echo "  Service Directory: $SERVICE_DIR"
echo ""

# Step 1: Install PostgreSQL
echo "📦 Step 1: Installing PostgreSQL..."
apt update
apt install -y postgresql postgresql-contrib jq

# Step 2: Start PostgreSQL
echo "🚀 Step 2: Starting PostgreSQL..."
systemctl start postgresql
systemctl enable postgresql

# Step 3: Create database and user
echo "👤 Step 3: Creating database and user..."
sudo -u postgres psql << EOF
-- Drop database if exists (for clean setup)
DROP DATABASE IF EXISTS $DB_NAME;
DROP USER IF EXISTS $DB_USER;

-- Create database
CREATE DATABASE $DB_NAME;

-- Create user with password
CREATE USER $DB_USER WITH PASSWORD '$DB_PASSWORD';

-- Grant privileges
GRANT ALL PRIVILEGES ON DATABASE $DB_NAME TO $DB_USER;

-- Connect to database and set permissions
\c $DB_NAME;
GRANT ALL ON SCHEMA public TO $DB_USER;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO $DB_USER;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO $DB_USER;

\q
EOF

# Step 4: Create database schema
echo "🗄️ Step 4: Creating database schema..."
PGPASSWORD=$DB_PASSWORD psql -h localhost -U $DB_USER -d $DB_NAME << 'EOF'
-- User Registration table
CREATE TABLE IF NOT EXISTS UserRegistrations (
    Id SERIAL PRIMARY KEY,
    WalletAddress VARCHAR(42) NOT NULL UNIQUE,
    Email VARCHAR(255),
    IsActive BOOLEAN DEFAULT true,
    RegistrationDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    LastActivity TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    Credits INTEGER DEFAULT 0,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Email Processing Tasks table
CREATE TABLE IF NOT EXISTS EmailProcessingTasks (
    Id SERIAL PRIMARY KEY,
    TaskId VARCHAR(100) NOT NULL UNIQUE,
    UserWalletAddress VARCHAR(42) NOT NULL,
    EmailMessageId VARCHAR(255),
    Status VARCHAR(50) DEFAULT 'pending',
    ProcessingLog TEXT,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CompletedAt TIMESTAMP,
    ErrorMessage TEXT
);

-- Email Data Wallets table
CREATE TABLE IF NOT EXISTS EmailDataWallets (
    Id SERIAL PRIMARY KEY,
    WalletId VARCHAR(100) NOT NULL UNIQUE,
    OwnerAddress VARCHAR(42) NOT NULL,
    EmailMessageId VARCHAR(255),
    Subject TEXT,
    Sender VARCHAR(255),
    Recipients TEXT,
    Timestamp TIMESTAMP,
    ContentHash VARCHAR(66),
    IpfsHash VARCHAR(66),
    BlockchainTxHash VARCHAR(66),
    Status VARCHAR(50) DEFAULT 'active',
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Authorization Requests table
CREATE TABLE IF NOT EXISTS AuthorizationRequests (
    Id SERIAL PRIMARY KEY,
    RequestId VARCHAR(66) NOT NULL UNIQUE,
    UserAddress VARCHAR(42) NOT NULL,
    EmailHash VARCHAR(66),
    AuthToken VARCHAR(255),
    CreditCost INTEGER DEFAULT 0,
    Status INTEGER DEFAULT 0,
    ExpiresAt TIMESTAMP,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    AuthorizedAt TIMESTAMP,
    ProcessedAt TIMESTAMP
);

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_user_registrations_wallet ON UserRegistrations(WalletAddress);
CREATE INDEX IF NOT EXISTS idx_email_tasks_user ON EmailProcessingTasks(UserWalletAddress);
CREATE INDEX IF NOT EXISTS idx_email_wallets_owner ON EmailDataWallets(OwnerAddress);
CREATE INDEX IF NOT EXISTS idx_auth_requests_user ON AuthorizationRequests(UserAddress);

-- Insert test user
INSERT INTO UserRegistrations (WalletAddress, Email, Credits) VALUES 
    ('0x107C5655ce50AB9744Fc36A4e9935E30d4923d0b', 'test@example.com', 100)
ON CONFLICT (WalletAddress) DO NOTHING;

SELECT 'Database schema created successfully!' as status;
EOF

# Step 5: Update service configuration
echo "⚙️ Step 5: Updating service configuration..."
cd $SERVICE_DIR

# Backup current configuration
cp appsettings.Production.json appsettings.Production.json.backup.$(date +%Y%m%d-%H%M%S) 2>/dev/null || true

# Create new configuration with PostgreSQL
cat > appsettings.Production.json << EOF
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD;Include Error Detail=true",
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

# Step 6: Test database connection
echo "🔧 Step 6: Testing database connection..."
PGPASSWORD=$DB_PASSWORD psql -h localhost -U $DB_USER -d $DB_NAME -c "SELECT version();" > /dev/null

if [ $? -eq 0 ]; then
    echo "✅ Database connection successful!"
else
    echo "❌ Database connection failed!"
    exit 1
fi

# Step 7: Setup systemd service
echo "🔧 Step 7: Setting up systemd service..."

# Create systemd service file
cat > /etc/systemd/system/email-wallet-service.service << 'EOF'
[Unit]
Description=Email Wallet Service - Blockchain Email Processing
After=network.target postgresql.service
Wants=postgresql.service

[Service]
Type=notify
User=ubuntu
Group=ubuntu
WorkingDirectory=/opt/email-wallet-service/src/EmailProcessingService/publish
ExecStart=/usr/bin/dotnet EmailProcessingService.dll --urls="http://0.0.0.0:5000"
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
Restart=always
RestartSec=10
StandardOutput=journal
StandardError=journal
SyslogIdentifier=email-wallet-service
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/opt/email-wallet-service
NoNewPrivileges=true
LimitNOFILE=65536
TimeoutStopSec=30

[Install]
WantedBy=multi-user.target
EOF

# Create service management script
cat > /usr/local/bin/email-wallet-ctl << 'EOF'
#!/bin/bash
case "$1" in
    start)   sudo systemctl start email-wallet-service ;;
    stop)    sudo systemctl stop email-wallet-service ;;
    restart) sudo systemctl restart email-wallet-service ;;
    status)  sudo systemctl status email-wallet-service --no-pager ;;
    logs)    sudo journalctl -u email-wallet-service -f ;;
    health)  curl -s http://localhost:5000/health | jq . 2>/dev/null || curl -s http://localhost:5000/health ;;
    *)       echo "Usage: $0 {start|stop|restart|status|logs|health}" ;;
esac
EOF

chmod +x /usr/local/bin/email-wallet-ctl

# Reload systemd and enable service
systemctl daemon-reload
systemctl enable email-wallet-service

# Fix permissions
chown -R $ACTUAL_USER:$ACTUAL_USER /opt/email-wallet-service

echo ""
echo "✅ Database setup complete!"
echo ""
echo "📊 Setup Summary:"
echo "  Database: $DB_NAME (PostgreSQL)"
echo "  User: $DB_USER"
echo "  Connection: Host=localhost;Database=$DB_NAME;Username=$DB_USER"
echo "  Service: email-wallet-service (systemd)"
echo "  Auto-start: Enabled"
echo ""
echo "🎯 Next Steps:"
echo "1. Start the service:"
echo "   email-wallet-ctl start"
echo ""
echo "2. Check service status:"
echo "   email-wallet-ctl status"
echo ""
echo "3. Test the API:"
echo "   curl http://rootz.global:5000/health"
echo ""
echo "4. View logs:"
echo "   email-wallet-ctl logs"
echo ""
echo "Service URLs:"
echo "  API: http://rootz.global:5000"
echo "  Health: http://rootz.global:5000/health"
echo "  Swagger: http://rootz.global:5000/swagger"
