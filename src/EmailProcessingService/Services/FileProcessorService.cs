using EmailProcessingService.Models;

namespace EmailProcessingService.Services
{
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
                
                // Placeholder implementation for MVP
                var result = new FileProcessingResult
                {
                    Success = true,
                    FileName = fileName,
                    FileSize = fileContent.Length,
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
                        ScannedAt = DateTime.UtcNow,
                        ScanEngine = "MVP-MockScanner"
                    }
                };

                await Task.Delay(100); // Simulate processing time
                
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
                    ExtractedMetadata = new FileMetadataInfo(),
                    VirusScanResult = new VirusScanResult 
                    { 
                        Scanned = false, 
                        Clean = false, 
                        ScannedAt = DateTime.UtcNow,
                        ScanEngine = "MVP-MockScanner"
                    }
                };
            }
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
                _ => "application/octet-stream"
            };
        }
    }
}
