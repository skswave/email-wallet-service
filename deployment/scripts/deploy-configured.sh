#!/bin/bash
# Email Wallet Service - Automated Deployment Script with Pre-configured Secrets
# Usage: sudo ./deployment/scripts/deploy-configured.sh [email_password]

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
SERVICE_NAME="email-wallet"
SERVICE_DIR="/opt/email-wallet-service"
BACKUP_DIR="/opt/backups/email-wallet"
GITHUB_REPO="https://github.com/skswave/email-wallet-service.git"
SERVICE_PORT="5000"
NGINX_SITE="email-wallet"

# Email password from command line argument
EMAIL_PASSWORD="$1"

echo -e "${BLUE}🚀 Starting Email Wallet Service Deployment with Pre-configured Secrets${NC}"
echo "=================================="

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   echo -e "${RED}❌ This script must be run as root (use sudo)${NC}"
   exit 1
fi

# Check for email password
if [ -z "$EMAIL_PASSWORD" ]; then
    echo -e "${YELLOW}⚠️ Email password not provided as argument${NC}"
    echo "Usage: sudo $0 [email_password]"
    echo "Example: sudo $0 'your_outlook_app_password'"
    echo ""
    echo -e "${YELLOW}You can also set it manually after deployment${NC}"
fi

# Check if .NET 8 is installed
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}❌ .NET 8 SDK not found. Please install .NET 8 first.${NC}"
    exit 1
fi

echo -e "${GREEN}✅ .NET 8 SDK found: $(dotnet --version)${NC}"

# Create backup
echo -e "${YELLOW}📦 Creating backup...${NC}"
mkdir -p $BACKUP_DIR
if [ -d "$SERVICE_DIR" ]; then
    BACKUP_NAME="backup_$(date +%Y%m%d_%H%M%S)"
    cp -r $SERVICE_DIR $BACKUP_DIR/$BACKUP_NAME
    echo -e "${GREEN}✅ Backup created: $BACKUP_DIR/$BACKUP_NAME${NC}"
else
    echo -e "${YELLOW}⚠️ No existing installation found, skipping backup${NC}"
fi

# Stop existing service if running
if systemctl is-active --quiet $SERVICE_NAME; then
    echo -e "${YELLOW}🛑 Stopping existing service...${NC}"
    systemctl stop $SERVICE_NAME
fi

# Clone/update repository
echo -e "${YELLOW}📥 Updating source code...${NC}"
if [ -d "$SERVICE_DIR" ]; then
    echo "Updating existing repository..."
    cd $SERVICE_DIR
    git fetch origin
    git reset --hard origin/main
    git clean -fd
else
    echo "Cloning new repository..."
    git clone $GITHUB_REPO $SERVICE_DIR
fi

cd $SERVICE_DIR
echo -e "${GREEN}✅ Source code updated${NC}"

# Set proper ownership
chown -R ubuntu:ubuntu $SERVICE_DIR

# Create production configuration with all secrets
echo -e "${YELLOW}⚙️ Creating production configuration with pre-configured secrets...${NC}"
CONFIG_FILE="$SERVICE_DIR/src/EmailProcessingService/appsettings.Production.json"

# Use the complete configuration file
if [ -f "$SERVICE_DIR/config/appsettings.Production.json.complete" ]; then
    cp $SERVICE_DIR/config/appsettings.Production.json.complete $CONFIG_FILE
    
    # Replace email password if provided
    if [ ! -z "$EMAIL_PASSWORD" ]; then
        sed -i "s/YOUR_EMAIL_PASSWORD_HERE/$EMAIL_PASSWORD/g" $CONFIG_FILE
        echo -e "${GREEN}✅ Email password configured${NC}"
    else
        echo -e "${YELLOW}⚠️ Email password placeholder left in config${NC}"
        echo "You can update it later by editing: $CONFIG_FILE"
    fi
    
    echo -e "${GREEN}✅ Production configuration created with all blockchain and IPFS secrets${NC}"
