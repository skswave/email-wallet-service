// Missing Model Types Only - No Interface Duplicates
// Verified against existing codebase to avoid conflicts

namespace EmailProcessingService.Models
{
    // WalletType enum - Referenced in BlockchainModels.cs but not defined anywhere
    public enum WalletType
    {
        EMAIL_CONTAINER,
        FILE_ATTACHMENT,
        EMAIL_DATA,
        ATTACHMENT,
        REGISTRATION,
        AUTHORIZATION
    }

    // VerificationInfo class - Referenced in WalletCreationResult but not defined
    public class VerificationInfo
    {
        public string ContentHash { get; set; } = string.Empty;
        public string BlockchainTx { get; set; } = string.Empty;
        public long BlockNumber { get; set; }
        public DateTime VerifiedAt { get; set; }
        public string Network { get; set; } = string.Empty;
        public bool IndependentVerification { get; set; }
    }

    // FileMetadataInfo class - Referenced in FileProcessingResult but not defined
    public class FileMetadataInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string FileHash { get; set; } = string.Empty;
    }

    // VirusScanResult class - Referenced in FileProcessingResult but not defined
    public class VirusScanResult
    {
        public bool Scanned { get; set; }
        public bool Clean { get; set; }
        public DateTime ScannedAt { get; set; }
        public string ScanEngine { get; set; } = string.Empty;
        public List<string> ThreatsDetected { get; set; } = new();
    }
}