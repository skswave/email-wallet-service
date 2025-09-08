#!/bin/bash
# Setup Systemd Service for Email Wallet Service
# Run this after database setup is complete

echo "🔧 Setting up Systemd Service for Email Wallet Service"
echo "======================================================"

# Create systemd service file
echo "📝 Creating systemd service configuration..."
sudo tee /etc/systemd/system/email-wallet-service.service > /dev/null << 'EOF'
[Unit]
Description=Email Wallet Service - Blockchain Email Processing
After=network.target postgresql.service
Wants=postgresql.service

[Service]
Type=notify
# User and group
User=ubuntu
Group=ubuntu

# Working directory
WorkingDirectory=/opt/email-wallet-service/src/EmailProcessingService/publish

# Command to run
ExecStart=/usr/bin/dotnet EmailProcessingService.dll --urls="http://0.0.0.0:5000"

# Environment variables
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

# Restart policy
Restart=always
RestartSec=10

# Logging
StandardOutput=journal
StandardError=journal
SyslogIdentifier=email-wallet-service

# Security settings
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/opt/email-wallet-service
NoNewPrivileges=true

# Limits
LimitNOFILE=65536
TimeoutStopSec=30

[Install]
WantedBy=multi-user.target
EOF

# Reload systemd daemon
echo "🔄 Reloading systemd daemon..."
sudo systemctl daemon-reload

# Enable the service to start on boot
echo "🚀 Enabling email-wallet-service to start on boot..."
sudo systemctl enable email-wallet-service

# Create log directory
echo "📁 Creating log directory..."
sudo mkdir -p /var/log/email-wallet-service
sudo chown ubuntu:ubuntu /var/log/email-wallet-service

# Create a helper script for service management
echo "🛠️ Creating service management script..."
sudo tee /usr/local/bin/email-wallet-ctl > /dev/null << 'EOF'
#!/bin/bash
# Email Wallet Service Control Script

case "$1" in
    start)
        echo "Starting Email Wallet Service..."
        sudo systemctl start email-wallet-service
        ;;
    stop)
        echo "Stopping Email Wallet Service..."
        sudo systemctl stop email-wallet-service
        ;;
    restart)
        echo "Restarting Email Wallet Service..."
        sudo systemctl restart email-wallet-service
        ;;
    status)
        sudo systemctl status email-wallet-service --no-pager
        ;;
    logs)
        sudo journalctl -u email-wallet-service -f
        ;;
    logs-tail)
        sudo journalctl -u email-wallet-service -n 50 --no-pager
        ;;
    enable)
        echo "Enabling Email Wallet Service to start on boot..."
        sudo systemctl enable email-wallet-service
        ;;
    disable)
        echo "Disabling Email Wallet Service from starting on boot..."
        sudo systemctl disable email-wallet-service
        ;;
    health)
        echo "Checking service health..."
        curl -s http://localhost:5000/health | jq . || curl -s http://localhost:5000/health
        ;;
    *)
        echo "Usage: $0 {start|stop|restart|status|logs|logs-tail|enable|disable|health}"
        echo ""
        echo "Commands:"
        echo "  start      - Start the service"
        echo "  stop       - Stop the service"
        echo "  restart    - Restart the service"
        echo "  status     - Show service status"
        echo "  logs       - Follow logs in real-time"
        echo "  logs-tail  - Show last 50 log entries"
        echo "  enable     - Enable auto-start on boot"
        echo "  disable    - Disable auto-start on boot"
        echo "  health     - Check service health endpoint"
        exit 1
        ;;
esac
EOF

# Make the control script executable
sudo chmod +x /usr/local/bin/email-wallet-ctl

echo ""
echo "✅ Systemd service setup complete!"
echo ""
echo "Service Management Commands:"
echo "  email-wallet-ctl start     - Start the service"
echo "  email-wallet-ctl stop      - Stop the service"
echo "  email-wallet-ctl restart   - Restart the service"
echo "  email-wallet-ctl status    - Check service status"
echo "  email-wallet-ctl logs      - Follow logs"
echo "  email-wallet-ctl health    - Check health endpoint"
echo ""
echo "Or use systemctl directly:"
echo "  sudo systemctl start email-wallet-service"
echo "  sudo systemctl status email-wallet-service"
echo "  sudo journalctl -u email-wallet-service -f"
echo ""
echo "To start the service now:"
echo "  email-wallet-ctl start"
echo ""
echo "The service is configured to start automatically on system boot."
