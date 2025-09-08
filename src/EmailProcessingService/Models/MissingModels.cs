// Missing Types for Email Wallet Service Compilation
// Based on actual compilation errors and code analysis

using System.ComponentModel.DataAnnotations;

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

    // VerificationInfo class - Referenced in WalletCreationResult.VerificationInfo property
    public class VerificationInfo
    {
        public string ContentHash { get; set; } = string.Empty;
        public string BlockchainTx { get; set; } = string.Empty;
        public long BlockNumber { get; set; }
        public DateTime VerifiedAt { get; set; }
        public string Network { get; set; } = string.Empty;
    }

    // FileMetadataInfo class - Referenced in FileProcessingResult.ExtractedMetadata property
    public class FileMetadataInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string FileHash { get; set; } = string.Empty;
    }

    // VirusScanResult class - Referenced in FileProcessingResult.VirusScanResult property
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
    // IFileProcessorService interface - Required by existing FileProcessorService implementation
    public interface IFileProcessorService
    {
        Task<FileProcessingResult> ProcessFileAsync(byte[] fileContent, string fileName);
    }

    // IWalletCreatorService interface - Referenced in EmailProcessingService and Program.cs
    public interface IWalletCreatorService
    {
        Task<WalletCreationResult> CreateEmailDataWalletAsync(IncomingEmailMessage email, UserRegistration user);
    }

    // WalletCreatorService implementation - Required by Program.cs service registration
    public class WalletCreatorService : IWalletCreatorService
    {
        private readonly ILogger<WalletCreatorService> _logger;

        public WalletCreatorService(ILogger<WalletCreatorService> logger)
        {
            _logger = logger;
        }

        public async Task<WalletCreationResult> CreateEmailDataWalletAsync(IncomingEmailMessage email, UserRegistration user)
        {
            _logger.LogInformation("Creating email data wallet for message {MessageId} and user {WalletAddress}", 
                email.MessageId, user.WalletAddress);

            // Simulate wallet creation process
            await Task.Delay(100);

            return new WalletCreationResult
            {
                Success = true,
                EmailWalletId = $"email_wallet_{Guid.NewGuid():N}",
                AttachmentWalletIds = email.Attachments.Select(a => $"attachment_wallet_{Guid.NewGuid():N}").ToList(),
                CreditsUsed = 3 + (email.Attachments.Count * 2),
                ProcessingTime = TimeSpan.FromMilliseconds(100)
            };
        }
    }
}