else
    echo -e "${RED}❌ Complete configuration template not found!${NC}"
    exit 1
fi

# Create required directories
echo -e "${YELLOW}📁 Creating required directories...${NC}"
mkdir -p $SERVICE_DIR/logs
mkdir -p $SERVICE_DIR/data
mkdir -p $SERVICE_DIR/quarantine
chown -R ubuntu:ubuntu $SERVICE_DIR/logs
chown -R ubuntu:ubuntu $SERVICE_DIR/data
chown -R ubuntu:ubuntu $SERVICE_DIR/quarantine

# Restore and build application
echo -e "${YELLOW}🔨 Building application...${NC}"
cd $SERVICE_DIR/src/EmailProcessingService

# Clean previous builds
rm -rf bin/ obj/

# Restore packages
echo "Restoring NuGet packages..."
dotnet restore
if [ $? -ne 0 ]; then
    echo -e "${RED}❌ Failed to restore packages${NC}"
    exit 1
fi

# Build and publish
echo "Building and publishing application..."
dotnet publish -c Release -o $SERVICE_DIR/build --no-restore
if [ $? -ne 0 ]; then
    echo -e "${RED}❌ Failed to build application${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Application built successfully${NC}"

# Install systemd service
echo -e "${YELLOW}⚙️ Installing systemd service...${NC}"
if [ -f "$SERVICE_DIR/deployment/systemd/email-wallet.service" ]; then
    cp $SERVICE_DIR/deployment/systemd/email-wallet.service /etc/systemd/system/
    systemctl daemon-reload
    systemctl enable $SERVICE_NAME
    echo -e "${GREEN}✅ Systemd service installed${NC}"
else
    echo -e "${YELLOW}⚠️ Systemd service file not found, creating default...${NC}"
    
    cat > /etc/systemd/system/$SERVICE_NAME.service << EOF
[Unit]
Description=Email Wallet Blockchain Service
After=network.target

[Service]
Type=notify
User=ubuntu
WorkingDirectory=$SERVICE_DIR/build
ExecStart=/usr/bin/dotnet EmailProcessingService.dll
Restart=always
RestartSec=5
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:$SERVICE_PORT

[Install]
WantedBy=multi-user.target
EOF
    
    systemctl daemon-reload
    systemctl enable $SERVICE_NAME
    echo -e "${GREEN}✅ Default systemd service created${NC}"
fi

# Update nginx configuration
echo -e "${YELLOW}🌐 Updating nginx configuration...${NC}"
if [ -f "$SERVICE_DIR/deployment/nginx/email-wallet.conf" ]; then
    cp $SERVICE_DIR/deployment/nginx/email-wallet.conf /etc/nginx/sites-available/$NGINX_SITE
    
    # Enable site if not already enabled
    if [ ! -L "/etc/nginx/sites-enabled/$NGINX_SITE" ]; then
        ln -s /etc/nginx/sites-available/$NGINX_SITE /etc/nginx/sites-enabled/
    fi
    
    # Test nginx configuration
    nginx -t
    if [ $? -eq 0 ]; then
        systemctl reload nginx
        echo -e "${GREEN}✅ Nginx configuration updated${NC}"
    else
        echo -e "${RED}❌ Nginx configuration test failed${NC}"
        exit 1
    fi
else
    echo -e "${YELLOW}⚠️ Nginx configuration file not found${NC}"
    echo "Service will be available only on port $SERVICE_PORT"
fi

# Start service
echo -e "${YELLOW}🚀 Starting service...${NC}"
systemctl start $SERVICE_NAME

# Wait for service to start
sleep 5

