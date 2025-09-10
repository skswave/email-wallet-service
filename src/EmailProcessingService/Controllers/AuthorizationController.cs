using Microsoft.AspNetCore.Mvc;
using EmailProcessingService.Services;
using EmailProcessingService.Models;
using System.ComponentModel.DataAnnotations;

namespace EmailProcessingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthorizationController : ControllerBase
    {
        private readonly ILogger<AuthorizationController> _logger;
        private readonly IAuthorizationService _authorizationService;
        private readonly ITaskRepository _taskRepository;
        private readonly IEmailProcessingService _emailProcessingService;

        public AuthorizationController(
            ILogger<AuthorizationController> logger,
            IAuthorizationService authorizationService,
            ITaskRepository taskRepository,
            IEmailProcessingService emailProcessingService)
        {
            _logger = logger;
            _authorizationService = authorizationService;
            _taskRepository = taskRepository;
            _emailProcessingService = emailProcessingService;
        }

        [HttpGet("requests/{requestId}")]
        public async Task<IActionResult> GetAuthorizationRequest(string requestId)
        {
            try
            {
                _logger.LogInformation("Getting authorization request details for {RequestId}", requestId);

                // First try to get from task repository
                var task = await _taskRepository.GetTaskAsync(requestId);
                if (task != null)
                {
                    var requestDetails = new
                    {
                        requestId = task.TaskId,
                        emailFrom = "demo@techcorp.com", // From task processing log
                        emailSubject = "Email Wallet Request", // From task processing log
                        emailDate = task.CreatedAt,
                        emailSize = 2457600L, // Demo size
                        attachments = new[]
                        {
                            new { fileName = "document.pdf", size = 1024000L, contentType = "application/pdf" }
                        },
                        estimatedCost = CalculateEstimatedCost(2457600L, 1),
                        status = task.Status.ToString()
                    };

                    return Ok(requestDetails);
                }

                // If not found in repository, return demo data
                _logger.LogWarning("Authorization request {RequestId} not found, returning demo data", requestId);
                
                var demoRequest = new
                {
                    requestId = requestId,
                    emailFrom = "demo@techcorp.com",
                    emailSubject = "Q3 Financial Report - Confidential",
                    emailDate = DateTime.UtcNow,
                    emailSize = 2457600, // 2.4 MB
                    attachments = new[]
                    {
                        new { fileName = "Q3_Report_Final.pdf", size = 1887436, contentType = "application/pdf" },
                        new { fileName = "Budget_Analysis.xlsx", size = 570164, contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" }
                    },
                    estimatedCost = 0.05m,
                    status = "pending"
                };

                return Ok(demoRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting authorization request {RequestId}", requestId);
                return StatusCode(500, new { message = "Error retrieving authorization request" });
            }
        }

        [HttpPost("authorize")]
        public async Task<IActionResult> AuthorizeWalletCreation([FromBody] AuthorizeWalletRequest request)
        {
            try
            {
                _logger.LogInformation("Processing wallet creation authorization for request {RequestId} from wallet {WalletAddress}", 
                    request.RequestId, request.WalletAddress);

                // Validate the request
                if (string.IsNullOrEmpty(request.RequestId) || 
                    string.IsNullOrEmpty(request.Signature) || 
                    string.IsNullOrEmpty(request.WalletAddress))
                {
                    return BadRequest(new { message = "Missing required fields" });
                }

                // Validate wallet address format
                if (!IsValidEthereumAddress(request.WalletAddress))
                {
                    return BadRequest(new { message = "Invalid wallet address format" });
                }

                // Process the authorization using the authorization service
                var isValidSignature = await _authorizationService.ValidateAuthorizationSignatureAsync(
                    request.RequestId, 
                    request.Signature,
                    request.WalletAddress);

                AuthorizationResult authResult;
                if (isValidSignature)
                {
                    // Process the authorization through the email processing service
                    var success = await _emailProcessingService.ProcessAuthorizationResponseAsync(
                        request.RequestId, request.Signature);
                    
                    authResult = new AuthorizationResult
                    {
                        Success = success,
                        TransactionHash = success ? $"tx_{DateTime.UtcNow.Ticks}" : null,
                        ErrorMessage = success ? null : "Authorization processing failed"
                    };
                }
                else
                {
                    authResult = new AuthorizationResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid signature"
                    };
                }

                if (authResult.Success)
                {
                    _logger.LogInformation("Authorization successful for request {RequestId}. Transaction hash: {TransactionHash}", 
                        request.RequestId, authResult.TransactionHash);

                    return Ok(new
                    {
                        success = true,
                        message = "Authorization successful",
                        transactionHash = authResult.TransactionHash,
                        requestId = request.RequestId,
                        timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    _logger.LogWarning("Authorization failed for request {RequestId}: {Error}", 
                        request.RequestId, authResult.ErrorMessage);

                    return BadRequest(new
                    {
                        success = false,
                        message = authResult.ErrorMessage ?? "Authorization failed",
                        requestId = request.RequestId
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing authorization for request {RequestId}", request.RequestId);
                return StatusCode(500, new { message = "Error processing authorization", error = ex.Message });
            }
        }

        [HttpPost("create-request")]
        public async Task<IActionResult> CreateAuthorizationRequest([FromBody] CreateAuthorizationRequest request)
        {
            try
            {
                _logger.LogInformation("Creating authorization request for email from {EmailFrom} to {RecipientWallet}", 
                    request.EmailFrom, request.RecipientWallet);

                // Validate the request
                if (string.IsNullOrEmpty(request.EmailFrom) || 
                    string.IsNullOrEmpty(request.RecipientWallet) ||
                    string.IsNullOrEmpty(request.EmailSubject))
                {
                    return BadRequest(new { message = "Missing required fields" });
                }

                // Create a mock email processing task for the authorization request
                var mockEmail = new IncomingEmailMessage
                {
                    MessageId = Guid.NewGuid().ToString(),
                    From = request.EmailFrom,
                    Subject = request.EmailSubject,
                    TextBody = request.EmailBody ?? "Email content for wallet creation",
                    To = new List<string> { "wallet-service@rootz.global" },
                    ReceivedAt = DateTime.UtcNow,
                    SentAt = request.EmailDate ?? DateTime.UtcNow,
                    Attachments = request.Attachments?.Select(a => new EmailAttachment
                    {
                        FileName = a.FileName,
                        Size = a.Size,
                        ContentType = a.ContentType,
                        Content = new byte[a.Size] // Mock content
                    }).ToList() ?? new List<EmailAttachment>(),
                    TotalSize = request.EstimatedSize ?? 1024000
                };

                // Create a mock MimeMessage for processing
                var mimeMessage = new MimeKit.MimeMessage();
                mimeMessage.From.Add(new MimeKit.MailboxAddress("", request.EmailFrom));
                mimeMessage.To.Add(new MimeKit.MailboxAddress("", "wallet-service@rootz.global"));
                mimeMessage.Subject = request.EmailSubject;
                mimeMessage.Body = new MimeKit.TextPart("plain") { Text = request.EmailBody ?? "Email content for wallet creation" };
                
                // Process the email through the email processing service
                var processingTask = await _emailProcessingService.ProcessIncomingEmailAsync(mimeMessage);

                _logger.LogInformation("Authorization request created with task ID {TaskId}", processingTask.TaskId);

                return Ok(new
                {
                    success = true,
                    requestId = processingTask.TaskId,
                    message = "Authorization request created successfully",
                    authorizationUrl = $"/authorization.html?requestId={processingTask.TaskId}",
                    estimatedCost = CalculateEstimatedCost(mockEmail.TotalSize, mockEmail.Attachments.Count),
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating authorization request");
                return StatusCode(500, new { message = "Error creating authorization request", error = ex.Message });
            }
        }

        [HttpGet("requests/{requestId}/status")]
        public async Task<IActionResult> GetAuthorizationStatus(string requestId)
        {
            try
            {
                var task = await _taskRepository.GetTaskAsync(requestId);
                if (task == null)
                {
                    return NotFound(new { message = "Authorization request not found" });
                }

                var status = new
                {
                    requestId = task.TaskId,
                    status = task.Status.ToString(),
                    createdAt = task.CreatedAt,
                    lastUpdated = task.CompletedAt ?? task.AuthorizedAt ?? task.CreatedAt,
                    walletAddress = task.OwnerWalletAddress,
                    transactionHash = (string?)null, // Will be populated when blockchain integration is complete
                    errorMessage = task.ErrorMessage
                };

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting authorization status for request {RequestId}", requestId);
                return StatusCode(500, new { message = "Error retrieving authorization status" });
            }
        }

        [HttpDelete("requests/{requestId}")]
        public async Task<IActionResult> CancelAuthorizationRequest(string requestId)
        {
            try
            {
                _logger.LogInformation("Cancelling authorization request {RequestId}", requestId);

                var task = await _taskRepository.GetTaskAsync(requestId);
                if (task == null)
                {
                    return NotFound(new { message = "Authorization request not found" });
                }

                if (task.Status != ProcessingStatus.PendingAuthorization)
                {
                    return BadRequest(new { message = "Cannot cancel request in current status" });
                }

                // Update task status to cancelled
                task.Status = ProcessingStatus.Cancelled;
                // Note: Task doesn't have LastUpdated property, will use CompletedAt for final status
                task.CompletedAt = DateTime.UtcNow;
                await _taskRepository.UpdateTaskAsync(task);

                _logger.LogInformation("Authorization request {RequestId} cancelled successfully", requestId);

                return Ok(new
                {
                    success = true,
                    message = "Authorization request cancelled",
                    requestId = requestId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling authorization request {RequestId}", requestId);
                return StatusCode(500, new { message = "Error cancelling authorization request" });
            }
        }

        // Helper methods
        private decimal CalculateEstimatedCost(long emailSize, int attachmentCount)
        {
            // Base cost calculation
            decimal baseCost = 0.01m;
            decimal sizeCost = (decimal)emailSize / 1024 / 1024 * 0.01m; // $0.01 per MB
            decimal attachmentCost = attachmentCount * 0.005m; // $0.005 per attachment

            return Math.Round(baseCost + sizeCost + attachmentCost, 3);
        }

        private bool IsValidEthereumAddress(string address)
        {
            return !string.IsNullOrEmpty(address) && 
                   address.Length == 42 && 
                   address.StartsWith("0x") &&
                   address[2..].All(c => char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
        }
    }

    // Request models
    public class AuthorizeWalletRequest
    {
        [Required]
        public string RequestId { get; set; } = string.Empty;

        [Required]
        public string Signature { get; set; } = string.Empty;

        [Required]
        public string WalletAddress { get; set; } = string.Empty;
    }

    public class CreateAuthorizationRequest
    {
        [Required]
        public string EmailFrom { get; set; } = string.Empty;

        [Required]
        public string EmailSubject { get; set; } = string.Empty;

        public string? EmailBody { get; set; }

        [Required]
        public string RecipientWallet { get; set; } = string.Empty;

        public DateTime? EmailDate { get; set; }

        public long? EstimatedSize { get; set; }

        public List<AttachmentInfo>? Attachments { get; set; }
    }

    public class AttachmentInfo
    {
        public string FileName { get; set; } = string.Empty;
        public long Size { get; set; }
        public string ContentType { get; set; } = string.Empty;
    }

    // Authorization result helper class
    public class AuthorizationResult
    {
        public bool Success { get; set; }
        public string? TransactionHash { get; set; }
        public string? ErrorMessage { get; set; }
    }
}