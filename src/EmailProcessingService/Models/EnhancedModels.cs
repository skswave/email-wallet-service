using System.Text.Json.Serialization;

namespace EmailProcessingService.Models
{
    // Main email data structure for IPFS storage
    public class EmailDataPackage
    {
        public EmailMetadata Metadata { get; set; } = new();
        public EmailContent Content { get; set; } = new();
        public List<AttachmentReference> Attachments { get; set; } = new();
        public ValidationResults Validation { get; set; } = new();
        public ProcessingInfo Processing { get; set; } = new();
    }

    public class EmailMetadata
    {
        public string MessageId { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
        public DateTime SentAt { get; set; }
        public string Subject { get; set; } = string.Empty;
        public EmailAddress From { get; set; } = new();
        public List<EmailAddress> To { get; set; } = new();
        public List<EmailAddress> Cc { get; set; } = new();
        public List<EmailAddress> Bcc { get; set; } = new();
        public string Priority { get; set; } = "Normal";
        public Dictionary<string, string> Headers { get; set; } = new();
    }

    public class EmailAddress
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        
        public override string ToString()
        {
            return string.IsNullOrEmpty(Name) ? Address : $"{Name} <{Address}>";
        }
    }

    public class EmailContent
    {
        public string? TextBody { get; set; }
        public string? HtmlBody { get; set; }
        public string ContentHash { get; set; } = string.Empty;
        public string ContentType { get; set; } = "text/plain";
        public string Encoding { get; set; } = "UTF-8";
        public long SizeBytes { get; set; }
    }

