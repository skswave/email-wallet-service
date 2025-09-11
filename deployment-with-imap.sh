#!/bin/bash

# Email Wallet Service Deployment with IMAP Monitoring
# September 11, 2025

set -e  # Exit on any error

echo "=========================================="
echo "Email Wallet Service Deployment with IMAP"
echo "=========================================="

# Configuration
PROJECT_DIR="/opt/email-wallet-service"
SERVICE_NAME="email-wallet-service"
BUILD_DIR="$PROJECT_DIR/src/EmailProcessingService"
BACKUP_DIR="/opt/email-wallet-backups/$(date +%Y%m%d-%H%M%S)"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Functions
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if running as root or with sudo
if [[ $EUID -eq 0 ]]; then
    log_error "This script should not be run as root. Use regular user with sudo privileges."
    exit 1
fi

# Verify we're on the correct server
if [[ "$(hostname)" != *"rootz"* ]]; then
    log_warn "This doesn't appear to be the rootz.global server. Continue? [y/N]"
    read -r response
    if [[ ! "$response" =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

# Step 1: Create backup
log_info "Creating backup of current deployment..."
sudo mkdir -p "$BACKUP_DIR"
if [ -d "$BUILD_DIR/publish" ]; then
    sudo cp -r "$BUILD_DIR/publish" "$BACKUP_DIR/"
    log_info "Backup created at $BACKUP_DIR"
else
    log_warn "No existing publish directory found to backup"
fi

# Step 2: Stop the service
log_info "Stopping email wallet service..."
if sudo systemctl is-active --quiet "$SERVICE_NAME"; then
    sudo systemctl stop "$SERVICE_NAME"
    log_info "Service stopped"
else
    log_warn "Service was not running"
fi

# Step 3: Pull latest code
log_info "Pulling latest code from repository..."
cd "$PROJECT_DIR"
sudo -u ubuntu git pull origin main
if [ $? -eq 0 ]; then
    log_info "Code updated successfully"
else
    log_error "Failed to pull latest code"
    exit 1
fi

# Step 4: Update configuration for IMAP
log_info "Checking IMAP configuration..."
APPSETTINGS_FILE="$BUILD_DIR/appsettings.json"
APPSETTINGS_PROD="$BUILD_DIR/appsettings.Production.json"

# Check if IMAP configuration exists
if grep -q "\"Imap\":" "$APPSETTINGS_FILE"; then
    log_info "IMAP configuration found in appsettings.json"
else
    log_warn "IMAP configuration not found. Please update manually:"
    echo "  - Add Email:Imap section with process@rivetz.com credentials"
    echo "  - Set Email:Imap:Enabled to true"
    echo "  - Configure Email:SmtpSettings for notifications"
fi

# Step 5: Build the application
log_info "Building the application..."
cd "$BUILD_DIR"
sudo -u ubuntu dotnet publish -c Release -o publish --no-restore
if [ $? -eq 0 ]; then
    log_info "Build completed successfully"
else
    log_error "Build failed"
    exit 1
fi

# Step 6: Update permissions
log_info "Updating file permissions..."
sudo chown -R www-data:www-data "$BUILD_DIR/publish"
sudo chmod +x "$BUILD_DIR/publish/EmailProcessingService"

# Step 7: Update systemd service file if needed
SERVICE_FILE="/etc/systemd/system/$SERVICE_NAME.service"
if [ ! -f "$SERVICE_FILE" ]; then
    log_info "Creating systemd service file..."
    sudo tee "$SERVICE_FILE" > /dev/null <<EOF
[Unit]
Description=Email Data Wallet Processing Service with IMAP Monitoring
After=network.target

[Service]
Type=notify
User=www-data
Group=www-data
WorkingDirectory=$BUILD_DIR/publish
ExecStart=/usr/bin/dotnet $BUILD_DIR/publish/EmailProcessingService.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=email-wallet-service
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
EOF
    sudo systemctl daemon-reload
    sudo systemctl enable "$SERVICE_NAME"
    log_info "Systemd service created and enabled"
fi

# Step 8: Start the service
log_info "Starting email wallet service..."
sudo systemctl start "$SERVICE_NAME"
sleep 5

# Step 9: Check service status
log_info "Checking service status..."
if sudo systemctl is-active --quiet "$SERVICE_NAME"; then
    log_info "✅ Service is running successfully"
    
    # Show service status
    sudo systemctl status "$SERVICE_NAME" --no-pager -l
    
    # Test the API
    log_info "Testing API endpoints..."
    
    # Health check
    if curl -s -f "http://localhost:5000/health" > /dev/null; then
        log_info "✅ Health check endpoint responding"
    else
        log_warn "❌ Health check endpoint not responding"
    fi
    
    # IMAP monitor status
    if curl -s -f "http://localhost:5000/api/emailmonitor/status" > /dev/null; then
        log_info "✅ IMAP monitor endpoint responding"
    else
        log_warn "❌ IMAP monitor endpoint not responding"
    fi
    
    # Swagger endpoint
    if curl -s -f "http://localhost:5000/swagger/index.html" > /dev/null; then
        log_info "✅ Swagger documentation available"
    else
        log_warn "❌ Swagger documentation not responding"
    fi
    
else
    log_error "❌ Service failed to start"
    log_error "Checking service logs..."
    sudo journalctl -u "$SERVICE_NAME" --no-pager -l --since "1 minute ago"
    exit 1
fi

# Step 10: Test IMAP connectivity
log_info "Testing IMAP connectivity..."
IMAP_TEST=$(curl -s "http://localhost:5000/api/emailmonitor/test-connection" || echo "failed")
if echo "$IMAP_TEST" | grep -q '"success":true'; then
    log_info "✅ IMAP connection test successful"
else
    log_warn "❌ IMAP connection test failed - check credentials"
    echo "Response: $IMAP_TEST"
fi

# Step 11: Show useful URLs
log_info "=========================================="
log_info "✅ Deployment completed successfully!"
log_info "=========================================="
echo
log_info "Service URLs:"
echo "  API Base:          http://rootz.global:5000"
echo "  Swagger UI:        http://rootz.global:5000/swagger"
echo "  Health Check:      http://rootz.global:5000/health"
echo "  IMAP Status:       http://rootz.global:5000/api/emailmonitor/status"
echo "  Email Monitor:     http://rootz.global:5000/api/emailmonitor/unread-emails"
echo
log_info "Management Commands:"
echo "  sudo systemctl status $SERVICE_NAME"
echo "  sudo systemctl restart $SERVICE_NAME"
echo "  sudo journalctl -u $SERVICE_NAME -f"
echo
log_info "Next Steps:"
echo "  1. Configure process@rivetz.com email credentials"
echo "  2. Set up email forwarding rules"
echo "  3. Test end-to-end email processing"
echo "  4. Configure notification email addresses"
echo
log_info "Backup location: $BACKUP_DIR"