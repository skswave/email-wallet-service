using EmailProcessingService.Models;
using EmailProcessingService.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EmailProcessingService.Services
{
    public interface IIpfsValidationService
    {
        Task<IpfsValidationResult> ValidateEmailDataPackageAsync(string ipfsHash);
        Task<IpfsValidationResult> ValidateAttachmentPackageAsync(string ipfsHash);
        Task<IpfsValidationResult> ValidateProofIntegrityAsync(string ipfsHash);
        Task<List<IpfsValidationResult>> ValidateMultipleHashesAsync(List<string> ipfsHashes);
        Task<IpfsValidationSummary> ValidateEmailProcessingTaskAsync(string taskId);
    }

    public class IpfsValidationService : IIpfsValidationService
    {
        private readonly IIpfsService _ipfsService;
        private readonly ITaskRepository _taskRepository;
        private readonly ILogger<IpfsValidationService> _logger;

        public IpfsValidationService(
            IIpfsService ipfsService,
            ITaskRepository taskRepository,
            ILogger<IpfsValidationService> logger)
        {
            _ipfsService = ipfsService;
            _taskRepository = taskRepository;
            _logger = logger;
        }

        public async Task<IpfsValidationResult> ValidateEmailDataPackageAsync(string ipfsHash)
        {
            try
            {
                _logger.LogInformation("Validating email data package: {Hash}", ipfsHash);

                var content = await _ipfsService.DownloadFileAsync(ipfsHash);
                var jsonContent = System.Text.Encoding.UTF8.GetString(content);

                var emailPackage = JsonSerializer.Deserialize<EmailDataPackage>(jsonContent);
                if (emailPackage == null)
                {
                    return new IpfsValidationResult
                    {
                        Success = false,
                        Error = "Failed to parse email data package"
                    };
                }

                var validation = new IpfsValidationResult
                {
                    Success = true,
                    IPFSHash = ipfsHash,
                    RawContent = jsonContent,
                    RetrievedAt = DateTime.UtcNow
                };

                var validationResults = new List<string>();

                // Validate email metadata
                if (emailPackage.Metadata != null)
                {
                    validationResults.Add("✓ Email metadata present");
                    
                    if (!string.IsNullOrEmpty(emailPackage.Metadata.MessageId))
                        validationResults.Add("✓ Message ID present");
                    else
                        validationResults.Add("✗ Missing message ID");

                    if (!string.IsNullOrEmpty(emailPackage.Metadata.Subject))
                        validationResults.Add("✓ Email subject present");
                    else
                        validationResults.Add("✗ Missing email subject");

                    if (emailPackage.Metadata.From?.Address != null)
                        validationResults.Add($"✓ From address: {emailPackage.Metadata.From.Address}");
                    else
                        validationResults.Add("✗ Missing from address");
                }
                else
                {
                    validationResults.Add("✗ Missing email metadata");
                }

                // Validate email content
                if (emailPackage.Content != null)
                {
                    validationResults.Add("✓ Email content present");
                    
                    if (!string.IsNullOrEmpty(emailPackage.Content.ContentHash))
                    {
                        validationResults.Add("✓ Content hash present");
                        
                        // Verify content hash integrity
                        var bodyContent = emailPackage.Content.TextBody ?? emailPackage.Content.HtmlBody ?? "";
                        var calculatedHash = CalculateContentHash(bodyContent);
                        
                        if (emailPackage.Content.ContentHash == calculatedHash)
                        {
                            validationResults.Add("✓ Content hash verified - email content is authentic");
                            validation.ContentVerified = true;
                        }
                        else
                        {
                            validationResults.Add("✗ Content hash mismatch - possible tampering");
                            validation.ContentVerified = false;
                        }
                    }
                    else
                    {
                        validationResults.Add("✗ Missing content hash");
                    }
                }
                else
                {
                    validationResults.Add("✗ Missing email content");
                }

                // Validate attachments
                if (emailPackage.Attachments?.Any() == true)
                {
                    validationResults.Add($"✓ {emailPackage.Attachments.Count} attachments referenced");
                    
                    foreach (var attachment in emailPackage.Attachments)
                    {
                        if (!string.IsNullOrEmpty(attachment.IpfsHash))
                        {
                            validationResults.Add($"✓ Attachment {attachment.FileName} has IPFS hash: {attachment.IpfsHash}");
                        }
                        else
                        {
                            validationResults.Add($"✗ Attachment {attachment.FileName} missing IPFS hash");
                        }
                    }
                }

                // Validate processing info
                if (emailPackage.Processing != null)
                {
                    validationResults.Add("✓ Processing information present");
                    
                    if (!string.IsNullOrEmpty(emailPackage.Processing.ProcessingId))
                        validationResults.Add($"✓ Processing ID: {emailPackage.Processing.ProcessingId}");
                    
                    if (emailPackage.Processing.Credits != null)
                    {
                        var credits = emailPackage.Processing.Credits;
                        var totalCredits = credits.EmailCredits + credits.AttachmentCredits + credits.AuthorizationCredits;
                        validationResults.Add($"✓ Credits calculated: {totalCredits} total (Email: {credits.EmailCredits}, Attachments: {credits.AttachmentCredits}, Auth: {credits.AuthorizationCredits})");
                    }
                }

                validation.ValidationResults = validationResults;
                validation.IsValid = validation.ContentVerified && !validationResults.Any(r => r.StartsWith("✗"));

                _logger.LogInformation("Email data package validation completed: {Hash}, Valid: {IsValid}", ipfsHash, validation.IsValid);
                return validation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate email data package: {Hash}", ipfsHash);
                return new IpfsValidationResult
                {
                    Success = false,
                    Error = $"Validation failed: {ex.Message}"
                };
            }
        }

        public async Task<IpfsValidationResult> ValidateAttachmentPackageAsync(string ipfsHash)
        {
            try
            {
                _logger.LogInformation("Validating attachment package: {Hash}", ipfsHash);

                var content = await _ipfsService.DownloadFileAsync(ipfsHash);
                var jsonContent = System.Text.Encoding.UTF8.GetString(content);

                var attachmentPackage = JsonSerializer.Deserialize<AttachmentDataPackage>(jsonContent);
                if (attachmentPackage == null)
                {
                    return new IpfsValidationResult
                    {
                        Success = false,
                        Error = "Failed to parse attachment data package"
                    };
                }

                var validation = new IpfsValidationResult
                {
                    Success = true,
                    IPFSHash = ipfsHash,
                    RawContent = jsonContent,
                    RetrievedAt = DateTime.UtcNow
                };

                var validationResults = new List<string>();

                // Validate attachment metadata
                if (attachmentPackage.Metadata != null)
                {
                    validationResults.Add("✓ Attachment metadata present");
                }

                // Validate attachment content
                if (attachmentPackage.Content != null)
                {
                    validationResults.Add("✓ Attachment content info present");
                    
                    if (!string.IsNullOrEmpty(attachmentPackage.Content.FileName))
                        validationResults.Add($"✓ File name: {attachmentPackage.Content.FileName}");
                    
                    if (!string.IsNullOrEmpty(attachmentPackage.Content.ContentHash))
                        validationResults.Add("✓ Content hash present");
                    
                    if (attachmentPackage.Content.SizeBytes > 0)
                        validationResults.Add($"✓ File size: {attachmentPackage.Content.SizeBytes} bytes");
                }

                // Validate virus scan results
                if (attachmentPackage.Validation?.VirusScan != null)
                {
                    var virusScan = attachmentPackage.Validation.VirusScan;
                    if (virusScan.Scanned)
                    {
                        if (virusScan.Clean)
                        {
                            validationResults.Add("✓ Virus scan: Clean");
                        }
                        else
                        {
                            validationResults.Add("✗ Virus scan: Threat detected");
                        }
                    }
                    else
                    {
                        validationResults.Add("⚠ Virus scan: Not performed");
                    }
                }

                validation.ValidationResults = validationResults;
                validation.IsValid = !validationResults.Any(r => r.StartsWith("✗"));

                _logger.LogInformation("Attachment package validation completed: {Hash}, Valid: {IsValid}", ipfsHash, validation.IsValid);
                return validation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate attachment package: {Hash}", ipfsHash);
                return new IpfsValidationResult
                {
                    Success = false,
                    Error = $"Validation failed: {ex.Message}"
                };
            }
        }

        public async Task<IpfsValidationResult> ValidateProofIntegrityAsync(string ipfsHash)
        {
            // Use the built-in IPFS service validation for backward compatibility with MVP
            return await _ipfsService.ValidateIPFSProofAsync(ipfsHash);
        }

        public async Task<List<IpfsValidationResult>> ValidateMultipleHashesAsync(List<string> ipfsHashes)
        {
            var results = new List<IpfsValidationResult>();

            var tasks = ipfsHashes.Select(async hash =>
            {
                try
                {
                    // Try to determine the type of content and validate accordingly
                    var content = await _ipfsService.DownloadFileAsync(hash);
                    var jsonContent = System.Text.Encoding.UTF8.GetString(content);

                    // Check if it's an email data package
                    if (jsonContent.Contains("\"Metadata\"") && jsonContent.Contains("\"Content\"") && jsonContent.Contains("\"Attachments\""))
                    {
                        return await ValidateEmailDataPackageAsync(hash);
                    }
                    // Check if it's an attachment package
                    else if (jsonContent.Contains("\"FileName\"") && jsonContent.Contains("\"ContentHash\""))
                    {
                        return await ValidateAttachmentPackageAsync(hash);
                    }
                    // Fallback to proof validation
                    else
                    {
                        return await ValidateProofIntegrityAsync(hash);
                    }
                }
                catch (Exception ex)
                {
                    return new IpfsValidationResult
                    {
                        Success = false,
                        IPFSHash = hash,
                        Error = $"Validation failed: {ex.Message}"
                    };
                }
            });

            results = (await Task.WhenAll(tasks)).ToList();
            return results;
        }

        public async Task<IpfsValidationSummary> ValidateEmailProcessingTaskAsync(string taskId)
        {
            try
            {
                _logger.LogInformation("Validating email processing task: {TaskId}", taskId);

                var task = await _taskRepository.GetTaskAsync(taskId);
                if (task == null)
                {
                    return new IpfsValidationSummary
                    {
                        Success = false,
                        Error = "Task not found"
                    };
                }

                var enhancedTask = task as EnhancedEmailProcessingTask;
                if (enhancedTask == null)
                {
                    return new IpfsValidationSummary
                    {
                        Success = false,
                        Error = "Task does not support IPFS validation (not enhanced)"
                    };
                }

                var summary = new IpfsValidationSummary
                {
                    Success = true,
                    TaskId = taskId,
                    ValidatedAt = DateTime.UtcNow
                };

                var validationTasks = new List<Task<IpfsValidationResult>>();

                // Validate email data if available
                if (!string.IsNullOrEmpty(enhancedTask.EmailDataIpfsHash))
                {
                    validationTasks.Add(ValidateEmailDataPackageAsync(enhancedTask.EmailDataIpfsHash));
                    summary.EmailDataHash = enhancedTask.EmailDataIpfsHash;
                }

                // Validate attachments
                foreach (var attachment in enhancedTask.AttachmentIpfsHashes)
                {
                    validationTasks.Add(ValidateAttachmentPackageAsync(attachment.IpfsHash));
                    summary.AttachmentHashes.Add(attachment.IpfsHash);
                }

                if (validationTasks.Any())
                {
                    var results = await Task.WhenAll(validationTasks);
                    summary.ValidationResults = results.ToList();
                    
                    summary.AllValid = results.All(r => r.Success && r.IsValid);
                    summary.TotalHashes = results.Length;
                    summary.ValidHashes = results.Count(r => r.Success && r.IsValid);
                    summary.InvalidHashes = results.Count(r => !r.Success || !r.IsValid);
                }
                else
                {
                    summary.Error = "No IPFS hashes found for validation";
                    summary.Success = false;
                }

                _logger.LogInformation("Task validation completed: {TaskId}, Valid: {AllValid}, Total: {Total}, Valid: {Valid}, Invalid: {Invalid}",
                    taskId, summary.AllValid, summary.TotalHashes, summary.ValidHashes, summary.InvalidHashes);

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate email processing task: {TaskId}", taskId);
                return new IpfsValidationSummary
                {
                    Success = false,
                    Error = $"Validation failed: {ex.Message}"
                };
            }
        }

        private string CalculateContentHash(string content)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
            return "0x" + Convert.ToHexString(hash).ToLowerInvariant();
        }
    }

    // Validation summary model
    public class IpfsValidationSummary
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string TaskId { get; set; } = string.Empty;
        public string? EmailDataHash { get; set; }
        public List<string> AttachmentHashes { get; set; } = new();
        public List<IpfsValidationResult> ValidationResults { get; set; } = new();
        public bool AllValid { get; set; }
        public int TotalHashes { get; set; }
        public int ValidHashes { get; set; }
        public int InvalidHashes { get; set; }
        public DateTime ValidatedAt { get; set; }
    }
}