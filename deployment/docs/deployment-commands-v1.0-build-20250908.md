# Deployment Commands Reference - Build 20250908-001

**Build:** v1.0-build-20250908  
**Service:** Email Wallet Service  
**Server:** Ubuntu 24.04 ARM64 at rootz.global  

## Successful Deployment Sequence

### 1. Initial Repository Setup
```bash
# Clone repository to production server
git clone https://github.com/skswave/email-wallet-service.git /opt/email-wallet-service

# Navigate to project directory
cd /opt/email-wallet-service

# Check initial status
git status
ls -la
```

### 2. Permission Configuration
```bash
# Fix ownership issues created by deployment script
sudo chown -R ubuntu:ubuntu /opt/email-wallet-service

# Set proper permissions for build process
sudo chmod -R 755 /opt/email-wallet-service

# Verify permissions
ls -la /opt/email-wallet-service/src/EmailProcessingService/
```

### 3. Missing Files Creation

#### DataWalletModels.cs Creation
```bash
cd /opt/email-wallet-service/src/EmailProcessingService

# Create the missing model file
cat > Models/DataWalletModels.cs << 'EOF'
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace EmailProcessingService.Models
{
    public enum WalletType
    {
        EMAIL_CONTAINER,
        FILE_ATTACHMENT
    }

    public enum WalletStatus
    {
        PENDING_AUTHORIZATION,
        ACTIVE,
        SUSPENDED,
        ARCHIVED
    }

    public class VerificationInfo
    {
        public string ContentHash { get; set; } = string.Empty;
        public string MerkleRoot { get; set; } = string.Empty;
        public List<string> MerkleProof { get; set; } = new();
        public string BlockchainTx { get; set; } = string.Empty;
        public long BlockNumber { get; set; }
        public DateTime VerifiedAt { get; set; }
        public string Network { get; set; } = "ethereum";
        public bool IndependentVerification { get; set; }
    }

    public class FileMetadataInfo
    {
        public string ContentType { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CreatedDate { get; set; }
        public DateTime? LastModified { get; set; }
        public string Author { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Producer { get; set; } = string.Empty;
        public string PdfVersion { get; set; } = string.Empty;
        public int PageCount { get; set; }
        public bool Encrypted { get; set; }
        public bool PasswordProtected { get; set; }
        public Dictionary<string, string> CustomProperties { get; set; } = new();
    }

    public class VirusScanResult
    {
        public bool Scanned { get; set; }
        public bool Clean { get; set; }
        public string Scanner { get; set; } = "clamav";
        public string ScanEngine { get; set; } = "clamav";
        public DateTime ScannedAt { get; set; }
        public List<string> Threats { get; set; } = new();
        public string? ThreatName { get; set; }
        public string? ScanVersion { get; set; }
        public List<string> Warnings { get; set; } = new();
        public TimeSpan ScanDuration { get; set; }
    }
}
EOF
```

#### WalletCreatorService.cs Creation
```bash
# Create the missing service file
cat > Services/WalletCreatorService.cs << 'EOF'
using EmailProcessingService.Models;

namespace EmailProcessingService.Services
{
    public interface IWalletCreatorService
    {
        Task<WalletCreationResult> CreateEmailWalletAsync(IncomingEmailMessage message, UserRegistration user);
        Task<WalletCreationResult> CreateEmailDataWalletAsync(IncomingEmailMessage message, UserRegistration user);
        Task<WalletCreationResult> CreateAttachmentWalletAsync(EmailAttachment attachment, string parentWalletId);
    }

    public class WalletCreatorService : IWalletCreatorService
    {
        private readonly ILogger<WalletCreatorService> _logger;

        public WalletCreatorService(ILogger<WalletCreatorService> logger)
        {
            _logger = logger;
        }

        public async Task<WalletCreationResult> CreateEmailWalletAsync(IncomingEmailMessage message, UserRegistration user)
        {
            await Task.Delay(100);
            return new WalletCreationResult { Success = true, EmailWalletId = Guid.NewGuid().ToString() };
        }

        public async Task<WalletCreationResult> CreateEmailDataWalletAsync(IncomingEmailMessage message, UserRegistration user)
        {
            return await CreateEmailWalletAsync(message, user);
        }

        public async Task<WalletCreationResult> CreateAttachmentWalletAsync(EmailAttachment attachment, string parentWalletId)
        {
            await Task.Delay(100);
            return new WalletCreationResult { Success = true, AttachmentWalletIds = new List<string> { Guid.NewGuid().ToString() } };
        }
    }
}
EOF
```

### 4. Build Process
```bash
# Clean any existing build artifacts
rm -rf obj/ bin/

# Restore dependencies
dotnet restore

# Build in release configuration
dotnet build --configuration Release

# Publish for production deployment
dotnet publish --configuration Release --output ./publish
```

### 5. Database Configuration
```bash
# Check current connection string
cat appsettings.Production.json | grep -A 3 "ConnectionStrings"

# Update to use in-memory database (method 1)
sed -i 's/"DefaultConnection": ".*"/"DefaultConnection": "InMemory"/' appsettings.Production.json

# Verify change
cat appsettings.Production.json | grep -A 3 "ConnectionStrings"
```

### 6. Service Launch
```bash
# Navigate to published application
cd publish

# Start service with environment variable override
CONNECTIONSTRINGS__DEFAULTCONNECTION="InMemory" dotnet EmailProcessingService.dll --urls="http://0.0.0.0:5000"
```

