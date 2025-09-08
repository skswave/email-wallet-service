# Database Setup - Quick Reference Guide

## 🗄️ PostgreSQL Database Setup for Email Wallet Service

### One-Command Setup
```bash
# On Ubuntu server, run this single command:
sudo bash /opt/email-wallet-service/deployment/scripts/master-database-setup.sh
```

### Manual Step-by-Step Setup

#### 1. Download Scripts to Server
```bash
# If using git deployment
cd /opt/email-wallet-service
git pull origin main

# Or manually copy scripts to server
```

#### 2. Run Database Setup
```bash
# Make script executable
sudo chmod +x /opt/email-wallet-service/deployment/scripts/master-database-setup.sh

# Run the setup
sudo /opt/email-wallet-service/deployment/scripts/master-database-setup.sh
```

#### 3. Start the Service
```bash
# Start the service
email-wallet-ctl start

# Check status
email-wallet-ctl status

# View logs
email-wallet-ctl logs
```

## 📋 What Gets Installed

### PostgreSQL Database
- **Database Name:** `emailwalletdb`
- **Username:** `emailwalletuser`
- **Password:** `SecurePassword2025!`
- **Connection:** `Host=localhost;Database=emailwalletdb;Username=emailwalletuser;Password=SecurePassword2025!`

### Database Tables Created
- **UserRegistrations** - User wallet addresses and credits
- **EmailProcessingTasks** - Email processing queue
- **EmailDataWallets** - Created email wallets
- **AuthorizationRequests** - Pending wallet authorizations

### Systemd Service
- **Service Name:** `email-wallet-service`
- **Auto-start:** Enabled (starts on boot)
- **User:** `ubuntu`
- **Working Directory:** `/opt/email-wallet-service/src/EmailProcessingService/publish`

## 🛠️ Service Management Commands

### Using the Control Script
```bash
email-wallet-ctl start      # Start service
email-wallet-ctl stop       # Stop service
email-wallet-ctl restart    # Restart service
email-wallet-ctl status     # Check status
email-wallet-ctl logs       # Follow logs
email-wallet-ctl health     # Test health endpoint
```

### Using Systemctl Directly
```bash
sudo systemctl start email-wallet-service
sudo systemctl stop email-wallet-service
sudo systemctl restart email-wallet-service
sudo systemctl status email-wallet-service
sudo journalctl -u email-wallet-service -f
```

## 🔍 Testing the Setup

### Health Check Endpoints
```bash
# Basic health check
curl http://rootz.global:5000/health

# IPFS health check
curl http://rootz.global:5000/health/ipfs

# Swagger documentation
curl http://rootz.global:5000/swagger
```

### Database Connection Test
```bash
# Test PostgreSQL connection
PGPASSWORD='SecurePassword2025!' psql -h localhost -U emailwalletuser -d emailwalletdb -c "SELECT version();"

# Check tables were created
PGPASSWORD='SecurePassword2025!' psql -h localhost -U emailwalletuser -d emailwalletdb -c "\dt"
```

### Service API Test
```bash
# Test user registration endpoint
curl -X POST "http://rootz.global:5000/api/users/register" \
  -H "Content-Type: application/json" \
  -d '{"walletAddress": "0x1234567890123456789012345678901234567890", "email": "test@example.com"}'

# Test blockchain status
curl "http://rootz.global:5000/api/blockchain/status"
```

## 📊 Configuration Details

### Database Configuration Updated
The service configuration (`appsettings.Production.json`) is updated with:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=emailwalletdb;Username=emailwalletuser;Password=SecurePassword2025!"
  }
}
```

### Service URLs
- **API Base:** http://rootz.global:5000
- **Health Check:** http://rootz.global:5000/health
- **IPFS Health:** http://rootz.global:5000/health/ipfs
- **Swagger UI:** http://rootz.global:5000/swagger
- **API Documentation:** http://rootz.global:5000/swagger/v1/swagger.json

## 🚨 Troubleshooting

### Service Won't Start
```bash
# Check service status
sudo systemctl status email-wallet-service

# Check service logs
sudo journalctl -u email-wallet-service -n 50

# Check configuration
cat /opt/email-wallet-service/src/EmailProcessingService/appsettings.Production.json
```

### Database Connection Issues
```bash
# Test PostgreSQL is running
sudo systemctl status postgresql

# Test database connection
PGPASSWORD='SecurePassword2025!' psql -h localhost -U emailwalletuser -d emailwalletdb -c "SELECT 1;"

# Check PostgreSQL logs
sudo journalctl -u postgresql -n 50
```

### Permission Issues
```bash
# Fix service file permissions
sudo chown -R ubuntu:ubuntu /opt/email-wallet-service

# Check service file exists
ls -la /opt/email-wallet-service/src/EmailProcessingService/publish/EmailProcessingService.dll
```

## 🔄 Migration from In-Memory Database

If you're currently running with in-memory database, this setup will:

1. **Preserve existing functionality** - All APIs continue to work
2. **Migrate to persistent storage** - Data will survive service restarts
3. **Maintain compatibility** - Same API endpoints and responses
4. **Add data persistence** - User registrations, wallets, and tasks are saved

### Migration Process
1. Stop current service
2. Run database setup script
3. Start service with new PostgreSQL configuration
4. Verify all endpoints still work
5. Test data persistence across restarts

## 📈 Performance Benefits

### With PostgreSQL vs In-Memory
- **Data Persistence** - Survives service restarts and server reboots
- **Better Performance** - Optimized queries with indexes
- **Concurrent Access** - Multiple service instances can share database
- **Data Integrity** - ACID transactions and referential integrity
- **Monitoring** - Database query logs and performance metrics
- **Backup/Recovery** - Standard PostgreSQL backup procedures

---

**Setup Time:** ~10 minutes  
**Compatibility:** Ubuntu 20.04+ with .NET 8  
**Database:** PostgreSQL 12+  
**Status:** Production Ready  

Run the master setup script and your Email Wallet Service will be running with PostgreSQL in under 10 minutes!
