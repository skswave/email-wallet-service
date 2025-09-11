using EmailProcessingService.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MimeKit;
using System.Text.Json;

namespace EmailProcessingService.Services
{
    public interface INotificationService
    {
        Task SendAuthorizationNotificationAsync(AuthorizationRequest request);
        Task SendCompletionNotificationAsync(EmailProcessingTask task);
        Task SendProcessingFailedNotificationAsync(EmailProcessingTask task);
        Task SendTestEmailAsync(string toAddress, string subject, string message);
    }

    public class SimpleNotificationService : INotificationService
    {
        private readonly ILogger<SimpleNotificationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly bool _useSsl;
        private readonly string _fromAddress;
        private readonly string _fromName;
        private readonly string _username;
        private readonly string _password;
        private readonly bool _enabled;

        public SimpleNotificationService(
            ILogger<SimpleNotificationService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            var smtpConfig = configuration.GetSection("Email:SmtpSettings");
            _smtpHost = smtpConfig["Host"] ?? "smtp.office365.com";
            _smtpPort = smtpConfig.GetValue<int>("Port", 587);
            _useSsl = smtpConfig.GetValue<bool>("UseSsl", true);
            _username = smtpConfig["Username"] ?? "";
            _password = smtpConfig["Password"] ?? "";
            
            var emailConfig = configuration.GetSection("Email");
            _fromAddress = emailConfig["FromAddress"] ?? "notifications@rootz.global";
            _fromName = emailConfig["FromName"] ?? "Rootz Email Data Wallet Service";
            _enabled = emailConfig.GetValue<bool>("NotificationsEnabled", true);

            _logger.LogInformation("Notification service configured: {FromAddress} via {SmtpHost}:{SmtpPort} (Enabled: {Enabled})", 
                _fromAddress, _smtpHost, _smtpPort, _enabled);
        }

