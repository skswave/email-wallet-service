using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit;
using MailKit.Security;
using MimeKit;
using EmailProcessingService.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text;

namespace EmailProcessingService.Services
{
    public interface IImapEmailMonitorService
    {
        Task<bool> TestConnectionAsync();
        Task<List<IncomingEmailMessage>> GetUnreadEmailsAsync();
        Task MarkEmailAsProcessedAsync(string messageId);
    }

    public class ImapEmailMonitorService : BackgroundService, IImapEmailMonitorService
    {
        private readonly ILogger<ImapEmailMonitorService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEmailProcessingService _emailProcessingService;
        private readonly HttpClient _httpClient;
        
        private readonly string _imapServer;
        private readonly int _imapPort;
        private readonly string _username;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _tenantId;
        private readonly int _pollIntervalMinutes;
        private readonly bool _enabled;
        private readonly bool _useOAuth2;

        private string _accessToken = string.Empty;
        private DateTime _tokenExpiry = DateTime.MinValue;

        public ImapEmailMonitorService(
            ILogger<ImapEmailMonitorService> logger,
            IConfiguration configuration,
            IEmailProcessingService emailProcessingService,
            HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _emailProcessingService = emailProcessingService;
            _httpClient = httpClient;

            // Read configuration
            var emailConfig = _configuration.GetSection("Email:Imap");
            _imapServer = emailConfig["Server"] ?? "outlook.office365.com";
            _imapPort = emailConfig.GetValue<int>("Port", 993);
            _username = emailConfig["Username"] ?? throw new ArgumentException("Email:Imap:Username not configured");
            _pollIntervalMinutes = emailConfig.GetValue<int>("PollIntervalMinutes", 1);
            _enabled = emailConfig.GetValue<bool>("Enabled", true);

            // OAuth2 configuration
            var oauthConfig = _configuration.GetSection("Email:OAuth2");
            _useOAuth2 = oauthConfig.GetValue<bool>("Enabled", false);
            _clientId = oauthConfig["ClientId"] ?? "";
            _clientSecret = oauthConfig["ClientSecret"] ?? "";
            _tenantId = oauthConfig["TenantId"] ?? "";

            _logger.LogInformation("IMAP Monitor configured for {Username} on {Server}:{Port} (OAuth2: {UseOAuth2}, Enabled: {Enabled})", 
                _username, _imapServer, _imapPort, _useOAuth2, _enabled);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_enabled)
            {
                _logger.LogInformation("IMAP Email Monitor Service is disabled in configuration");
                return;
            }

            _logger.LogInformation("IMAP Email Monitor Service starting for {Username}", _username);

            // Test connection on startup
            if (!await TestConnectionAsync())
            {
                _logger.LogError("Failed initial IMAP connection test. Service will continue trying...");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await MonitorEmails(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in IMAP email monitoring");
                }

                // Wait before next check
                await Task.Delay(TimeSpan.FromMinutes(_pollIntervalMinutes), stoppingToken);
            }

            _logger.LogInformation("IMAP Email Monitor Service stopping");
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var client = new ImapClient();
                
                await client.ConnectAsync(_imapServer, _imapPort, SecureSocketOptions.SslOnConnect);
                _logger.LogDebug("IMAP connection test: Connected to {Server}:{Port}", _imapServer, _imapPort);

                if (_useOAuth2)
                {
                    var accessToken = await GetAccessTokenAsync();
                    if (string.IsNullOrEmpty(accessToken))
                    {
                        _logger.LogError("Failed to obtain OAuth2 access token");
                        return false;
                    }

                    var oauth2 = new SaslMechanismOAuth2(_username, accessToken);
                    await client.AuthenticateAsync(oauth2);
                    _logger.LogDebug("IMAP OAuth2 authentication successful for {Username}", _username);
                }
                else
                {
                    var password = _configuration["Email:Imap:Password"] ?? "";
                    await client.AuthenticateAsync(_username, password);
                    _logger.LogDebug("IMAP basic authentication successful for {Username}", _username);
                }

                var inbox = client.Inbox;
                await inbox.OpenAsync(FolderAccess.ReadOnly);
                
                var messageCount = inbox.Count;
                _logger.LogInformation("IMAP connection test successful. Inbox contains {Count} messages", messageCount);

