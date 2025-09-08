using EmailProcessingService.Models;

namespace EmailProcessingService.Services
{
    public interface IFileProcessorService
    {
        Task<FileProcessingResult> ProcessFileAsync(byte[] fileContent, string fileName);
        Task<VirusScanResult> ScanFileAsync(byte[] fileContent);
        Task<FileMetadataInfo> ExtractMetadataAsync(byte[] fileContent, string fileName);
    }

    public class FileProcessorService : IFileProcessorService
    {
        private readonly ILogger<FileProcessorService> _logger;
        
        public FileProcessorService(ILogger<FileProcessorService> logger)
        {
            _logger = logger;
        }

        public async Task<FileProcessingResult> ProcessFileAsync(byte[] fileContent, string fileName)
        {
            try
            {
                _logger.LogInformation("Processing file: {FileName} ({Size} bytes)", fileName, fileContent.Length);
                
                // Simulate processing time
                await Task.Delay(100);
                
                var result = new FileProcessingResult
                {
                    Success = true,
                    FileName = fileName,
                    FileSize = fileContent.Length,
                    ContentHash = ComputeHash(fileContent),
                    MimeType = GetContentType(fileName),
                    ExtractedMetadata = new FileMetadataInfo
                    {
                        ContentType = GetContentType(fileName),
                        FileType = Path.GetExtension(fileName),
                        ProcessedAt = DateTime.UtcNow
                    },
                    VirusScanResult = new VirusScanResult
                    {
                        Scanned = true,
                        Clean = true,
                        Scanner = "MVP-MockScanner",
                        ScannedAt = DateTime.UtcNow
                    }
                };
                
                _logger.LogInformation("File processed successfully: {FileName}", fileName);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file: {FileName}", fileName);
                
                return new FileProcessingResult
                {
                    Success = false,
                    FileName = fileName,
                    FileSize = fileContent.Length,
                    ErrorMessage = ex.Message,
                    ExtractedMetadata = new FileMetadataInfo
                    {
                        ContentType = GetContentType(fileName),
                        FileType = Path.GetExtension(fileName),
                        ProcessedAt = DateTime.UtcNow
                    },
                    VirusScanResult = new VirusScanResult
                    {
                        Scanned = false,
                        Clean = false,
                        Scanner = "MVP-MockScanner",
                        ScannedAt = DateTime.UtcNow
                    }
                };
            }
        }

        public async Task<VirusScanResult> ScanFileAsync(byte[] fileContent)
        {
            await Task.Delay(50); // Simulate scan time
            
            return new VirusScanResult
            {
                Scanned = true,
                Clean = true,
                Scanner = "MVP-MockScanner",
                ScannedAt = DateTime.UtcNow
            };
        }

        public async Task<FileMetadataInfo> ExtractMetadataAsync(byte[] fileContent, string fileName)
        {
            await Task.Delay(25); // Simulate extraction time
            
            return new FileMetadataInfo
            {
                ContentType = GetContentType(fileName),
                FileType = Path.GetExtension(fileName),
                ProcessedAt = DateTime.UtcNow
            };
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".zip" => "application/zip",
                _ => "application/octet-stream"
            };
        }

        private string ComputeHash(byte[] fileContent)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(fileContent);
            return Convert.ToHexString(hashBytes);
        }
    }
}