        public async Task SendAuthorizationNotificationAsync(AuthorizationRequest request)
        {
            if (!_enabled)
            {
                _logger.LogInformation("Notifications disabled, skipping authorization notification for task {TaskId}", request.TaskId);
                return;
            }

            try
            {
                // For now, we'll need to get the user's email address from their registration
                // This is a placeholder - in production you'd look up the user's notification email
                var userEmail = await GetUserNotificationEmailAsync(request.WalletAddress);
                
                if (string.IsNullOrEmpty(userEmail))
                {
                    _logger.LogWarning("No notification email found for wallet {WalletAddress}", request.WalletAddress);
                    return;
                }

                var subject = $"Email Wallet Authorization Required - {request.EmailSubject}";
                var htmlBody = GenerateAuthorizationEmailHtml(request);
                var textBody = GenerateAuthorizationEmailText(request);

                await SendEmailAsync(userEmail, subject, htmlBody, textBody);
                
                _logger.LogInformation("Authorization notification sent for task {TaskId} to {Email}", 
                    request.TaskId, userEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send authorization notification for task {TaskId}", request.TaskId);
            }
        }

        public async Task SendCompletionNotificationAsync(EmailProcessingTask task)
        {
            if (!_enabled)
            {
                _logger.LogInformation("Notifications disabled, skipping completion notification for task {TaskId}", task.TaskId);
                return;
            }

            try
            {
                var userEmail = await GetUserNotificationEmailAsync(task.OwnerWalletAddress);
                
                if (string.IsNullOrEmpty(userEmail))
                {
                    _logger.LogWarning("No notification email found for wallet {WalletAddress}", task.OwnerWalletAddress);
                    return;
                }

                var subject = "Email Wallet Created Successfully";
                var htmlBody = GenerateCompletionEmailHtml(task);
                var textBody = GenerateCompletionEmailText(task);

                await SendEmailAsync(userEmail, subject, htmlBody, textBody);
                
                _logger.LogInformation("Completion notification sent for task {TaskId} to {Email}", 
                    task.TaskId, userEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send completion notification for task {TaskId}", task.TaskId);
            }
        }

        public async Task SendProcessingFailedNotificationAsync(EmailProcessingTask task)
        {
            if (!_enabled)
            {
                _logger.LogInformation("Notifications disabled, skipping failure notification for task {TaskId}", task.TaskId);
                return;
            }

            try
            {
                var userEmail = await GetUserNotificationEmailAsync(task.OwnerWalletAddress);
                
                if (string.IsNullOrEmpty(userEmail))
                {
                    _logger.LogWarning("No notification email found for wallet {WalletAddress}", task.OwnerWalletAddress);
                    return;
                }

                var subject = "Email Wallet Processing Failed";
                var htmlBody = GenerateFailureEmailHtml(task);
                var textBody = GenerateFailureEmailText(task);

                await SendEmailAsync(userEmail, subject, htmlBody, textBody);
                
                _logger.LogInformation("Failure notification sent for task {TaskId} to {Email}", 
                    task.TaskId, userEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send failure notification for task {TaskId}", task.TaskId);
            }
        }

        public async Task SendTestEmailAsync(string toAddress, string subject, string message)
        {
            try
            {
                await SendEmailAsync(toAddress, subject, $"<p>{message}</p>", message);
                _logger.LogInformation("Test email sent to {ToAddress}", toAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test email to {ToAddress}", toAddress);
                throw;
            }
        }

        private async Task SendEmailAsync(string toAddress, string subject, string htmlBody, string textBody)
        {
            if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password))
            {
                _logger.LogWarning("SMTP credentials not configured, cannot send email");
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_fromName, _fromAddress));
            message.To.Add(new MailboxAddress("", toAddress));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody,
                TextBody = textBody
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            await client.ConnectAsync(_smtpHost, _smtpPort, _useSsl);
            await client.AuthenticateAsync(_username, _password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        private async Task<string> GetUserNotificationEmailAsync(string walletAddress)
        {
            // This is a placeholder implementation
            // In production, you would:
            // 1. Look up the user registration by wallet address
            // 2. Return their notification email preference
            // 3. Fall back to their registered email address
            
            // For now, we'll use a simple mapping or configuration
            var notificationEmails = _configuration.GetSection("Email:UserNotifications")
                .Get<Dictionary<string, string>>() ?? new Dictionary<string, string>();
                
            if (notificationEmails.TryGetValue(walletAddress, out var email))
            {
                return email;
            }

            // Fallback to default notification email for testing
            var defaultEmail = _configuration["Email:DefaultNotificationEmail"];
            if (!string.IsNullOrEmpty(defaultEmail))
            {
                _logger.LogInformation("Using default notification email for wallet {WalletAddress}", walletAddress);
                return defaultEmail;
            }

            _logger.LogWarning("No notification email configured for wallet {WalletAddress}", walletAddress);
            return "";
        }

        private string GenerateAuthorizationEmailHtml(AuthorizationRequest request)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; }}
        .button {{ background-color: #28a745; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block; margin: 10px 5px; }}
        .button.danger {{ background-color: #dc3545; }}
        .details {{ background-color: #f8f9fa; padding: 15px; border-radius: 4px; margin: 15px 0; }}
        .footer {{ background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class=""header"">
        <h1>📧 Email Wallet Authorization Required</h1>
    </div>
    <div class=""content"">
        <p>Hi there,</p>
        <p>We've processed an email and prepared a secure blockchain wallet for you.</p>
        
        <div class=""details"">
            <h3>Email Details:</h3>
            <ul>
                <li><strong>Subject:</strong> {request.EmailSubject}</li>
                <li><strong>Sender:</strong> {request.EmailSender}</li>
                <li><strong>Attachments:</strong> {request.AttachmentCount}</li>
                <li><strong>Estimated Cost:</strong> {request.EstimatedCredits} credits</li>
                <li><strong>Expires:</strong> {request.ExpiresAt:yyyy-MM-dd HH:mm} UTC</li>
            </ul>
        </div>

        <p>This wallet will permanently store your email data on the blockchain with cryptographic verification.</p>

        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""{request.AuthorizationUrl}"" class=""button"">✅ Authorize Wallet Creation</a>
            <a href=""#"" class=""button danger"">❌ Reject</a>
        </div>

        <p><strong>You have until {request.ExpiresAt:yyyy-MM-dd HH:mm} UTC to decide.</strong></p>
        <p>No action is needed immediately - you can take your time to review.</p>
    </div>
    <div class=""footer"">
        <p>Rootz Email Data Wallet Service | <a href=""https://rootz.global"">rootz.global</a></p>
        <p>Task ID: {request.TaskId} | Token: {request.Token[..8]}...</p>
    </div>
</body>
</html>";
        }

        private string GenerateAuthorizationEmailText(AuthorizationRequest request)
        {
            return $@"
EMAIL WALLET AUTHORIZATION REQUIRED

Hi there,

We've processed an email and prepared a secure blockchain wallet for you.

Email Details:
- Subject: {request.EmailSubject}
- Sender: {request.EmailSender}
- Attachments: {request.AttachmentCount}
- Estimated Cost: {request.EstimatedCredits} credits
- Expires: {request.ExpiresAt:yyyy-MM-dd HH:mm} UTC

This wallet will permanently store your email data on the blockchain with cryptographic verification.

To authorize this wallet creation, visit:
{request.AuthorizationUrl}

You have until {request.ExpiresAt:yyyy-MM-dd HH:mm} UTC to decide.
No action is needed immediately - you can take your time to review.

Questions? Reply to this email or visit https://rootz.global

---
Rootz Email Data Wallet Service
Task ID: {request.TaskId}
Token: {request.Token[..8]}...
";
        }

        private string GenerateCompletionEmailHtml(EmailProcessingTask task)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; }}
        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; }}
        .success {{ background-color: #d4edda; padding: 15px; border-radius: 4px; margin: 15px 0; border-left: 4px solid #28a745; }}
        .details {{ background-color: #f8f9fa; padding: 15px; border-radius: 4px; margin: 15px 0; }}
        .footer {{ background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class=""header"">
        <h1>✅ Email Wallet Created Successfully</h1>
    </div>
    <div class=""content"">
        <div class=""success"">
            <h3>🎉 Your email wallet has been created and verified on the blockchain!</h3>
        </div>
        
        <div class=""details"">
            <h3>Wallet Details:</h3>
            <ul>
                <li><strong>Task ID:</strong> {task.TaskId}</li>
                <li><strong>Wallet ID:</strong> {task.TemporaryEmailWalletId}</li>
                <li><strong>Credits Used:</strong> {task.ActualCreditsUsed}</li>
                <li><strong>Created:</strong> {task.CreatedAt:yyyy-MM-dd HH:mm} UTC</li>
                <li><strong>Completed:</strong> {task.CompletedAt:yyyy-MM-dd HH:mm} UTC</li>
                <li><strong>Attachments:</strong> {task.TemporaryAttachmentWalletIds.Count} processed</li>
            </ul>
        </div>

        <p>Your email data is now permanently stored on the blockchain with cryptographic verification. You can access and verify your wallet at any time using the wallet ID above.</p>
    </div>
    <div class=""footer"">
        <p>Rootz Email Data Wallet Service | <a href=""https://rootz.global"">rootz.global</a></p>
    </div>
</body>
</html>";
        }

        private string GenerateCompletionEmailText(EmailProcessingTask task)
        {
            return $@"
EMAIL WALLET CREATED SUCCESSFULLY

Your email wallet has been created and verified on the blockchain!

Wallet Details:
- Task ID: {task.TaskId}
- Wallet ID: {task.TemporaryEmailWalletId}
- Credits Used: {task.ActualCreditsUsed}
- Created: {task.CreatedAt:yyyy-MM-dd HH:mm} UTC
- Completed: {task.CompletedAt:yyyy-MM-dd HH:mm} UTC
- Attachments: {task.TemporaryAttachmentWalletIds.Count} processed

Your email data is now permanently stored on the blockchain with cryptographic verification.
You can access and verify your wallet at any time using the wallet ID above.

---
Rootz Email Data Wallet Service
https://rootz.global
";
        }

        private string GenerateFailureEmailHtml(EmailProcessingTask task)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; }}
        .error {{ background-color: #f8d7da; padding: 15px; border-radius: 4px; margin: 15px 0; border-left: 4px solid #dc3545; }}
        .details {{ background-color: #f8f9fa; padding: 15px; border-radius: 4px; margin: 15px 0; }}
        .footer {{ background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class=""header"">
        <h1>❌ Email Wallet Processing Failed</h1>
    </div>
    <div class=""content"">
        <div class=""error"">
            <h3>Unfortunately, we encountered an issue processing your email wallet.</h3>
        </div>
        
        <div class=""details"">
            <h3>Error Details:</h3>
            <ul>
                <li><strong>Task ID:</strong> {task.TaskId}</li>
                <li><strong>Error:</strong> {task.ErrorMessage}</li>
                <li><strong>Failed At:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</li>
                <li><strong>Status:</strong> {task.Status}</li>
            </ul>
        </div>

        <p>Our support team has been notified and will investigate the issue. You can try forwarding the email again, or contact support with the Task ID above.</p>
    </div>
    <div class=""footer"">
        <p>Rootz Email Data Wallet Service | <a href=""https://rootz.global"">rootz.global</a></p>
        <p>Support: support@rootz.global</p>
    </div>
</body>
</html>";
        }

        private string GenerateFailureEmailText(EmailProcessingTask task)
        {
            return $@"
EMAIL WALLET PROCESSING FAILED

Unfortunately, we encountered an issue processing your email wallet.

Error Details:
- Task ID: {task.TaskId}
- Error: {task.ErrorMessage}
- Failed At: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC
- Status: {task.Status}

Our support team has been notified and will investigate the issue.
You can try forwarding the email again, or contact support with the Task ID above.

---
Rootz Email Data Wallet Service
https://rootz.global
Support: support@rootz.global
";
        }
    }
}