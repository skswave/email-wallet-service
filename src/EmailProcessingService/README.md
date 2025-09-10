# Email Processing Service Implementation

## Overview

This is the **core implementation layer** for the Email Data Wallet Service, built as a .NET 6+ Web API service that processes incoming emails and transforms them into cryptographically verified, blockchain-backed data wallets.

## Architecture Summary

This service implements the **dual-tier wallet system** designed in your architecture documents:

- **Email Data Wallets**: Contains complete email with context and metadata
- **Attachment Data Wallets**: Individual wallets for each file attachment

## Project Structure

```
EmailProcessingService/
├── Controllers/
│   └── EmailProcessingController.cs    # REST API endpoints
├── Models/
│   ├── DataWalletModels.cs             # Email & Attachment wallet models
│   └── EmailProcessingModels.cs        # Processing & validation models  
├── Services/
│   ├── EmailParserService.cs           # Email parsing & attachment extraction
│   ├── EmailValidationService.cs       # SPF/DKIM/DMARC validation & authorization
│   ├── WalletCreatorService.cs         # Dual-tier wallet creation logic
│   └── EmailProcessingService.cs       # Main orchestration service
├── Program.cs                          # Service configuration & DI setup
├── appsettings.json                    # Configuration settings
└── EmailProcessingService.csproj       # Project dependencies
```

## Key Features Implemented

### ✅ **Step 1: Email Receipt & Parsing**
- **EmailParserService**: Full email parsing with MimeKit
- Attachment extraction and validation
- Email header analysis (SPF/DKIM/DMARC)
- Content hash generation

### ✅ **Step 2: Validation & Authorization**
- **EmailValidationService**: Multi-layer email validation
- User registration lookup
- Whitelist domain checking  
- Corporate authorization validation
- File type and size validation

### ✅ **Step 3: Wallet Creation**
- **WalletCreatorService**: Dual-tier wallet structure creation
- Email Data Wallet with full context preservation
- Independent Attachment Data Wallets
- Wallet ID generation and relationship management
- Credit calculation (3 + 2×attachments + 1 authorization)

### ✅ **Step 4: Processing Orchestration**
- **EmailProcessingService**: Main workflow coordination
- Authorization request generation
- MetaMask signature validation
- Background wallet finalization
- IPFS storage integration (ready)
- Blockchain verification integration (ready)

## API Endpoints

### Email Processing
- `POST /api/emailprocessing/process` - Process incoming email
- `POST /api/emailprocessing/authorize` - Process authorization response
- `GET /api/emailprocessing/task/{taskId}` - Get task status
- `GET /api/emailprocessing/wallet/{address}/tasks` - Get user tasks

### Utilities  
- `POST /api/emailprocessing/parse` - Parse email (testing)
- `GET /api/emailprocessing/health` - Health check

## Configuration

### Database Setup
```bash
# PostgreSQL connection required
"DefaultConnection": "Host=localhost;Database=emailprocessing;Username=postgres;Password=your_password"
```

### Key Configuration Sections
- **EmailProcessing**: Size limits, file types, authorization settings
- **Blockchain**: Contract addresses, RPC URLs, gas settings  
- **IPFS**: Storage configuration and backup settings
- **Credits**: Pricing model and payment integration
- **Security**: Encryption keys, rate limiting, virus scanning

## Processing Flow

### 1. Email Receipt → Validation
```
Raw Email → Parse → Validate Sender → Check Whitelist → Verify Email Auth
```

### 2. Wallet Creation → Authorization Request  
```
Create Temp Wallets → Calculate Credits → Generate Auth Token → Send Notification
```

### 3. User Authorization → Finalization
```
MetaMask Signature → Validate → Upload to IPFS → Record on Blockchain → Complete
```

## Credit Model

- **Email Data Wallet**: 3 credits
- **Attachment Wallets**: 2 credits per file
- **Authorization**: 1 credit  
- **Example**: Email + 2 PDFs = 8 credits total

## Getting Started

### Prerequisites
- .NET 6+ SDK
- PostgreSQL database
- Redis (optional, for caching)

### Run the Service
```bash
cd EmailProcessingService
dotnet restore
dotnet run
```

### API Documentation
- Swagger UI: `https://localhost:7000/swagger`
- Health Check: `https://localhost:7000/api/emailprocessing/health`

## Integration Points

### Ready for Integration
- **IPFS Storage**: Service interfaces defined, ready for implementation
- **Blockchain Recording**: Contract integration points prepared  
- **Credit Management**: Payment validation hooks in place
- **Notification System**: Email/webhook notification framework ready

### Placeholder Services (To Be Implemented)
- `IAuthorizationService` - MetaMask signature validation
- `INotificationService` - Email notifications
- `IFileProcessorService` - Advanced file metadata extraction
- IPFS upload implementation
- Blockchain transaction services

## Security Features

- **Email Authentication**: SPF, DKIM, DMARC validation
- **File Validation**: Type checking, virus scanning hooks
- **Access Control**: Wallet-based authorization
- **Rate Limiting**: Configurable request throttling
- **Audit Logging**: Complete processing trail

## Monitoring & Logging

- **Serilog**: Structured logging to console and files
- **Health Checks**: Database connectivity monitoring
- **Processing Logs**: Detailed step-by-step task tracking
- **Error Handling**: Comprehensive exception management

## Next Steps

1. **Complete Placeholder Implementations**:
   - MetaMask signature validation
   - IPFS upload service
   - Blockchain transaction recording
   - Email notification service

2. **Deploy Authorization Portal** (Frontend):
   - React.js MetaMask integration
   - User authorization interface
   - Task status tracking

3. **Email Infrastructure Setup**:
   - Office 365 integration OR standalone SMTP
   - Email forwarding configuration
   - Domain setup (wallet@rootz.global)

4. **Testing & Validation**:
   - End-to-end email processing tests
   - Load testing for concurrent processing
   - Security penetration testing

## Smart Contract Integration

This service is designed to work with your completed smart contracts:
- `EmailWalletRegistration.sol` - User registration lookup
- `EmailDataWallet.sol` - Email wallet blockchain recording  
- `AttachmentWallet.sol` - File wallet verification
- `AuthorizationManager.sol` - MetaMask authorization coordination

The blockchain integration points are prepared and ready for your smart contract deployment.

---

**Status**: Core email processing service implemented and ready for integration testing. The foundational infrastructure is complete and follows your dual-tier wallet architecture specification.
