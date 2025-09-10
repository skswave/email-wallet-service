# Email Wallet Service Deployment Guide

## Quick Setup

### 1. Prerequisites
```bash
# Install .NET 8
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update
sudo apt install -y dotnet-sdk-8.0
```

### 2. Clone and Deploy
```bash
# Clone repository
sudo git clone https://github.com/skswave/email-wallet-service.git /opt/email-wallet-service
cd /opt/email-wallet-service

# Configure production settings
sudo cp config/appsettings.Production.json.template src/EmailProcessingService/appsettings.Production.json
sudo nano src/EmailProcessingService/appsettings.Production.json

# Run automated deployment
sudo chmod +x deployment/scripts/deploy.sh
sudo ./deployment/scripts/deploy.sh
```

### 3. SSL Setup (Optional)
```bash
# Install Certbot
sudo apt install certbot python3-certbot-nginx

# Get SSL certificate
sudo certbot --nginx -d api.rivetz.global
```

## Configuration

### Required Environment Variables
- `Blockchain.PrivateKey` - Your service wallet private key
- `IPFS.PinataApiKey` - Pinata API key
- `IPFS.PinataSecretKey` - Pinata secret key
- `IPFS.PinataJWT` - Pinata JWT token
- `Email.SmtpSettings.Password` - Email password

### Contract Addresses (Polygon Amoy)
- EmailWalletRegistration: `0x71C1d6a0DAB73b25dE970E032bafD42a29dC010F`
- EmailDataWallet: `0x52eBB3761D36496c29FB6A3D5354C449928A4048`
- AttachmentWallet: `0x5e0e2d3FE611e4FA319ceD3f2CF1fe7EdBb5Dbb7`
- AuthorizationManager: `0x555ba5C1ff253c1D91483b52F1906670608fE9bC`

## Service Management

```bash
# Check service status
sudo systemctl status email-wallet

# View logs
sudo journalctl -u email-wallet -f

# Restart service
sudo systemctl restart email-wallet

# Check nginx
sudo systemctl status nginx
sudo nginx -t
```

## Testing

```bash
# Health check
curl http://localhost:5000/api/health

# Blockchain test
curl http://localhost:5000/api/blockchaintest/status

# Test specific wallet
curl "http://localhost:5000/api/blockchaintest/wallet/0x107C5655ce50AB9744Fc36A4e9935E30d4923d0b/registered"
```

## Troubleshooting

### Service Won't Start
1. Check configuration file exists
2. Verify private key format
3. Check .NET installation
4. Review logs: `sudo journalctl -u email-wallet -l`

### Nginx Issues
1. Test config: `sudo nginx -t`
2. Check site enabled: `ls -la /etc/nginx/sites-enabled/`
3. Verify port 5000 is accessible: `netstat -tlnp | grep 5000`

### Blockchain Connection Issues
1. Verify RPC URL accessibility
2. Check private key has funds
3. Test contract addresses on Polygon scanner
4. Verify network configuration