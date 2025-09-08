#!/bin/bash
# PostgreSQL Setup Script for Email Wallet Service
# Run on Ubuntu server at rootz.global

echo "🗄️ Setting up PostgreSQL for Email Wallet Service"
echo "=================================================="

# Update package list
echo "📦 Updating package list..."
sudo apt update

# Install PostgreSQL and additional tools
echo "🔧 Installing PostgreSQL..."
sudo apt install -y postgresql postgresql-contrib

# Start and enable PostgreSQL service
echo "🚀 Starting PostgreSQL service..."
sudo systemctl start postgresql
sudo systemctl enable postgresql

# Check PostgreSQL status
echo "📊 PostgreSQL service status:"
sudo systemctl status postgresql --no-pager

# Create database and user for email wallet service
echo "👤 Creating database and user..."
sudo -u postgres psql << 'EOF'
-- Create database
CREATE DATABASE emailwalletdb;

-- Create user with password
CREATE USER emailwalletuser WITH PASSWORD 'SecurePassword2025!';

-- Grant privileges
GRANT ALL PRIVILEGES ON DATABASE emailwalletdb TO emailwalletuser;

-- Grant schema privileges
\c emailwalletdb;
GRANT ALL ON SCHEMA public TO emailwalletuser;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO emailwalletuser;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO emailwalletuser;

-- Show created database and user
\l
\du

-- Exit
\q
EOF

echo "✅ PostgreSQL setup complete!"
echo ""
echo "Database Details:"
echo "  Database: emailwalletdb"
echo "  User: emailwalletuser"
echo "  Password: SecurePassword2025!"
echo "  Host: localhost"
echo "  Port: 5432"
echo ""
echo "Connection String:"
echo "  Host=localhost;Database=emailwalletdb;Username=emailwalletuser;Password=SecurePassword2025!"
