# Email Wallet Blockchain Service

A revolutionary blockchain-based email data wallet service that creates immutable, verified records of email communications on the Polygon network. Part of the Rootz Digital Names ecosystem enabling universal data ownership and origin verification.

## 🌟 Features

- 📧 **Email Processing**: Automated email ingestion and blockchain storage
- 🔐 **Digital Signatures**: Cryptographic verification of email authenticity
- 🏦 **Credit System**: Anti-spam protection through credit-based authorization
- 📎 **IPFS Storage**: Decentralized attachment storage via Pinata
- ⛓️ **Polygon Integration**: Fast, low-cost transactions on Polygon Amoy testnet
- 🛡️ **Anti-Spam Protection**: User consent required for all DATA_WALLET creation
- 📊 **Swagger Documentation**: Interactive API documentation

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK
- Ubuntu 20.04+ (ARM64 or x64)
- Nginx (for production deployment)
- Git

### Development Setup
```bash
git clone https://github.com/skswave/email-wallet-service.git
cd email-wallet-service/src/EmailProcessingService
cp appsettings.json.template appsettings.json
# Edit appsettings.json with your configuration
dotnet restore
dotnet run
```

The service will be available at `https://localhost:7000` with Swagger UI at `https://localhost:7000/swagger`

### Production Deployment on Ubuntu
```bash
git clone https://github.com/skswave/email-wallet-service.git
cd email-wallet-service
chmod +x deployment/scripts/deploy.sh
sudo ./deployment/scripts/deploy.sh
```

## 📡 API Documentation

- **Production**: `https://api.rivetz.global`
- **Swagger UI**: `https://api.rivetz.global/swagger`
- **Health Check**: `https://api.rivetz.global/api/health`

### Key Endpoints

#### Email Processing
- `POST /api/email/process` - Process incoming email and create DATA_WALLET
- `GET /api/email/{walletId}/status` - Check email processing status

#### Wallet Management
- `GET /api/wallet/{address}/registered` - Check wallet registration status
- `GET /api/wallet/{address}/credits` - Check wallet credit balance
- `POST /api/wallet/register` - Register new wallet

#### Authorization Flow
- `POST /api/authorization/request` - Create authorization request
- `POST /api/authorization/approve` - User approves DATA_WALLET creation
- `GET /api/authorization/{requestId}/status` - Check authorization status

#### Blockchain Testing
- `GET /api/blockchaintest/status` - Test blockchain connectivity
- `GET /api/blockchaintest/wallet/{address}/registered` - Test wallet registration
- `GET /api/blockchaintest/wallet/{address}/credits` - Test credit balance

## ⚙️ Configuration

### Environment Setup
Copy configuration templates and customize:

```bash
cp config/appsettings.Production.json.template src/EmailProcessingService/appsettings.Production.json
cp config/environment.template .env
```

### Required Configuration

#### Blockchain Settings
```json
{
  "Blockchain": {
    "Network": "polygon-amoy",
    "RpcUrl": "https://rpc-amoy.polygon.technology",
    "PrivateKey": "your_service_wallet_private_key",
    "ExplorerUrl": "https://amoy.polygonscan.com"
  }
}
```

#### IPFS Configuration
```json
{
  "IPFS": {
    "PinataApiKey": "your_pinata_api_key",
    "PinataSecretKey": "your_pinata_secret_key",
    "PinataJWT": "your_pinata_jwt_token"
  }
}
```

#### Email Settings
```json
{
  "Email": {
    "SmtpSettings": {
      "Host": "smtp.office365.com",
      "Port": 587,
      "Username": "notifications@rootz.global",
      "Password": "your_email_password"
    }
  }
}
```

## 🏗️ Architecture

### Smart Contracts (Polygon Amoy Testnet)

| Contract | Address | Purpose |
|----------|---------|---------|
| EmailWalletRegistration | `0x71C1d6a0DAB73b25dE970E032bafD42a29dC010F` | User registration and credit management |
| EmailDataWallet | `0x52eBB3761D36496c29FB6A3D5354C449928A4048` | Email data storage and retrieval |
| AttachmentWallet | `0x5e0e2d3FE611e4FA319ceD3f2CF1fe7EdBb5Dbb7` | Email attachment management |
| AuthorizationManager | `0x555ba5C1ff253c1D91483b52F1906670608fE9bC` | User consent and authorization flow |

### Technology Stack
- **.NET 8**: High-performance web API framework
- **Nethereum**: Ethereum/Polygon blockchain interaction library
- **IPFS/Pinata**: Decentralized file storage
- **Nginx**: Reverse proxy and SSL termination
- **Systemd**: Service management on Ubuntu
- **Let's Encrypt**: Free SSL certificates

### Email Wallet Creation Flow

```
1. Email Received → 2. User Notification → 3. User Authorization → 4. DATA_WALLET Created
     ↓                      ↓                      ↓                      ↓
Platform detects      User reviews email     MetaMask signature     Immutable blockchain
incoming email        and cost estimate      required for consent   record created
```

## 🔒 Security Features

- **Private Key Management**: Environment variables and secure storage
- **User Consent**: No DATA_WALLET created without explicit authorization
- **Credit System**: Prevents spam through cost-based protection
- **Rate Limiting**: API throttling to prevent abuse
- **HTTPS Required**: All production traffic encrypted
- **Input Validation**: Comprehensive request validation

## 🚀 Development

### Local Development
```bash
# Start the service
cd src/EmailProcessingService
dotnet watch run

# Run tests
cd ../EmailProcessingService.Tests
dotnet test

# Generate code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Testing the Service
```bash
# Test blockchain connectivity
curl -k "https://localhost:7000/api/blockchaintest/status"

# Test wallet registration
curl -k "https://localhost:7000/api/blockchaintest/wallet/0x107C5655ce50AB9744Fc36A4e9935E30d4923d0b/registered"
```

## 📦 Deployment

### Manual Deployment
1. Clone repository to `/opt/email-wallet-service`
2. Configure environment variables
3. Build and publish application
4. Configure nginx and systemd
5. Start services

### Automated Deployment
```bash
# Single command deployment
./deployment/scripts/deploy.sh
```

### Health Monitoring
```bash
# Check service status
sudo systemctl status email-wallet

# View logs
sudo journalctl -u email-wallet -f

# Check nginx status
sudo systemctl status nginx
```

## 🌐 Integration with Rootz Ecosystem

This service is part of the larger Rootz Digital Names infrastructure:

- **Website**: [rootz.global](https://rootz.global)
- **Digital Names**: Wallet-based naming system
- **Data Ownership**: Universal data provenance tracking
- **Economic Recognition**: Data as a tradeable asset

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Commit your changes: `git commit -m 'Add amazing feature'`
4. Push to the branch: `git push origin feature/amazing-feature`
5. Open a Pull Request

### Development Guidelines
- Follow .NET coding standards
- Add unit tests for new features
- Update documentation
- Ensure all tests pass
- Test deployment scripts

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

For issues and questions:
- **GitHub Issues**: [Create an issue](https://github.com/skswave/email-wallet-service/issues)
- **Email**: support@rootz.global
- **Documentation**: See `/docs` folder for detailed guides

## 🔄 Version History

- **v1.0.0** - Initial release with core email processing
- **v1.1.0** - Added authorization flow and anti-spam protection
- **v1.2.0** - Enhanced blockchain integration and error handling

## ⚠️ Important Notes

- This service handles blockchain private keys - secure your environment variables
- Always test on Polygon Amoy testnet before mainnet deployment
- Regular backups of configuration and data recommended
- Monitor gas costs and credit balances

---

**Built with ❤️ for the decentralized future of data ownership**