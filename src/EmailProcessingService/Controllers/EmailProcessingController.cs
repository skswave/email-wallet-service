using EmailProcessingService.Models;
using EmailProcessingService.Services;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using System.ComponentModel.DataAnnotations;

namespace EmailProcessingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailProcessingController : ControllerBase
    {
        private readonly IEmailProcessingService _emailProcessingService;
        private readonly IBlockchainService _blockchainService;
        private readonly IAuthorizationService _authorizationService;
        private readonly ITaskRepository _taskRepository;
        private readonly ILogger<EmailProcessingController> _logger;

        public EmailProcessingController(
            IEmailProcessingService emailProcessingService,
            IBlockchainService blockchainService,
            IAuthorizationService authorizationService,
            ITaskRepository taskRepository,
            ILogger<EmailProcessingController> logger)
        {
            _emailProcessingService = emailProcessingService;
            _blockchainService = blockchainService;
            _authorizationService = authorizationService;
            _taskRepository = taskRepository;
            _logger = logger;
        }

        /// <summary>
        /// Process an incoming email from raw email content
        /// </summary>
        [HttpPost("process")]
        public async Task<ActionResult<EmailProcessingResponse>> ProcessEmailAsync([FromBody] ProcessEmailRequest request)
        {
            try
            {
                _logger.LogInformation("Received email processing request");

                if (string.IsNullOrEmpty(request.RawEmailContent))
                {
                    return BadRequest(new { error = "Raw email content is required" });
                }

                var task = await _emailProcessingService.ProcessIncomingEmailAsync(request.RawEmailContent);

                var response = new EmailProcessingResponse
                {
                    TaskId = task.TaskId,
                    Status = task.Status.ToString(),
                    Message = GetStatusMessage(task.Status),
                    AuthorizationUrl = task.AuthorizationUrl,
                    EstimatedCredits = task.EstimatedCredits,
                    CreatedAt = task.CreatedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing email request");
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Process an email file upload
        /// </summary>
        [HttpPost("upload")]
        public async Task<ActionResult<EmailProcessingResponse>> UploadEmailAsync(IFormFile emailFile)
        {
            try
            {
                if (emailFile == null || emailFile.Length == 0)
                {
                    return BadRequest(new { error = "Email file is required" });
                }

                if (!emailFile.FileName.EndsWith(".eml") && !emailFile.FileName.EndsWith(".msg"))
                {
                    return BadRequest(new { error = "Only .eml and .msg files are supported" });
                }

                using var stream = emailFile.OpenReadStream();
                var mimeMessage = await MimeMessage.LoadAsync(stream);

                var task = await _emailProcessingService.ProcessIncomingEmailAsync(mimeMessage);

                var response = new EmailProcessingResponse
                {
                    TaskId = task.TaskId,
                    Status = task.Status.ToString(),
                    Message = GetStatusMessage(task.Status),
                    AuthorizationUrl = task.AuthorizationUrl,
                    EstimatedCredits = task.EstimatedCredits,
                    CreatedAt = task.CreatedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing uploaded email file");
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Get the status of an email processing task
        /// </summary>
        [HttpGet("task/{taskId}/status")]
        public async Task<ActionResult<TaskStatusResponse>> GetTaskStatusAsync(string taskId)
        {
            try
            {
                var task = await _emailProcessingService.GetProcessingTaskAsync(taskId);

                if (task == null)
                {
                    return NotFound(new { error = "Task not found" });
                }

                var response = new TaskStatusResponse
                {
                    TaskId = task.TaskId,
                    Status = task.Status.ToString(),
                    Message = GetStatusMessage(task.Status),
                    OwnerWalletAddress = task.OwnerWalletAddress,
                    CreatedAt = task.CreatedAt,
                    AuthorizedAt = task.AuthorizedAt,
                    CompletedAt = task.CompletedAt,
                    EstimatedCredits = task.EstimatedCredits,
                    ActualCreditsUsed = task.ActualCreditsUsed,
                    ErrorMessage = task.ErrorMessage,
                    ProcessingLog = task.ProcessingLog.ToList(),
                    EmailWalletId = task.TemporaryEmailWalletId,
                    AttachmentWalletIds = task.TemporaryAttachmentWalletIds.ToList()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task status for {TaskId}", taskId);
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all tasks for a specific wallet address
        /// </summary>
        [HttpGet("wallet/{walletAddress}/tasks")]
        public async Task<ActionResult<List<TaskSummary>>> GetWalletTasksAsync(string walletAddress)
        {
            try
            {
                var tasks = await _emailProcessingService.GetTasksForUserAsync(walletAddress);

                var summaries = tasks.Select(task => new TaskSummary
                {
                    TaskId = task.TaskId,
                    Status = task.Status.ToString(),
                    CreatedAt = task.CreatedAt,
                    CompletedAt = task.CompletedAt,
                    EstimatedCredits = task.EstimatedCredits,
                    ActualCreditsUsed = task.ActualCreditsUsed,
                    EmailWalletId = task.TemporaryEmailWalletId,
                    AttachmentCount = task.TemporaryAttachmentWalletIds.Count()
                }).ToList();

                return Ok(summaries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks for wallet {WalletAddress}", walletAddress);
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Authorize a pending email processing task
        /// </summary>
        [HttpPost("task/{taskId}/authorize")]
        public async Task<ActionResult<AuthorizationResponse>> AuthorizeTaskAsync(string taskId, [FromBody] AuthorizeTaskRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.AuthorizationSignature))
                {
                    return BadRequest(new { error = "Authorization signature is required" });
                }

                var success = await _emailProcessingService.ProcessAuthorizationResponseAsync(taskId, request.AuthorizationSignature);

                if (!success)
                {
                    return BadRequest(new { error = "Authorization failed", message = "Invalid signature or task not found" });
                }

                var response = new AuthorizationResponse
                {
                    TaskId = taskId,
                    Authorized = true,
                    Message = "Task authorized successfully, processing will continue",
                    AuthorizedAt = DateTime.UtcNow
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authorizing task {TaskId}", taskId);
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Get wallet information and credits
        /// </summary>
        [HttpGet("wallet/{walletAddress}/info")]
        public async Task<ActionResult<WalletInfoResponse>> GetWalletInfoAsync(string walletAddress)
        {
            try
            {
                var isRegistered = await _blockchainService.IsWalletRegisteredAsync(walletAddress);
                var creditBalance = await _blockchainService.GetCreditBalanceAsync(walletAddress);
                
                var response = new WalletInfoResponse
                {
                    WalletAddress = walletAddress,
                    IsRegistered = isRegistered,
                    Credits = creditBalance?.Balance ?? 0,
                    EmailWalletCount = 0, // Will be implemented with proper wallet retrieval
                    EmailWalletIds = new List<string>()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wallet info for {WalletAddress}", walletAddress);
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpGet("health")]
        public async Task<ActionResult<HealthCheckResponse>> HealthCheckAsync()
        {
            try
            {
                var blockchainConnected = await _blockchainService.TestConnectionAsync();
                
                var response = new HealthCheckResponse
                {
                    Status = blockchainConnected ? "Healthy" : "Degraded",
                    Timestamp = DateTime.UtcNow,
                    Services = new Dictionary<string, bool>
                    {
                        { "Blockchain", blockchainConnected },
                        { "EmailProcessing", true },
                        { "Authorization", true }
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during health check");
                return StatusCode(500, new { error = "Health check failed", details = ex.Message });
            }
        }

        /// <summary>
        /// Get processing statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<ProcessingStatsResponse>> GetStatsAsync()
        {
            try
            {
                if (_taskRepository is InMemoryTaskRepository inMemoryRepo)
                {
                    var totalTasks = await inMemoryRepo.GetTaskCountAsync();
                    var taskStats = await inMemoryRepo.GetTaskStatisticsAsync();

                    var response = new ProcessingStatsResponse
                    {
                        TotalTasks = totalTasks,
                        TasksByStatus = taskStats,
                        GeneratedAt = DateTime.UtcNow
                    };

                    return Ok(response);
                }

                return Ok(new ProcessingStatsResponse { GeneratedAt = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting processing statistics");
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        private static string GetStatusMessage(ProcessingStatus status)
        {
            return status switch
            {
                ProcessingStatus.Received => "Email received and parsing started",
                ProcessingStatus.Validating => "Validating email content and sender",
                ProcessingStatus.Creating => "Creating temporary wallets",
                ProcessingStatus.PendingAuthorization => "Waiting for user authorization",
                ProcessingStatus.Authorized => "Authorization received, processing continuing",
                ProcessingStatus.Processing => "Processing email data",
                ProcessingStatus.StoringToIPFS => "Uploading data to IPFS",
                ProcessingStatus.VerifyingOnBlockchain => "Recording on blockchain",
                ProcessingStatus.Completed => "Email processing completed successfully",
                ProcessingStatus.Failed => "Email processing failed",
                _ => "Unknown status"
            };
        }
    }

    // Request/Response models
    public class ProcessEmailRequest
    {
        [Required]
        public string RawEmailContent { get; set; } = string.Empty;
    }

    public class AuthorizeTaskRequest
    {
        [Required]
        public string AuthorizationSignature { get; set; } = string.Empty;
    }

    public class EmailProcessingResponse
    {
        public string TaskId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? AuthorizationUrl { get; set; }
        public int EstimatedCredits { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TaskStatusResponse
    {
        public string TaskId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? OwnerWalletAddress { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? AuthorizedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int EstimatedCredits { get; set; }
        public int ActualCreditsUsed { get; set; }
        public string? ErrorMessage { get; set; }
        public List<ProcessingLogEntry> ProcessingLog { get; set; } = new();
        public string? EmailWalletId { get; set; }
        public List<string> AttachmentWalletIds { get; set; } = new();
    }

    public class TaskSummary
    {
        public string TaskId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int EstimatedCredits { get; set; }
        public int ActualCreditsUsed { get; set; }
        public string? EmailWalletId { get; set; }
        public int AttachmentCount { get; set; }
    }

    public class AuthorizationResponse
    {
        public string TaskId { get; set; } = string.Empty;
        public bool Authorized { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime AuthorizedAt { get; set; }
    }

    public class WalletInfoResponse
    {
        public string WalletAddress { get; set; } = string.Empty;
        public bool IsRegistered { get; set; }
        public long Credits { get; set; }
        public int EmailWalletCount { get; set; }
        public List<string> EmailWalletIds { get; set; } = new();
    }

    public class HealthCheckResponse
    {
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Dictionary<string, bool> Services { get; set; } = new();
    }

    public class ProcessingStatsResponse
    {
        public int TotalTasks { get; set; }
        public Dictionary<ProcessingStatus, int> TasksByStatus { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }
}
