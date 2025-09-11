using Microsoft.AspNetCore.Mvc;
using EmailProcessingService.Services;
using EmailProcessingService.Models;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace EmailProcessingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailMonitorController : ControllerBase
    {
        private readonly ILogger<EmailMonitorController> _logger;
        private readonly IImapEmailMonitorService _imapService;
        private readonly IExtendedNotificationService _notificationService;
        private readonly IEmailProcessingService _emailProcessingService;

        public EmailMonitorController(
            ILogger<EmailMonitorController> logger,
            IImapEmailMonitorService imapService,
            IExtendedNotificationService notificationService,
            IEmailProcessingService emailProcessingService)
        {
            _logger = logger;
            _imapService = imapService;
            _notificationService = notificationService;
            _emailProcessingService = emailProcessingService;
        }

        /// <summary>
        /// Test IMAP connection to the monitored email account
        /// </summary>
        [HttpGet("test-connection")]
        public async Task<IActionResult> TestImapConnection()
        {
            try
            {
                var isConnected = await _imapService.TestConnectionAsync();
                
                return Ok(new
                {
                    success = isConnected,
                    message = isConnected ? "IMAP connection successful" : "IMAP connection failed",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing IMAP connection");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error testing IMAP connection",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get list of unread emails from the monitored inbox
        /// </summary>
        [HttpGet("unread-emails")]
        public async Task<IActionResult> GetUnreadEmails()
        {
            try
            {
                var emails = await _imapService.GetUnreadEmailsAsync();
                
                var emailSummaries = emails.Select(email => new
                {
                    messageId = email.MessageId,
                    from = email.From,
                    subject = email.Subject,
                    receivedAt = email.ReceivedAt,
                    attachmentCount = email.Attachments.Count,
                    totalSize = email.TotalSize,
                    processingStatus = email.ProcessingStatus
                }).ToList();

                return Ok(new
                {
                    success = true,
                    count = emails.Count,
                    emails = emailSummaries,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unread emails");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving unread emails",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Manually trigger processing of a specific email by Message ID
        /// </summary>
        [HttpPost("process-email/{messageId}")]
        public async Task<IActionResult> ProcessSpecificEmail(string messageId)
        {
            try
            {
                // This would need to be implemented to process a specific email
                // For now, return a placeholder response
                
                _logger.LogInformation("Manual processing requested for email {MessageId}", messageId);
                
                return Ok(new
                {
                    success = true,
                    message = $"Processing initiated for email {messageId}",
                    messageId = messageId,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing email {MessageId}", messageId);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error processing email {messageId}",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Mark an email as processed (read) in the IMAP server
        /// </summary>
        [HttpPost("mark-processed/{messageId}")]
        public async Task<IActionResult> MarkEmailAsProcessed(string messageId)
        {
            try
            {
                await _imapService.MarkEmailAsProcessedAsync(messageId);
                
                return Ok(new
                {
                    success = true,
                    message = $"Email {messageId} marked as processed",
                    messageId = messageId,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking email as processed {MessageId}", messageId);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error marking email {messageId} as processed",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Send a test notification email
        /// </summary>
        [HttpPost("test-notification")]
        public async Task<IActionResult> SendTestNotification([FromBody] TestNotificationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                await _notificationService.SendTestEmailAsync(
                    request.ToAddress, 
                    request.Subject, 
                    request.Message);
                
                return Ok(new
                {
                    success = true,
                    message = $"Test notification sent to {request.ToAddress}",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test notification to {ToAddress}", request.ToAddress);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error sending test notification",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get monitoring service status and statistics
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetMonitoringStatus()
        {
            try
            {
                var isImapConnected = await _imapService.TestConnectionAsync();
                var unreadCount = (await _imapService.GetUnreadEmailsAsync()).Count;
                
                return Ok(new
                {
                    success = true,
                    status = new
                    {
                        imapConnected = isImapConnected,
                        unreadEmailCount = unreadCount,
                        monitoringActive = true, // This would check if the background service is running
                        lastCheck = DateTime.UtcNow
                    },
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monitoring status");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error getting monitoring status",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get recent processing tasks for monitoring
        /// </summary>
        [HttpGet("recent-tasks")]
        public async Task<IActionResult> GetRecentProcessingTasks([FromQuery] int limit = 10)
        {
            try
            {
                // This would get recent tasks from the repository
                // For now, return a placeholder response
                
                return Ok(new
                {
                    success = true,
                    message = "Recent task retrieval not yet implemented",
                    limit = limit,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent tasks");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving recent tasks",
                    error = ex.Message
                });
            }
        }
    }

    public class TestNotificationRequest
    {
        [Required]
        [EmailAddress]
        public string ToAddress { get; set; } = "";

        [Required]
        public string Subject { get; set; } = "";

        [Required]
        public string Message { get; set; } = "";
    }
}