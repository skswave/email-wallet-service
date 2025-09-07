using System.ComponentModel.DataAnnotations;

namespace EmailProcessingService.Models
{
    // Blockchain Configuration Models
    public class BlockchainConfiguration
    {
        [Required]
        public string Network { get; set; } = "amoy";
        
        [Required]
        public int ChainId { get; set; } = 80002;
        
        [Required]
        public string RpcUrl { get; set; } = string.Empty;
        
        public string EthereumRpcUrl { get; set; } = string.Empty;
        
        public string PolygonRpcUrl { get; set; } = string.Empty;
        
        [Required]
        public ContractAddresses ContractAddresses { get; set; } = new();
        
        public string RegistrationFee { get; set; } = "0.01";
        
        public string RegistrationFeeWei { get; set; } = "10000000000000000";
        
        public double GasLimitMultiplier { get; set; } = 1.2;
        
        public string MaxGasPrice { get; set; } = "50000000000";
        
        public int ConfirmationBlocks { get; set; } = 3;
        
        public string ExplorerUrl { get; set; } = string.Empty;
        
        public bool EnableRealTransactions { get; set; } = false;
        
        public TestWalletConfiguration? TestWallet { get; set; }
    }

    public class TestWalletConfiguration
    {
        public string PrivateKey { get; set; } = string.Empty;
        
        public string Address { get; set; } = string.Empty;
    }

    public class ContractAddresses
    {
        [Required]
        public string EmailWalletRegistration { get; set; } = string.Empty;
        
        [Required]
        public string EmailDataWallet { get; set; } = string.Empty;
        
        [Required]
        public string AttachmentWallet { get; set; } = string.Empty;
        
        [Required]
        public string AuthorizationManager { get; set; } = string.Empty;
    }

    // Blockchain Transaction Models
    public class BlockchainTransaction
    {
        public string TransactionHash { get; set; } = string.Empty;
        
        public string From { get; set; } = string.Empty;
        
        public string To { get; set; } = string.Empty;
        
        public string Value { get; set; } = "0";
        
        public long Gas { get; set; }
        
        public string GasPrice { get; set; } = string.Empty;
        
        public long BlockNumber { get; set; }
        
        public DateTime Timestamp { get; set; }
        
        public TransactionStatus Status { get; set; }
        
        public string? ErrorMessage { get; set; }
    }

    public enum TransactionStatus
    {
        Pending,
        Confirmed,
        Failed
    }

    // Smart Contract Interaction Models
    public class ContractCallResult
    {
        public bool Success { get; set; }
        
        public object? Result { get; set; }
        
        public string? ErrorMessage { get; set; }
        
        public long GasUsed { get; set; }
        
        public string TransactionHash { get; set; } = string.Empty;
    }

    public class WalletRegistrationRequest
    {
        [Required]
        public string WalletAddress { get; set; } = string.Empty;
        
        [Required]
        public string PrimaryEmail { get; set; } = string.Empty;
        
        public List<string> AdditionalEmails { get; set; } = new();
        
        public string? ParentCorporateWallet { get; set; }
        
        public List<string> AuthorizationTxs { get; set; } = new();
        
        public List<string> WhitelistedDomains { get; set; } = new();
        
        public bool AutoProcessCC { get; set; } = false;
        
        public string RegistrationFee { get; set; } = "0.01";
    }

    public class WalletRegistrationResult
    {
        public bool Success { get; set; }
        
        public string RegistrationId { get; set; } = string.Empty;
        
        public string TransactionHash { get; set; } = string.Empty;
        
        public string? ErrorMessage { get; set; }
        
        public DateTime RegisteredAt { get; set; }
        
        public long GasUsed { get; set; }
    }

    // Email Data Storage Models
    public class EmailDataStorageRequest
    {
        [Required]
        public string WalletAddress { get; set; } = string.Empty;
        
        [Required]
        public string EmailWalletId { get; set; } = string.Empty;
        
        [Required]
        public string DataHash { get; set; } = string.Empty;
        
        [Required]
        public string IpfsHash { get; set; } = string.Empty;
        
        public string MetadataHash { get; set; } = string.Empty;
        
        public long DataSize { get; set; }
        
        public List<string> AttachmentWalletIds { get; set; } = new();
    }

    public class AttachmentStorageRequest
    {
        [Required]
        public string WalletAddress { get; set; } = string.Empty;
        
        [Required]
        public string AttachmentWalletId { get; set; } = string.Empty;
        
        [Required]
        public string FileHash { get; set; } = string.Empty;
        
        [Required]
        public string IpfsHash { get; set; } = string.Empty;
        
        public string FileName { get; set; } = string.Empty;
        
