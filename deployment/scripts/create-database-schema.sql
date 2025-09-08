-- Email Wallet Service Database Schema
-- PostgreSQL Database Setup Script
-- Run after creating the database and user

\c emailwalletdb;

-- Create tables for the Email Wallet Service
-- Based on the Entity Framework models in the service

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
    ErrorMessage TEXT,
    
    FOREIGN KEY (UserWalletAddress) REFERENCES UserRegistrations(WalletAddress)
);

-- Email Data Wallets table
CREATE TABLE IF NOT EXISTS EmailDataWallets (
    Id SERIAL PRIMARY KEY,
    WalletId VARCHAR(100) NOT NULL UNIQUE,
    OwnerAddress VARCHAR(42) NOT NULL,
    EmailMessageId VARCHAR(255),
    Subject TEXT,
    Sender VARCHAR(255),
    Recipients TEXT, -- JSON array as text
    Timestamp TIMESTAMP,
    ContentHash VARCHAR(66), -- IPFS hash
    IpfsHash VARCHAR(66),
    BlockchainTxHash VARCHAR(66),
    Status VARCHAR(50) DEFAULT 'active',
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (OwnerAddress) REFERENCES UserRegistrations(WalletAddress)
);

-- Attachment Wallets table
CREATE TABLE IF NOT EXISTS AttachmentWallets (
    Id SERIAL PRIMARY KEY,
    WalletId VARCHAR(100) NOT NULL UNIQUE,
    ParentEmailWalletId VARCHAR(100) NOT NULL,
    FileName VARCHAR(255),
    ContentType VARCHAR(100),
    FileSize BIGINT,
    ContentHash VARCHAR(66),
    IpfsHash VARCHAR(66),
    BlockchainTxHash VARCHAR(66),
    Status VARCHAR(50) DEFAULT 'active',
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (ParentEmailWalletId) REFERENCES EmailDataWallets(WalletId)
);

-- Authorization Requests table
CREATE TABLE IF NOT EXISTS AuthorizationRequests (
    Id SERIAL PRIMARY KEY,
    RequestId VARCHAR(66) NOT NULL UNIQUE,
    UserAddress VARCHAR(42) NOT NULL,
    EmailHash VARCHAR(66),
    AuthToken VARCHAR(255),
    CreditCost INTEGER DEFAULT 0,
    Status INTEGER DEFAULT 0, -- 0=pending, 1=authorized, 2=rejected, 3=expired
    ExpiresAt TIMESTAMP,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    AuthorizedAt TIMESTAMP,
    ProcessedAt TIMESTAMP,
    
    FOREIGN KEY (UserAddress) REFERENCES UserRegistrations(WalletAddress)
);

-- IPFS Uploads tracking table
CREATE TABLE IF NOT EXISTS IpfsUploads (
    Id SERIAL PRIMARY KEY,
    IpfsHash VARCHAR(66) NOT NULL UNIQUE,
    OriginalFileName VARCHAR(255),
    ContentType VARCHAR(100),
    FileSize BIGINT,
    UploadedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UploadedBy VARCHAR(42), -- Wallet address
    Metadata TEXT, -- JSON metadata
    PinStatus VARCHAR(50) DEFAULT 'pinned',
    
    FOREIGN KEY (UploadedBy) REFERENCES UserRegistrations(WalletAddress)
);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_user_registrations_wallet ON UserRegistrations(WalletAddress);
CREATE INDEX IF NOT EXISTS idx_user_registrations_email ON UserRegistrations(Email);
CREATE INDEX IF NOT EXISTS idx_user_registrations_active ON UserRegistrations(IsActive, RegistrationDate);

CREATE INDEX IF NOT EXISTS idx_email_tasks_user ON EmailProcessingTasks(UserWalletAddress);
CREATE INDEX IF NOT EXISTS idx_email_tasks_status ON EmailProcessingTasks(Status);
CREATE INDEX IF NOT EXISTS idx_email_tasks_created ON EmailProcessingTasks(CreatedAt);

CREATE INDEX IF NOT EXISTS idx_email_wallets_owner ON EmailDataWallets(OwnerAddress);
CREATE INDEX IF NOT EXISTS idx_email_wallets_status ON EmailDataWallets(Status);
CREATE INDEX IF NOT EXISTS idx_email_wallets_created ON EmailDataWallets(CreatedAt);

CREATE INDEX IF NOT EXISTS idx_attachment_wallets_parent ON AttachmentWallets(ParentEmailWalletId);
CREATE INDEX IF NOT EXISTS idx_attachment_wallets_status ON AttachmentWallets(Status);

CREATE INDEX IF NOT EXISTS idx_auth_requests_user ON AuthorizationRequests(UserAddress);
CREATE INDEX IF NOT EXISTS idx_auth_requests_status ON AuthorizationRequests(Status);
CREATE INDEX IF NOT EXISTS idx_auth_requests_expires ON AuthorizationRequests(ExpiresAt);

CREATE INDEX IF NOT EXISTS idx_ipfs_uploads_hash ON IpfsUploads(IpfsHash);
CREATE INDEX IF NOT EXISTS idx_ipfs_uploads_user ON IpfsUploads(UploadedBy);
CREATE INDEX IF NOT EXISTS idx_ipfs_uploads_date ON IpfsUploads(UploadedAt);

-- Insert sample test data (optional)
INSERT INTO UserRegistrations (WalletAddress, Email, Credits) VALUES 
    ('0x107C5655ce50AB9744Fc36A4e9935E30d4923d0b', 'test@example.com', 100)
ON CONFLICT (WalletAddress) DO NOTHING;

-- Grant all permissions to the service user
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO emailwalletuser;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO emailwalletuser;

-- Show created tables
\dt

-- Show table sizes
SELECT 
    schemaname,
    tablename,
    attname,
    n_distinct,
    most_common_vals
FROM pg_stats 
WHERE schemaname = 'public'
ORDER BY tablename, attname;

-- Database setup complete
SELECT 'Database schema created successfully!' as status;