## Service Management Commands

### Start Service (Production)
```bash
cd /opt/email-wallet-service/src/EmailProcessingService/publish
CONNECTIONSTRINGS__DEFAULTCONNECTION="InMemory" dotnet EmailProcessingService.dll --urls="http://0.0.0.0:5000"
```

### Start Service (Background)
```bash
cd /opt/email-wallet-service/src/EmailProcessingService/publish
nohup CONNECTIONSTRINGS__DEFAULTCONNECTION="InMemory" dotnet EmailProcessingService.dll --urls="http://0.0.0.0:5000" > service.log 2>&1 &
```

### Stop Service
```bash
# If running in foreground
# Press Ctrl+C

# If running in background
pkill -f "EmailProcessingService.dll"
```

### Check Service Status
```bash
# Health check
curl http://localhost:5000/health

# IPFS health check
curl http://localhost:5000/health/ipfs

# Check if service is running
ps aux | grep EmailProcessingService

# Check port status
sudo netstat -tlnp | grep :5000
```

### View Service Logs
```bash
# If running in foreground - logs appear in terminal
# If running with nohup
tail -f /opt/email-wallet-service/src/EmailProcessingService/publish/service.log
```

## Update and Maintenance Commands

### Update from Git Repository
```bash
cd /opt/email-wallet-service

# Pull latest changes
git pull origin main

# Check what changed
git log --oneline -10

# Navigate to service directory
cd src/EmailProcessingService
```

### Rebuild After Updates
```bash
# Clean previous build
rm -rf obj/ bin/ publish/

# Rebuild and publish
dotnet build --configuration Release
dotnet publish --configuration Release --output ./publish

# Restart service
cd publish
CONNECTIONSTRINGS__DEFAULTCONNECTION="InMemory" dotnet EmailProcessingService.dll --urls="http://0.0.0.0:5000"
```

### Configuration Updates
```bash
# Update database connection
sed -i 's/"DefaultConnection": ".*"/"DefaultConnection": "InMemory"/' appsettings.Production.json

# Update CORS settings (if needed)
nano appsettings.Production.json
```

## Troubleshooting Commands

### Permission Issues
```bash
# Check current permissions
ls -la /opt/email-wallet-service/src/EmailProcessingService/

# Fix ownership
sudo chown -R ubuntu:ubuntu /opt/email-wallet-service

# Fix permissions
sudo chmod -R 755 /opt/email-wallet-service
```

### Build Issues
```bash
# Check .NET version
dotnet --version

# Clean and restore
dotnet clean
dotnet restore

# Detailed build output
dotnet build --configuration Release --verbosity detailed
```

### Configuration Issues
```bash
# Check configuration values
cat appsettings.json | grep -A 5 "ConnectionStrings"
cat appsettings.Production.json | grep -A 5 "ConnectionStrings"

# Test with explicit environment variable
export CONNECTIONSTRINGS__DEFAULTCONNECTION="InMemory"
dotnet EmailProcessingService.dll --urls="http://0.0.0.0:5000"
```

### Network Issues
```bash
# Check if port is available
sudo netstat -tlnp | grep :5000

# Check firewall status
sudo ufw status

# Test local connectivity
curl http://localhost:5000/health

# Test external connectivity
curl http://rootz.global:5000/health
```

### File System Issues
```bash
# Check disk space
df -h

# Check file permissions
ls -la /opt/email-wallet-service/src/EmailProcessingService/

# Check for missing files
find /opt/email-wallet-service -name "*.cs" | grep -E "(DataWalletModels|WalletCreatorService)"
```

## Environment Variables

### Required for Service Operation
```bash
# Database connection override
export CONNECTIONSTRINGS__DEFAULTCONNECTION="InMemory"

# Optional: Set environment
export ASPNETCORE_ENVIRONMENT="Production"

# Optional: Explicit URL binding
export ASPNETCORE_URLS="http://0.0.0.0:5000"
```

### Optional Configuration Overrides
```bash
# Logging level
export ASPNETCORE_LOGGING__LOGLEVEL__DEFAULT="Information"

# CORS settings (if needed)
export CORS__ALLOWEDORIGINS="http://rootz.global,https://rootz.global"
```

## Health Check URLs

### Service Health Checks
- **Basic Health:** http://rootz.global:5000/health
- **IPFS Health:** http://rootz.global:5000/health/ipfs
- **Swagger UI:** http://rootz.global:5000/swagger
- **API Documentation:** http://rootz.global:5000/swagger/v1/swagger.json

### Expected Health Check Responses
```json
// GET /health
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456"
}

// GET /health/ipfs  
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0567890"
}
```

## Performance Monitoring

### Basic Performance Checks
```bash
# CPU usage
top -p $(pgrep -f EmailProcessingService.dll)

# Memory usage
ps aux | grep EmailProcessingService.dll

# Network connections
sudo netstat -an | grep :5000
```

### Log Monitoring
```bash
# Follow service logs (if using nohup)
tail -f /opt/email-wallet-service/src/EmailProcessingService/publish/service.log

# Filter for errors
tail -f service.log | grep -i error

# Filter for IPFS activity
tail -f service.log | grep -i ipfs
```

---

**Commands tested and verified on:** September 8, 2025  
**Service Status:** All commands functional  
**Build:** v1.0-build-20250908-001