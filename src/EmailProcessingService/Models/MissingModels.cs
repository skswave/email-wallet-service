// Missing Types for Email Wallet Service - Minimal Fix
using EmailProcessingService.Models;

namespace EmailProcessingService.Models
{
    // WalletType enum - Referenced in BlockchainModels.cs
    public enum WalletType
    {
        EmailData,
        Attachment, 
        Registration,
        Authorization
    }

    // VerificationInfo class - Referenced in WalletCreationResult
    public class VerificationInfo
    {
        public string ContentHash { get; set; } = string.Empty;
        public string BlockchainTx { get; set; } = string.Empty;
        public long BlockNumber { get; set; }
        public DateTime VerifiedAt { get; set; }
        public string Network { get; set; } = string.Empty;
    }

    // FileMetadataInfo class - Referenced in FileProcessingResult
    public class FileMetadataInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string FileHash { get; set; } = string.Empty;
    }

    // VirusScanResult class - Referenced in FileProcessingResult
    public class VirusScanResult
    {
        public bool Scanned { get; set; }
        public bool Clean { get; set; }
        public DateTime ScannedAt { get; set; }
        public string ScanEngine { get; set; } = string.Empty;
        public List<string> ThreatsDetected { get; set; } = new();
    }
}

namespace EmailProcessingService.Services
{
    using EmailProcessingService.Models;

    // IFileProcessorService interface - Required by FileProcessorService
    public interface IFileProcessorService
    {
        Task<FileProcessingResult> ProcessFileAsync(byte[] fileContent, string fileName);
    }

    // IWalletCreatorService interface - Required by EmailProcessingService
    public interface IWalletCreatorService
    {
        Task<WalletCreationResult> CreateEmailDataWalletAsync(IncomingEmailMessage email, UserRegistration user);
    }

    // WalletCreatorService implementation - Required by Program.cs
    public class WalletCreatorService : IWalletCreatorService
    {
        private readonly ILogger<WalletCreatorService> _logger;

        public WalletCreatorService(ILogger<WalletCreatorService> logger)
        {
            _logger = logger;
        }

        public async Task<WalletCreationResult> CreateEmailDataWalletAsync(IncomingEmailMessage email, UserRegistration user)
        {
            await Task.Delay(50); // Simulate processing

            return new WalletCreationResult
            {
                Success = true,
                EmailWalletId = $"email_wallet_{Guid.NewGuid():N}",
                AttachmentWalletIds = email.Attachments.Select(a => $"attachment_wallet_{Guid.NewGuid():N}").ToList(),
                CreditsUsed = 3 + (email.Attachments.Count * 2),
                ProcessingTime = TimeSpan.FromMilliseconds(50)
            };
        }
    }
}