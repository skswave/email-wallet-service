using Microsoft.AspNetCore.Mvc;
using EmailProcessingService.Services;
using EmailProcessingService.Models;

namespace EmailProcessingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IpfsValidationController : ControllerBase
    {
        private readonly ILogger<IpfsValidationController> _logger;
        private readonly IIpfsService _ipfsService;
        private readonly IIpfsValidationService _validationService;
        private readonly ITaskRepository _taskRepository;

        public IpfsValidationController(
            ILogger<IpfsValidationController> logger,
            IIpfsService ipfsService,
            IIpfsValidationService validationService,
            ITaskRepository taskRepository)
        {
            _logger = logger;
            _ipfsService = ipfsService;
            _validationService = validationService;
            _taskRepository = taskRepository;
        }

        /// <summary>
        /// Validate any IPFS hash - automatically detects content type
        /// </summary>
        [HttpPost("validate")]
        public async Task<ActionResult<IpfsValidationResponse>> ValidateIpfsHash([FromBody] IpfsValidationRequest request)
        {
            try
            {
                _logger.LogInformation("Validating IPFS hash: {Hash}", request.IpfsHash);

                if (string.IsNullOrWhiteSpace(request.IpfsHash))
                {
                    return BadRequest(new { error = "IPFS hash is required" });
                }

                var result = await _ipfsService.ValidateIPFSProofAsync(request.IpfsHash);

                var response = new IpfsValidationResponse
                {
                    Success = result.Success,
                    IPFSHash = request.IpfsHash,
                    IsValid = result.IsValid,
                    ContentVerified = result.ContentVerified,
                    WalletVerified = result.WalletVerified,
                    ValidationResults = result.ValidationResults,
                    UserKey = result.UserKey,
                    WalletAddress = result.WalletAddress,
                    TransactionHash = result.TransactionHash,
                    ContentHash = result.ContentHash,
                    ValidatedAt = DateTime.UtcNow,
                    ErrorMessage = result.Error
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating IPFS hash: {Hash}", request.IpfsHash);
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Validate email data package from IPFS
        /// </summary>
        [HttpPost("validate/email")]
        public async Task<ActionResult<IpfsValidationResponse>> ValidateEmailDataPackage([FromBody] IpfsValidationRequest request)
        {
            try
            {
                _logger.LogInformation("Validating email data package: {Hash}", request.IpfsHash);

                var result = await _validationService.ValidateEmailDataPackageAsync(request.IpfsHash);

                var response = new IpfsValidationResponse
                {
                    Success = result.Success,
                    IPFSHash = request.IpfsHash,
                    IsValid = result.IsValid,
                    ContentVerified = result.ContentVerified,
                    ValidationResults = result.ValidationResults,
                    ValidatedAt = DateTime.UtcNow,
                    ErrorMessage = result.Error,
                    ContentType = "EmailDataPackage"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating email data package: {Hash}", request.IpfsHash);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Validate attachment package from IPFS
        /// </summary>
        [HttpPost("validate/attachment")]
        public async Task<ActionResult<IpfsValidationResponse>> ValidateAttachmentPackage([FromBody] IpfsValidationRequest request)
        {
            try
            {
                _logger.LogInformation("Validating attachment package: {Hash}", request.IpfsHash);

                var result = await _validationService.ValidateAttachmentPackageAsync(request.IpfsHash);

                var response = new IpfsValidationResponse
                {
                    Success = result.Success,
                    IPFSHash = request.IpfsHash,
                    IsValid = result.IsValid,
                    ValidationResults = result.ValidationResults,
                    ValidatedAt = DateTime.UtcNow,
                    ErrorMessage = result.Error,
                    ContentType = "AttachmentDataPackage"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating attachment package: {Hash}", request.IpfsHash);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Validate multiple IPFS hashes at once
        /// </summary>
        [HttpPost("validate/batch")]
        public async Task<ActionResult<BatchValidationResponse>> ValidateMultipleHashes([FromBody] BatchValidationRequest request)
        {
            try
            {
                _logger.LogInformation("Validating {Count} IPFS hashes", request.IpfsHashes.Count);

                if (!request.IpfsHashes.Any())
                {
                    return BadRequest(new { error = "At least one IPFS hash is required" });
                }

                if (request.IpfsHashes.Count > 20)
                {
                    return BadRequest(new { error = "Maximum 20 hashes allowed per batch" });
                }

                var results = await _validationService.ValidateMultipleHashesAsync(request.IpfsHashes);

                var response = new BatchValidationResponse
                {
                    Success = true,
                    TotalHashes = results.Count,
                    ValidHashes = results.Count(r => r.Success && r.IsValid),
                    InvalidHashes = results.Count(r => !r.Success || !r.IsValid),
                    Results = results.Select(r => new IpfsValidationResponse
                    {
                        Success = r.Success,
                        IPFSHash = r.IPFSHash,
                        IsValid = r.IsValid,
                        ContentVerified = r.ContentVerified,
                        WalletVerified = r.WalletVerified,
                        ValidationResults = r.ValidationResults,
                        UserKey = r.UserKey,
                        WalletAddress = r.WalletAddress,
                        TransactionHash = r.TransactionHash,
                        ContentHash = r.ContentHash,
                        ErrorMessage = r.Error
                    }).ToList(),
                    ValidatedAt = DateTime.UtcNow
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating batch of IPFS hashes");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Validate complete email processing task with all IPFS content
        /// </summary>
        [HttpPost("validate/task/{taskId}")]
        public async Task<ActionResult<TaskValidationResponse>> ValidateEmailProcessingTask(string taskId)
        {
            try
            {
                _logger.LogInformation("Validating email processing task: {TaskId}", taskId);

                var summary = await _validationService.ValidateEmailProcessingTaskAsync(taskId);

                var response = new TaskValidationResponse
                {
                    Success = summary.Success,
                    TaskId = taskId,
                    AllValid = summary.AllValid,
                    EmailDataHash = summary.EmailDataHash,
                    AttachmentHashes = summary.AttachmentHashes,
                    TotalHashes = summary.TotalHashes,
                    ValidHashes = summary.ValidHashes,
                    InvalidHashes = summary.InvalidHashes,
                    ValidationResults = summary.ValidationResults.Select(r => new IpfsValidationResponse
                    {
                        Success = r.Success,
                        IPFSHash = r.IPFSHash,
                        IsValid = r.IsValid,
                        ContentVerified = r.ContentVerified,
                        ValidationResults = r.ValidationResults,
                        ErrorMessage = r.Error
                    }).ToList(),
                    ValidatedAt = summary.ValidatedAt,
                    ErrorMessage = summary.Error
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating email processing task: {TaskId}", taskId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Download and display IPFS content for inspection
        /// </summary>
        [HttpGet("content/{ipfsHash}")]
        public async Task<ActionResult> GetIpfsContent(string ipfsHash, [FromQuery] bool raw = false)
        {
            try
            {
                _logger.LogInformation("Retrieving IPFS content: {Hash}", ipfsHash);

                var content = await _ipfsService.DownloadFileAsync(ipfsHash);
                var contentString = System.Text.Encoding.UTF8.GetString(content);

                if (raw)
                {
                    return Content(contentString, "application/json");
                }

                // Try to parse as JSON for better display
                try
                {
                    var jsonDocument = System.Text.Json.JsonDocument.Parse(contentString);
                    var formattedJson = System.Text.Json.JsonSerializer.Serialize(jsonDocument, new System.Text.Json.JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                    
                    return Ok(new
                    {
                        ipfsHash = ipfsHash,
                        contentType = "application/json",
                        size = content.Length,
                        retrievedAt = DateTime.UtcNow,
                        content = System.Text.Json.JsonSerializer.Deserialize<object>(formattedJson)
                    });
                }
                catch
                {
                    // Return as plain text if not valid JSON
                    return Ok(new
                    {
                        ipfsHash = ipfsHash,
                        contentType = "text/plain",
                        size = content.Length,
                        retrievedAt = DateTime.UtcNow,
                        content = contentString
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving IPFS content: {Hash}", ipfsHash);
                return StatusCode(500, new { error = "Failed to retrieve content", details = ex.Message });
            }
        }

        /// <summary>
        /// Test IPFS connectivity and upload/download capability
        /// </summary>
        [HttpPost("test")]
        public async Task<ActionResult<IpfsTestResponse>> TestIpfsIntegration([FromBody] IpfsTestRequest? request = null)
        {
            try
            {
                _logger.LogInformation("Testing IPFS integration");

                var testData = request?.TestData ?? $"IPFS Integration Test - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
                var testFileName = $"test-{DateTime.UtcNow:yyyyMMddHHmmss}.txt";

                var response = new IpfsTestResponse
                {
                    TestStarted = DateTime.UtcNow
                };

                // Test connection
                response.ConnectionTest = await _ipfsService.TestConnectionAsync();
                if (!response.ConnectionTest)
                {
                    response.Success = false;
                    response.ErrorMessage = "IPFS connection test failed";
                    return Ok(response);
                }

                // Test upload
                var uploadResult = await _ipfsService.UploadFileAsync(
                    System.Text.Encoding.UTF8.GetBytes(testData),
                    testFileName,
                    new Dictionary<string, object>
                    {
                        { "test", true },
                        { "timestamp", DateTime.UtcNow },
                        { "source", "api-test" }
                    });

                response.UploadTest = uploadResult.Success;
                response.IpfsHash = uploadResult.IpfsHash;
                response.GatewayUrl = uploadResult.GatewayUrl;

                if (!uploadResult.Success)
                {
                    response.Success = false;
                    response.ErrorMessage = $"Upload failed: {uploadResult.ErrorMessage}";
                    return Ok(response);
                }

                // Test download
                try
                {
                    var downloadedData = await _ipfsService.DownloadFileAsync(uploadResult.IpfsHash!);
                    var downloadedText = System.Text.Encoding.UTF8.GetString(downloadedData);
                    response.DownloadTest = downloadedText == testData;
                    response.DownloadedContent = downloadedText;
                }
                catch (Exception ex)
                {
                    response.DownloadTest = false;
                    response.ErrorMessage = $"Download failed: {ex.Message}";
                }

                // Test validation
                if (response.DownloadTest)
                {
                    try
                    {
                        var validationResult = await _ipfsService.ValidateIPFSProofAsync(uploadResult.IpfsHash!);
                        response.ValidationTest = validationResult.Success;
                    }
                    catch (Exception ex)
                    {
                        response.ValidationTest = false;
                        response.ErrorMessage += $" Validation failed: {ex.Message}";
                    }
                }

                response.Success = response.ConnectionTest && response.UploadTest && response.DownloadTest;
                response.TestCompleted = DateTime.UtcNow;
                response.TestDuration = response.TestCompleted - response.TestStarted;

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing IPFS integration");
                return Ok(new IpfsTestResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    TestStarted = DateTime.UtcNow,
                    TestCompleted = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Get validation statistics for monitoring
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<ValidationStatsResponse>> GetValidationStats([FromQuery] DateTime? since = null)
        {
            try
            {
                var sinceDate = since ?? DateTime.UtcNow.AddDays(-7);
                
                // This would typically query a validation log/database
                // For now, return placeholder statistics
                var stats = new ValidationStatsResponse
                {
                    TotalValidations = 150,
                    SuccessfulValidations = 142,
                    FailedValidations = 8,
                    SuccessRate = 94.7m,
                    AverageValidationTime = TimeSpan.FromSeconds(2.3),
                    Since = sinceDate,
                    GeneratedAt = DateTime.UtcNow
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting validation statistics");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }

    // Request/Response models for validation API
    public class IpfsValidationRequest
    {
        public string IpfsHash { get; set; } = string.Empty;
    }

    public class IpfsValidationResponse
    {
        public bool Success { get; set; }
        public string IPFSHash { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public bool ContentVerified { get; set; }
        public bool WalletVerified { get; set; }
        public List<string> ValidationResults { get; set; } = new();
        public string? UserKey { get; set; }
        public string? WalletAddress { get; set; }
        public string? TransactionHash { get; set; }
        public string? ContentHash { get; set; }
        public string? ContentType { get; set; }
        public DateTime ValidatedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class BatchValidationRequest
    {
        public List<string> IpfsHashes { get; set; } = new();
    }

    public class BatchValidationResponse
    {
        public bool Success { get; set; }
        public int TotalHashes { get; set; }
        public int ValidHashes { get; set; }
        public int InvalidHashes { get; set; }
        public List<IpfsValidationResponse> Results { get; set; } = new();
        public DateTime ValidatedAt { get; set; }
    }

    public class TaskValidationResponse
    {
        public bool Success { get; set; }
        public string TaskId { get; set; } = string.Empty;
        public bool AllValid { get; set; }
        public string? EmailDataHash { get; set; }
        public List<string> AttachmentHashes { get; set; } = new();
        public int TotalHashes { get; set; }
        public int ValidHashes { get; set; }
        public int InvalidHashes { get; set; }
        public List<IpfsValidationResponse> ValidationResults { get; set; } = new();
        public DateTime ValidatedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class IpfsTestRequest
    {
        public string? TestData { get; set; }
    }

    public class IpfsTestResponse
    {
        public bool Success { get; set; }
        public bool ConnectionTest { get; set; }
        public bool UploadTest { get; set; }
        public bool DownloadTest { get; set; }
        public bool ValidationTest { get; set; }
        public string? IpfsHash { get; set; }
        public string? GatewayUrl { get; set; }
        public string? DownloadedContent { get; set; }
        public DateTime TestStarted { get; set; }
        public DateTime TestCompleted { get; set; }
        public TimeSpan TestDuration { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class ValidationStatsResponse
    {
        public int TotalValidations { get; set; }
        public int SuccessfulValidations { get; set; }
        public int FailedValidations { get; set; }
        public decimal SuccessRate { get; set; }
        public TimeSpan AverageValidationTime { get; set; }
        public DateTime Since { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}