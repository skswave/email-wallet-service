using System.ComponentModel.DataAnnotations;
using MimeKit;

namespace EmailProcessingService.Models
{
    // Email Processing Models
    public class IncomingEmailMessage
    {
        [Required]
        public string MessageId { get; set; } = string.Empty;
        
        [Required]
        public string From { get; set; } = string.Empty;
        
        [Required]
        public List<string> To { get; set; } = new();
        
        public List<string> Cc { get; set; } = new();
        
        public List<string> Bcc { get; set; } = new();
        
        [Required]
        public string Subject { get; set; } = string.Empty;
        
        public string TextBody { get; set; } = string.Empty;
        
        public string HtmlBody { get; set; } = string.Empty;
        
        public DateTime ReceivedAt { get; set; }
        
        public DateTime SentAt { get; set; }
        
        public List<EmailAttachment> Attachments { get; set; } = new();
        
        public Dictionary<string, string> Headers { get; set; } = new();
        
        public string RawMessage { get; set; } = string.Empty;
        
        public long TotalSize { get; set; }
        
        public string ForwardedBy { get; set; } = string.Empty; // The user who forwarded this email
        
        public string ProcessingStatus { get; set; } = "received";
    }

    // Email Attachment
    public class EmailAttachment
    {
        [Required]
        public string FileName { get; set; } = string.Empty;
        
        public string ContentType { get; set; } = string.Empty;
        
        public long Size { get; set; }
        
        public byte[] Content { get; set; } = Array.Empty<byte>();
        
        public string ContentId { get; set; } = string.Empty;
        
        public bool IsInline { get; set; }
        
        public string ContentDisposition { get; set; } = string.Empty;
        
        public string ContentHash { get; set; } = string.Empty;
        
        public int AttachmentIndex { get; set; }
    }

    // Email Validation Result
    public class EmailValidationResult
    {
        public bool IsValid => IsSenderRegistered || IsWhitelistAuthorized;
        
        public bool IsSenderRegistered { get; set; }
        
        public bool IsWhitelistAuthorized { get; set; }
        
        public string? RegisteredWalletAddress { get; set; }
        
        public UserRegistration? RegisteredWallet { get; set; }
        
        public string WhitelistReason { get; set; } = string.Empty;
        
        public bool SPFPass { get; set; }
        
        public bool DKIMValid { get; set; }
        
        public bool DMARCPass { get; set; }
        
        public bool CorporateAuthValid { get; set; }
        
        public List<string> ValidationErrors { get; set; } = new();
        
        public List<string> SecurityWarnings { get; set; } = new();
    }

    // User Registration
    public class UserRegistration
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string WalletAddress { get; set; } = string.Empty;
        
        [Required]
        public string EmailAddress { get; set; } = string.Empty;
        
        public string DisplayName { get; set; } = string.Empty;
        
        public string? ParentCorporateWallet { get; set; }
        
        public bool IsVerified { get; set; }
        
        public DateTime RegisteredAt { get; set; }
        
        public DateTime? VerifiedAt { get; set; }
        
        public string RegistrationTx { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        
        public UserRegistrationSettings Settings { get; set; } = new();
        
        public List<string> WhitelistedDomains { get; set; } = new();
        
        public int ProcessedEmailCount { get; set; }
        
        public int TotalCreditsUsed { get; set; }
    }

    // User Registration Settings
    public class UserRegistrationSettings
    {
        public bool AutoProcessWhitelistedEmails { get; set; } = false;
        
        public bool RequireExplicitAuth { get; set; } = true;
        
        public int MaxEmailSize { get; set; } = 25 * 1024 * 1024; // 25MB
        
        public int MaxAttachmentCount { get; set; } = 10;
        
        public List<string> AllowedFileTypes { get; set; } = new() { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".jpg", ".png" };
        
        public bool EnableVirusScanning { get; set; } = true;
        
        public string NotificationEmail { get; set; } = string.Empty;
        
        public string TimeZone { get; set; } = "UTC";
    }

    // Processing Task
    public class EmailProcessingTask
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string TaskId { get; set; } = string.Empty;
        
        [Required]
        public string MessageId { get; set; } = string.Empty;
        
        [Required]
        public string OwnerWalletAddress { get; set; } = string.Empty;
        
        public string TemporaryEmailWalletId { get; set; } = string.Empty;
        
        public List<string> TemporaryAttachmentWalletIds { get; set; } = new();
        
        public ProcessingStatus Status { get; set; } = ProcessingStatus.Received;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? AuthorizedAt { get; set; }
        
        public DateTime? CompletedAt { get; set; }
        
        public string AuthorizationToken { get; set; } = string.Empty;
        
        public string AuthorizationUrl { get; set; } = string.Empty;
        
        public int EstimatedCredits { get; set; }
        
        public int ActualCreditsUsed { get; set; }
        
        public string? ErrorMessage { get; set; }
        
