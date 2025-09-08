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