                await client.DisconnectAsync(true);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IMAP connection test failed");
                return false;
            }
        }

        private async Task<string> GetAccessTokenAsync()
        {
            try
            {
                // Check if we have a valid token
                if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry.AddMinutes(-5))
                {
                    return _accessToken;
                }

                // Request new token using client credentials flow
                var tokenEndpoint = $"https://login.microsoftonline.com/{_tenantId}/oauth2/v2.0/token";
                
                var requestBody = new List<KeyValuePair<string, string>>
                {
                    new("client_id", _clientId),
                    new("client_secret", _clientSecret),
                    new("scope", "https://outlook.office365.com/.default"),
                    new("grant_type", "client_credentials")
                };

                var content = new FormUrlEncodedContent(requestBody);
                var response = await _httpClient.PostAsync(tokenEndpoint, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("OAuth2 token request failed: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                    return string.Empty;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);

                if (tokenResponse?.access_token == null)
                {
                    _logger.LogError("OAuth2 token response missing access_token");
                    return string.Empty;
                }

                _accessToken = tokenResponse.access_token;
                _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in - 300); // 5 min buffer

                _logger.LogDebug("OAuth2 access token obtained, expires at {Expiry}", _tokenExpiry);
                return _accessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to obtain OAuth2 access token");
                return string.Empty;
            }
        }

        public async Task<List<IncomingEmailMessage>> GetUnreadEmailsAsync()
        {
            var emails = new List<IncomingEmailMessage>();
            
            try
            {
                using var client = new ImapClient();
                
                await client.ConnectAsync(_imapServer, _imapPort, SecureSocketOptions.SslOnConnect);

                if (_useOAuth2)
                {
                    var accessToken = await GetAccessTokenAsync();
                    var oauth2 = new SaslMechanismOAuth2(_username, accessToken);
                    await client.AuthenticateAsync(oauth2);
                }
                else
                {
                    var password = _configuration["Email:Imap:Password"] ?? "";
                    await client.AuthenticateAsync(_username, password);
                }

                var inbox = client.Inbox;
                await inbox.OpenAsync(FolderAccess.ReadOnly);

                var unreadUids = await inbox.SearchAsync(SearchQuery.NotSeen);
                
                foreach (var uid in unreadUids)
                {
                    try
                    {
                        var message = await inbox.GetMessageAsync(uid);
                        var emailMessage = await ConvertMimeMessageToIncomingEmail(message);
                        emails.Add(emailMessage);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing email with UID {Uid}", uid);
                    }
                }

                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unread emails");
            }

            return emails;
        }

        public async Task MarkEmailAsProcessedAsync(string messageId)
        {
            try
            {
                using var client = new ImapClient();
                
                await client.ConnectAsync(_imapServer, _imapPort, SecureSocketOptions.SslOnConnect);

                if (_useOAuth2)
                {
                    var accessToken = await GetAccessTokenAsync();
                    var oauth2 = new SaslMechanismOAuth2(_username, accessToken);
                    await client.AuthenticateAsync(oauth2);
                }
                else
                {
                    var password = _configuration["Email:Imap:Password"] ?? "";
                    await client.AuthenticateAsync(_username, password);
                }

                var inbox = client.Inbox;
                await inbox.OpenAsync(FolderAccess.ReadWrite);

                // Search for the message by Message-ID
                var query = SearchQuery.HeaderContains("Message-ID", messageId);
                var uids = await inbox.SearchAsync(query);

                foreach (var uid in uids)
                {
                    await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true);
                    _logger.LogDebug("Marked email {MessageId} as read (UID: {Uid})", messageId, uid);
                }

                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking email {MessageId} as processed", messageId);
            }
        }

        private async Task MonitorEmails(CancellationToken cancellationToken)
        {
            try
            {
                using var client = new ImapClient();
                
                // Connect to the server
                await client.ConnectAsync(_imapServer, _imapPort, SecureSocketOptions.SslOnConnect, cancellationToken);
                _logger.LogDebug("Connected to IMAP server {Server}:{Port}", _imapServer, _imapPort);

                // Authenticate
                if (_useOAuth2)
                {
                    var accessToken = await GetAccessTokenAsync();
                    if (string.IsNullOrEmpty(accessToken))
                    {
                        _logger.LogError("Failed to obtain OAuth2 access token for monitoring");
                        return;
                    }

                    var oauth2 = new SaslMechanismOAuth2(_username, accessToken);
                    await client.AuthenticateAsync(oauth2, cancellationToken);
                    _logger.LogDebug("OAuth2 authenticated as {Username}", _username);
                }
                else
                {
                    var password = _configuration["Email:Imap:Password"] ?? "";
                    await client.AuthenticateAsync(_username, password, cancellationToken);
                    _logger.LogDebug("Basic authenticated as {Username}", _username);
                }

                // Open the inbox
                var inbox = client.Inbox;
                await inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

                // Search for unread messages
                var unreadUids = await inbox.SearchAsync(SearchQuery.NotSeen, cancellationToken);
                
                if (unreadUids.Count > 0)
                {
                    _logger.LogInformation("Found {Count} unread emails", unreadUids.Count);

                    foreach (var uid in unreadUids)
                    {
                        try
                        {
                            // Fetch the message
                            var message = await inbox.GetMessageAsync(uid, cancellationToken);
                            
                            _logger.LogInformation("Processing email from {From} with subject '{Subject}'", 
                                message.From.ToString(), message.Subject);

                            // Process the email through existing service
                            var result = await _emailProcessingService.ProcessIncomingEmailAsync(message);
                            
                            if (result.Status != ProcessingStatus.Failed)
                            {
                                // Mark as read only if processing succeeded
                                await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true, cancellationToken);
                                
                                _logger.LogInformation("Successfully processed email from {From} with task ID {TaskId}", 
                                    message.From.ToString(), result.TaskId);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to process email from {From}: {Error}", 
                                    message.From.ToString(), result.ErrorMessage);
                                
                                // Optionally, move failed emails to a different folder
                                // or add a custom flag for manual review
                                await inbox.AddFlagsAsync(uid, MessageFlags.Flagged, true, cancellationToken);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing email with UID {Uid}", uid);
                            
                            // Mark problematic emails as flagged for manual review
                            try
                            {
                                await inbox.AddFlagsAsync(uid, MessageFlags.Flagged, true, cancellationToken);
                            }
                            catch
                            {
                                // Ignore flag errors
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogDebug("No unread emails found");
                }

                // Disconnect
                await client.DisconnectAsync(true, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to IMAP server or process emails");
            }
        }

        private async Task<IncomingEmailMessage> ConvertMimeMessageToIncomingEmail(MimeMessage message)
        {
            var emailMessage = new IncomingEmailMessage
            {
                MessageId = message.MessageId ?? Guid.NewGuid().ToString(),
                From = message.From.Mailboxes.FirstOrDefault()?.Address ?? "unknown",
                To = message.To.Mailboxes.Select(m => m.Address).ToList(),
                Cc = message.Cc.Mailboxes.Select(m => m.Address).ToList(),
                Bcc = message.Bcc.Mailboxes.Select(m => m.Address).ToList(),
                Subject = message.Subject ?? "",
                TextBody = message.TextBody ?? "",
                HtmlBody = message.HtmlBody ?? "",
                ReceivedAt = DateTime.UtcNow,
                SentAt = message.Date.DateTime,
                Headers = message.Headers.ToDictionary(h => h.Field, h => h.Value),
                ProcessingStatus = "received"
            };

            // Calculate total size (approximate)
            emailMessage.TotalSize = System.Text.Encoding.UTF8.GetByteCount(emailMessage.TextBody) + 
                                   System.Text.Encoding.UTF8.GetByteCount(emailMessage.HtmlBody);

            // Process attachments
            foreach (var attachment in message.Attachments)
            {
                if (attachment is MimePart mimePart)
                {
                    try
                    {
                        using var stream = new MemoryStream();
                        await mimePart.Content.DecodeToAsync(stream);
                        
                        var emailAttachment = new EmailAttachment
                        {
                            FileName = mimePart.FileName ?? "attachment",
                            ContentType = mimePart.ContentType.MimeType,
                            Size = stream.Length,
                            Content = stream.ToArray(),
                            ContentId = mimePart.ContentId ?? "",
                            IsInline = mimePart.ContentDisposition?.Disposition == "inline",
                            ContentDisposition = mimePart.ContentDisposition?.Disposition ?? "",
                            AttachmentIndex = emailMessage.Attachments.Count
                        };

                        // Calculate content hash
                        using var sha256 = System.Security.Cryptography.SHA256.Create();
                        var hash = sha256.ComputeHash(emailAttachment.Content);
                        emailAttachment.ContentHash = Convert.ToHexString(hash);

                        emailMessage.Attachments.Add(emailAttachment);
                        emailMessage.TotalSize += emailAttachment.Size;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process attachment {FileName}", mimePart.FileName);
                    }
                }
            }

            return emailMessage;
        }

        private class TokenResponse
        {
            public string access_token { get; set; } = "";
            public int expires_in { get; set; }
            public string token_type { get; set; } = "";
        }
    }
}