# Health check
echo -e "${YELLOW}🏥 Running health check...${NC}"
if systemctl is-active --quiet $SERVICE_NAME; then
    echo -e "${GREEN}✅ Service is running${NC}"
    
    # Test HTTP endpoint
    if curl -f -s http://localhost:$SERVICE_PORT/api/health > /dev/null; then
        echo -e "${GREEN}✅ Health check passed${NC}"
    else
        echo -e "${YELLOW}⚠️ Health endpoint not responding (checking logs...)${NC}"
        journalctl -u $SERVICE_NAME --no-pager -n 20
    fi
    
    # Test blockchain connectivity
    echo -e "${YELLOW}🔗 Testing blockchain connectivity...${NC}"
    if curl -f -s http://localhost:$SERVICE_PORT/api/blockchaintest/status > /dev/null; then
        echo -e "${GREEN}✅ Blockchain test endpoint accessible${NC}"
    else
        echo -e "${YELLOW}⚠️ Blockchain test endpoint not responding${NC}"
    fi
    
else
    echo -e "${RED}❌ Service failed to start${NC}"
    echo "Checking logs..."
    journalctl -u $SERVICE_NAME --no-pager -l
    exit 1
fi

# Show service status
echo -e "${BLUE}📊 Service Status:${NC}"
systemctl status $SERVICE_NAME --no-pager -l

# Show useful information
echo ""
echo -e "${GREEN}🎉 Email Wallet Service deployed successfully with pre-configured secrets!${NC}"
echo "=================================="
echo -e "${BLUE}Service Information:${NC}"
echo "• Service Name: $SERVICE_NAME"
echo "• Installation Directory: $SERVICE_DIR"
echo "• Service Port: $SERVICE_PORT"
echo "• Configuration: $CONFIG_FILE"
echo "• Log Command: sudo journalctl -u $SERVICE_NAME -f"
echo "• Status Command: sudo systemctl status $SERVICE_NAME"
echo ""
echo -e "${BLUE}Pre-configured Components:${NC}"
echo "✅ Blockchain: Polygon Amoy testnet with your deployed contracts"
echo "✅ IPFS: Pinata integration with your API keys"
echo "✅ Wallet: Service wallet (0xE2d0E252E7da22901bd1ffDD012Da2c77aC3033a)"
echo "✅ Real Transactions: Enabled for production use"
echo ""
echo -e "${BLUE}Available Endpoints:${NC}"
echo "• Health Check: http://localhost:$SERVICE_PORT/api/health"
echo "• Swagger UI: http://localhost:$SERVICE_PORT/swagger"
echo "• Blockchain Test: http://localhost:$SERVICE_PORT/api/blockchaintest/status"
echo "• Wallet Registration Test: http://localhost:$SERVICE_PORT/api/blockchaintest/wallet/[address]/registered"

if [ -f "/etc/nginx/sites-enabled/$NGINX_SITE" ]; then
    echo ""
    echo -e "${BLUE}Nginx Configuration:${NC}"
    echo "• Configuration: /etc/nginx/sites-available/$NGINX_SITE"
    echo "• Reload Nginx: sudo systemctl reload nginx"
    echo "• Test Config: sudo nginx -t"
fi

if [ -z "$EMAIL_PASSWORD" ]; then
    echo ""
    echo -e "${YELLOW}⚠️ Email Configuration Required:${NC}"
    echo "To enable email notifications, update the password in:"
    echo "  $CONFIG_FILE"
    echo "Find: \"Password\": \"YOUR_EMAIL_PASSWORD_HERE\""
    echo "Replace with your Outlook app password"
    echo "Then restart: sudo systemctl restart $SERVICE_NAME"
fi

echo ""
echo -e "${BLUE}Next Steps:${NC}"
echo "1. Configure DNS to point api.rivetz.global to this server"
echo "2. Set up SSL certificate: sudo certbot --nginx -d api.rivetz.global"
echo "3. Test blockchain operations: curl http://localhost:$SERVICE_PORT/api/blockchaintest/status"
echo "4. Monitor logs: sudo journalctl -u $SERVICE_NAME -f"

echo ""
echo -e "${GREEN}✨ Email Wallet Service is ready for production use!${NC}"