        public string FileType { get; set; } = string.Empty;
        
        public long FileSize { get; set; }
        
        public string ParentEmailWalletId { get; set; } = string.Empty;
    }

    // Authorization Models
    public class BlockchainAuthorizationRequest
    {
        [Required]
        public string RequesterWallet { get; set; } = string.Empty;
        
        [Required]
        public string TargetWallet { get; set; } = string.Empty;
        
        [Required]
        public string ResourceId { get; set; } = string.Empty;
        
        [Required]
        public AuthorizationType AuthorizationType { get; set; }
        
        public string Reason { get; set; } = string.Empty;
        
        public DateTime RequestedAt { get; set; }
        
        public DateTime? ExpiresAt { get; set; }
    }

    public enum AuthorizationType
    {
        EmailAccess,
        AttachmentAccess,
        CorporateAccess,
        TemporaryAccess
    }

    public class AuthorizationResponse
    {
        public bool Authorized { get; set; }
        
        public string AuthorizationId { get; set; } = string.Empty;
        
        public string TransactionHash { get; set; } = string.Empty;
        
        public DateTime AuthorizedAt { get; set; }
        
        public DateTime? ExpiresAt { get; set; }
        
        public string? Signature { get; set; }
        
        public string? ErrorMessage { get; set; }
    }

    // Credit System Models
    public class CreditOperation
    {
        [Required]
        public string WalletAddress { get; set; } = string.Empty;
        
        public long Amount { get; set; }
        
        public CreditOperationType OperationType { get; set; }
        
        public string Reason { get; set; } = string.Empty;
        
        public string? RelatedTransactionId { get; set; }
    }

    public enum CreditOperationType
    {
        Deposit,
        Deduct,
        Refund
    }

    public class CreditBalance
    {
        public string WalletAddress { get; set; } = string.Empty;
        
        public long Balance { get; set; }
        
        public decimal NativeBalance { get; set; }
        
        public DateTime LastUpdated { get; set; }
        
        public List<CreditTransaction> RecentTransactions { get; set; } = new();
    }

    public class CreditTransaction
    {
        public string TransactionId { get; set; } = string.Empty;
        
        public CreditOperationType Type { get; set; }
        
        public long Amount { get; set; }
        
        public long BalanceAfter { get; set; }
        
        public string Reason { get; set; } = string.Empty;
        
        public DateTime Timestamp { get; set; }
        
        public string BlockchainTxHash { get; set; } = string.Empty;
    }

    // Verification Models
    public class DataVerificationRequest
    {
        [Required]
        public string WalletId { get; set; } = string.Empty;
        
        [Required]
        public string DataHash { get; set; } = string.Empty;
        
        public string? IpfsHash { get; set; }
        
        public WalletType WalletType { get; set; }
    }

    public class DataVerificationResult
    {
        public bool IsValid { get; set; }
        
        public bool OnChainMatch { get; set; }
        
        public bool IpfsMatch { get; set; }
        
        public string StoredHash { get; set; } = string.Empty;
        
        public string ComputedHash { get; set; } = string.Empty;
        
        public DateTime VerifiedAt { get; set; }
        
        public string TransactionHash { get; set; } = string.Empty;
        
        public long BlockNumber { get; set; }
        
        public string? ErrorMessage { get; set; }
    }

    // Event Models for Blockchain Events
    public class BlockchainEvent
    {
        public string EventName { get; set; } = string.Empty;
        
        public string ContractAddress { get; set; } = string.Empty;
        
        public string TransactionHash { get; set; } = string.Empty;
        
        public long BlockNumber { get; set; }
        
        public DateTime Timestamp { get; set; }
        
        public Dictionary<string, object> EventData { get; set; } = new();
    }

    public class EmailWalletRegisteredEvent : BlockchainEvent
    {
        public string WalletAddress { get; set; } = string.Empty;
        
        public string RegistrationId { get; set; } = string.Empty;
        
        public string PrimaryEmailHash { get; set; } = string.Empty;
        
        public string? ParentWallet { get; set; }
    }

    public class EmailDataStoredEvent : BlockchainEvent
    {
        public string WalletAddress { get; set; } = string.Empty;
        
        public string EmailWalletId { get; set; } = string.Empty;
        
        public string DataHash { get; set; } = string.Empty;
        
        public string IpfsHash { get; set; } = string.Empty;
    }

    public class AttachmentStoredEvent : BlockchainEvent
    {
        public string WalletAddress { get; set; } = string.Empty;
        
        public string AttachmentWalletId { get; set; } = string.Empty;
        
        public string FileHash { get; set; } = string.Empty;
        
        public string ParentEmailWalletId { get; set; } = string.Empty;
    }
}