    public class AttachmentReference
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string ContentHash { get; set; } = string.Empty;
        public string IpfsHash { get; set; } = string.Empty;
        public string AttachmentWalletId { get; set; } = string.Empty;
        public AttachmentMetadata Metadata { get; set; } = new();
    }

    public class AttachmentMetadata
    {
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public string? Description { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
        public VirusScanResult? VirusScan { get; set; }
    }

    public class ValidationResults
    {
        public bool IsValid { get; set; }
        public List<ValidationIssue> Issues { get; set; } = new();
        public SecurityValidation Security { get; set; } = new();
        public AuthenticationResults Authentication { get; set; } = new();
    }

    public class ValidationIssue
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = "Info"; // Info, Warning, Error
    }

    public class SecurityValidation
    {
        public bool PassedSpamFilter { get; set; }
        public bool PassedVirusScan { get; set; }
        public bool HasSuspiciousAttachments { get; set; }
        public bool HasSuspiciousLinks { get; set; }
        public List<string> SecurityFlags { get; set; } = new();
    }

    public class AuthenticationResults
    {
        public SpfResult Spf { get; set; } = new();
        public DkimResult Dkim { get; set; } = new();
        public DmarcResult Dmarc { get; set; } = new();
    }

    public class SpfResult
    {
        public bool Passed { get; set; }
        public string Result { get; set; } = string.Empty;
        public string? Details { get; set; }
    }

    public class DkimResult
    {
        public bool Passed { get; set; }
        public List<string> Signatures { get; set; } = new();
        public string? Details { get; set; }
    }

    public class DmarcResult
    {
        public bool Passed { get; set; }
        public string Policy { get; set; } = string.Empty;
        public string? Details { get; set; }
    }

    public class ProcessingInfo
    {
        public string ProcessingId { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
        public string ProcessorVersion { get; set; } = "1.0.0";
        public WalletInfo EmailWallet { get; set; } = new();
        public List<WalletInfo> AttachmentWallets { get; set; } = new();
        public CreditInfo Credits { get; set; } = new();
    }

    public class WalletInfo
    {
        public string WalletId { get; set; } = string.Empty;
        public string BlockchainAddress { get; set; } = string.Empty;
        public string TransactionHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Failed
    }

    public class CreditInfo
    {
        public int EmailCredits { get; set; } = 3;
        public int AttachmentCredits { get; set; }
        public int AuthorizationCredits { get; set; } = 1;
        public int TotalCredits { get; set; }
        public bool PaymentConfirmed { get; set; }
        public string? PaymentTransactionHash { get; set; }
    }

    // Attachment-specific data package
    public class AttachmentDataPackage
    {
        public AttachmentMetadata Metadata { get; set; } = new();
        public AttachmentContent Content { get; set; } = new();
        public AttachmentValidation Validation { get; set; } = new();
        public WalletInfo Wallet { get; set; } = new();
    }

    public class AttachmentContent
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string ContentHash { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string Encoding { get; set; } = "binary";
        public byte[]? PreviewData { get; set; } // Small preview/thumbnail
    }

    public class AttachmentValidation
    {
        public bool IsValid { get; set; }
        public VirusScanResult VirusScan { get; set; } = new();
        public List<string> ValidationErrors { get; set; } = new();
        public bool IsSafeForDownload { get; set; }
    }

    // Blockchain storage models
    public class BlockchainEmailRecord
    {
        public string EmailWalletId { get; set; } = string.Empty;
        public string IpfsHash { get; set; } = string.Empty;
        public string ContentHash { get; set; } = string.Empty;
        public string OwnerAddress { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public List<string> AttachmentWalletIds { get; set; } = new();
        public int TotalCreditsUsed { get; set; }
    }

    public class BlockchainAttachmentRecord
    {
        public string AttachmentWalletId { get; set; } = string.Empty;
        public string ParentEmailWalletId { get; set; } = string.Empty;
        public string IpfsHash { get; set; } = string.Empty;
        public string ContentHash { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // Enhanced processing task with IPFS integration
    public class EnhancedEmailProcessingTask : EmailProcessingTask
    {
        public string? EmailDataIpfsHash { get; set; }
        public List<AttachmentIpfsInfo> AttachmentIpfsHashes { get; set; } = new();
        public string? BlockchainTransactionHash { get; set; }
        public DateTime? BlockchainConfirmedAt { get; set; }
        public IpfsStorageInfo IpfsStorage { get; set; } = new();
    }

    public class AttachmentIpfsInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string IpfsHash { get; set; } = string.Empty;
        public string AttachmentWalletId { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class IpfsStorageInfo
    {
        public DateTime? EmailDataUploadedAt { get; set; }
        public int AttachmentsUploaded { get; set; }
        public int AttachmentsTotal { get; set; }
        public bool AllUploadsComplete { get; set; }
        public List<string> UploadErrors { get; set; } = new();
    }

    // API response models
    public class EmailProcessingStatusResponse
    {
        public string TaskId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? EmailDataIpfsHash { get; set; }
        public List<AttachmentIpfsInfo> Attachments { get; set; } = new();
        public string? BlockchainTransactionHash { get; set; }
        public ProcessingProgress Progress { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    public class ProcessingProgress
    {
        public bool EmailParsed { get; set; }
        public bool EmailValidated { get; set; }
        public bool EmailUploadedToIpfs { get; set; }
        public bool AttachmentsUploadedToIpfs { get; set; }
        public bool AuthorizationRequested { get; set; }
        public bool AuthorizationCompleted { get; set; }
        public bool RecordedOnBlockchain { get; set; }
        public bool WalletsCreated { get; set; }
        public int ProgressPercentage { get; set; }
    }

    // Enhanced blockchain transaction result (extends base model)
    public class EnhancedBlockchainTransactionResult : BlockchainTransactionResult
    {
        public string? IpfsHash { get; set; }
        public DateTime? BlockchainConfirmedAt { get; set; }
        public List<string> RelatedTransactions { get; set; } = new();
    }

    // Enhanced IPFS upload result (extends base model)
    public class EnhancedIPFSUploadResult : IPFSUploadResult
    {
        public string? IpfsHash { get; set; }
        public string? GatewayUrl { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public bool IsPinned { get; set; }
    }
}