        public List<ProcessingLogEntry> ProcessingLog { get; set; } = new();
    }

    // Processing Status
    public enum ProcessingStatus
    {
        Received = 1,
        Validating = 2,
        Creating = 3,
        PendingAuthorization = 4,
        Authorized = 5,
        Processing = 6,
        StoringToIPFS = 7,
        VerifyingOnBlockchain = 8,
        Completed = 9,
        Failed = 10,
        Cancelled = 11
    }

    // Processing Log Entry
    public class ProcessingLogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public string Step { get; set; } = string.Empty;
        
        public string Status { get; set; } = string.Empty;
        
        public string Message { get; set; } = string.Empty;
        
        public string? Error { get; set; }
        
        public TimeSpan Duration { get; set; }
    }

    // Authorization Request
    public class AuthorizationRequest
    {
        [Required]
        public string TaskId { get; set; } = string.Empty;
        
        [Required]
        public string WalletAddress { get; set; } = string.Empty;
        
        [Required]
        public string Token { get; set; } = string.Empty;
        
        public DateTime ExpiresAt { get; set; }
        
        public string EmailSubject { get; set; } = string.Empty;
        
        public string EmailSender { get; set; } = string.Empty;
        
        public int AttachmentCount { get; set; }
        
        public int EstimatedCredits { get; set; }
        
        public List<AttachmentSummary> AttachmentSummaries { get; set; } = new();
        
        public string AuthorizationUrl { get; set; } = string.Empty;
        
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    }

    // Attachment Summary
    public class AttachmentSummary
    {
        public string FileName { get; set; } = string.Empty;
        
        public long FileSize { get; set; }
        
        public string FileType { get; set; } = string.Empty;
        
        public string ContentHash { get; set; } = string.Empty;
        
        public bool VirusScanPassed { get; set; }
    }

    // Wallet Creation Result
    public class WalletCreationResult
    {
        public bool Success { get; set; }
        
        public string? EmailWalletId { get; set; }
        
        public List<string> AttachmentWalletIds { get; set; } = new();
        
        public string? ErrorMessage { get; set; }
        
        public int CreditsUsed { get; set; }
        
        public TimeSpan ProcessingTime { get; set; }
        
        public VerificationInfo? VerificationInfo { get; set; }
    }

    // IPFS Upload Result
    public class IPFSUploadResult
    {
        public bool Success { get; set; }
        
        public string Hash { get; set; } = string.Empty;
        
        public long Size { get; set; }
        
        public string Gateway { get; set; } = string.Empty;
        
        public string? ErrorMessage { get; set; }
        
        public TimeSpan UploadTime { get; set; }
        
        public List<string> BackupLocations { get; set; } = new();
    }

    // Blockchain Transaction Result
    public class BlockchainTransactionResult
    {
        public bool Success { get; set; }
        
        public string TransactionHash { get; set; } = string.Empty;
        
        public long BlockNumber { get; set; }
        
        public string Network { get; set; } = string.Empty;
        
        public decimal GasUsed { get; set; }
        
        public decimal GasCost { get; set; }
        
        public string? ErrorMessage { get; set; }
        
        public TimeSpan ConfirmationTime { get; set; }
    }

    // Credit Calculation
    public class CreditCalculation
    {
        public int EmailDataWalletCredits { get; set; } = 3;
        
        public int AttachmentWalletCredits { get; set; } = 2;
        
        public int AuthorizationCredits { get; set; } = 1;
        
        public int AttachmentCount { get; set; }
        
        public int TotalCredits => EmailDataWalletCredits + (AttachmentWalletCredits * AttachmentCount) + AuthorizationCredits;
        
        public Dictionary<string, int> CreditBreakdown => new()
        {
            { "Email Data Wallet", EmailDataWalletCredits },
            { "File Attachments", AttachmentWalletCredits * AttachmentCount },
            { "Authorization", AuthorizationCredits },
            { "Total", TotalCredits }
        };
    }

    // File Processing Result
    public class FileProcessingResult
    {
        public bool Success { get; set; }
        
        public string FileName { get; set; } = string.Empty;
        
        public long FileSize { get; set; }
        
        public string ContentHash { get; set; } = string.Empty;
        
        public string MimeType { get; set; } = string.Empty;
        
        public FileMetadataInfo ExtractedMetadata { get; set; } = new();
        
        public VirusScanResult VirusScanResult { get; set; } = new();
        
        public string? ErrorMessage { get; set; }
        
        public bool RequiresManualReview { get; set; }
        
        public List<string> SecurityWarnings { get; set; } = new();
    }

    // Email Authentication Check
    public class EmailAuthenticationCheck
    {
        public bool SPFPass { get; set; }
        
        public bool DKIMValid { get; set; }
        
        public bool DMARCPass { get; set; }
        
        public string SPFRecord { get; set; } = string.Empty;
        
        public string DKIMSignature { get; set; } = string.Empty;
        
        public string DMARCPolicy { get; set; } = string.Empty;
        
        public List<string> AuthenticationWarnings { get; set; } = new();
        
        public int AuthenticationScore => (SPFPass ? 1 : 0) + (DKIMValid ? 1 : 0) + (DMARCPass ? 1 : 0);
        
        public bool IsFullyAuthenticated => AuthenticationScore == 3;
    }

    // Whitelist Entry
    public class WhitelistEntry
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string OwnerWalletAddress { get; set; } = string.Empty;
        
        [Required]
        public string Domain { get; set; } = string.Empty;
        
        public string? SpecificEmail { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public string CreatedBy { get; set; } = string.Empty;
        
        public bool AutoProcess { get; set; } = false;
        
        public string Reason { get; set; } = string.Empty;
        
        public int UsageCount { get; set; }
        
        public DateTime? LastUsed { get; set; }
    }
}