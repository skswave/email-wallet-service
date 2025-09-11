using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using EmailProcessingService.Models;
using System.Net.Http;
using System.Text.Json;
using System.Text;

namespace EmailProcessingService.Services
{
    public interface IGraphEmailMonitorService
    {
        Task<bool> TestConnectionAsync();
        Task<List<IncomingEmailMessage>> GetUnreadEmailsAsync();
        Task MarkEmailAsReadAsync(string messageId);
    }

    public class GraphEmailMonitorService : BackgroundService, IGraphEmailMonitorService
    {
        private readonly ILogger<GraphEmailMonitorService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEmailProcessingService _emailProcessingService;
        private readonly HttpClient _httpClient;
        
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _tenantId;
        private readonly string _userPrincipalName;
        private readonly int _pollIntervalMinutes;
        private readonly bool _enabled;

        private string _accessToken = string.Empty;
        private DateTime _tokenExpiry = DateTime.MinValue;

        public GraphEmailMonitorService(
            ILogger<GraphEmailMonitorService> logger,
            IConfiguration configuration,
            IEmailProcessingService emailProcessingService,
            HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _emailProcessingService = emailProcessingService;
            _httpClient = httpClient;

            // Read configuration
            var graphConfig = _configuration.GetSection("Email:MicrosoftGraph");
            _clientId = graphConfig["ClientId"] ?? _configuration["Email:OAuth2:ClientId"] ?? "";
            _clientSecret = graphConfig["ClientSecret"] ?? _configuration["Email:OAuth2:ClientSecret"] ?? "";
            _tenantId = graphConfig["TenantId"] ?? _configuration["Email:OAuth2:TenantId"] ?? "";
            _userPrincipalName = graphConfig["UserPrincipalName"] ?? _configuration["Email:Imap:Username"] ?? "";
            _pollIntervalMinutes = graphConfig.GetValue<int>("PollIntervalMinutes", 1);
            _enabled = graphConfig.GetValue<bool>("Enabled", true);

            _logger.LogInformation("Graph Email Monitor configured for {UserPrincipalName} (Enabled: {Enabled})", 
                _userPrincipalName, _enabled);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_enabled)
            {
                _logger.LogInformation("Graph Email Monitor Service is disabled in configuration");
                return;
            }

            _logger.LogInformation("Graph Email Monitor Service starting for {UserPrincipalName}", _userPrincipalName);

            // Test connection on startup
            if (!await TestConnectionAsync())
            {
                _logger.LogError("Failed initial Graph API connection test. Service will continue trying...");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await MonitorEmails(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Graph email monitoring");
                }

                // Wait before next check
                await Task.Delay(TimeSpan.FromMinutes(_pollIntervalMinutes), stoppingToken);
            }

            _logger.LogInformation("Graph Email Monitor Service stopping");
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogError("Failed to obtain access token for Graph API");
                    return false;
                }

                var endpoint = $"https://graph.microsoft.com/v1.0/users/{_userPrincipalName}/mailFolders/Inbox";
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.GetAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Graph API connection test successful for {UserPrincipalName}", _userPrincipalName);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Graph API connection test failed: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Graph API connection test failed");
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
                    new("scope", "https://graph.microsoft.com/.default"),
                    new("grant_type", "client_credentials")
                };

                var content = new FormUrlEncodedContent(requestBody);
                var response = await _httpClient.PostAsync(tokenEndpoint, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Graph API token request failed: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                    return string.Empty;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);

                if (tokenResponse?.access_token == null)
                {
                    _logger.LogError("Graph API token response missing access_token");
                    return string.Empty;
                }

                _accessToken = tokenResponse.access_token;
                _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in - 300); // 5 min buffer

                _logger.LogDebug("Graph API access token obtained, expires at {Expiry}", _tokenExpiry);
                return _accessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to obtain Graph API access token");
                return string.Empty;
            }
        }

        public async Task<List<IncomingEmailMessage>> GetUnreadEmailsAsync()
        {
            var emails = new List<IncomingEmailMessage>();
            
            try
            {
                var accessToken = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    return emails;
                }

                // Get unread emails from inbox
                var endpoint = $"https://graph.microsoft.com/v1.0/users/{_userPrincipalName}/mailFolders/Inbox/messages" +
                              "?$filter=isRead eq false" +
                              "&$select=id,subject,from,toRecipients,ccRecipients,bccRecipients,receivedDateTime,sentDateTime,body,hasAttachments,internetMessageId" +
                              "&$top=50";

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.GetAsync(endpoint);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to retrieve unread emails: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                    return emails;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var emailsResponse = JsonSerializer.Deserialize<GraphEmailsResponse>(responseContent);

                if (emailsResponse?.value != null)
                {
                    foreach (var graphEmail in emailsResponse.value)
                    {
                        try
                        {
                            var emailMessage = await ConvertGraphEmailToIncomingEmail(graphEmail);
                            emails.Add(emailMessage);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error converting Graph email {MessageId}", graphEmail.id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unread emails from Graph API");
            }

            return emails;
        }

        public async Task MarkEmailAsReadAsync(string messageId)
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    return;
                }

                var endpoint = $"https://graph.microsoft.com/v1.0/users/{_userPrincipalName}/messages/{messageId}";
                
                var updateData = new { isRead = true };
                var jsonContent = JsonSerializer.Serialize(updateData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.PatchAsync(endpoint, content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Marked email {MessageId} as read", messageId);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to mark email as read: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking email {MessageId} as read", messageId);
            }
        }

        private async Task MonitorEmails(CancellationToken cancellationToken)
        {
            try
            {
                var unreadEmails = await GetUnreadEmailsAsync();
                
                if (unreadEmails.Count > 0)
                {
                    _logger.LogInformation("Found {Count} unread emails via Graph API", unreadEmails.Count);

                    foreach (var email in unreadEmails)
                    {
                        try
                        {
                            _logger.LogInformation("Processing email from {From} with subject '{Subject}'", 
                                email.From, email.Subject);

                            // Convert to MimeMessage for existing processing pipeline
                            var mimeMessage = await ConvertToMimeMessage(email);
                            
                            // Process the email through existing service
                            var result = await _emailProcessingService.ProcessIncomingEmailAsync(mimeMessage);
                            
                            if (result.Status != ProcessingStatus.Failed)
                            {
                                // Mark as read only if processing succeeded
                                await MarkEmailAsReadAsync(email.MessageId);
                                
                                _logger.LogInformation("Successfully processed email from {From} with task ID {TaskId}", 
                                    email.From, result.TaskId);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to process email from {From}: {Error}", 
                                    email.From, result.ErrorMessage);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing email {MessageId}", email.MessageId);
                        }
                    }
                }
                else
                {
                    _logger.LogDebug("No unread emails found via Graph API");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to monitor emails via Graph API");
            }
        }

        private async Task<IncomingEmailMessage> ConvertGraphEmailToIncomingEmail(GraphEmail graphEmail)
        {
            var emailMessage = new IncomingEmailMessage
            {
                MessageId = graphEmail.internetMessageId ?? graphEmail.id,
                From = graphEmail.from?.emailAddress?.address ?? "unknown",
                To = graphEmail.toRecipients?.Select(r => r.emailAddress.address).ToList() ?? new List<string>(),
                Cc = graphEmail.ccRecipients?.Select(r => r.emailAddress.address).ToList() ?? new List<string>(),
                Bcc = graphEmail.bccRecipients?.Select(r => r.emailAddress.address).ToList() ?? new List<string>(),
                Subject = graphEmail.subject ?? "",
                TextBody = graphEmail.body?.contentType == "text" ? graphEmail.body.content : "",
                HtmlBody = graphEmail.body?.contentType == "html" ? graphEmail.body.content : "",
                ReceivedAt = DateTime.UtcNow,
                SentAt = graphEmail.sentDateTime ?? DateTime.UtcNow,
                Headers = new Dictionary<string, string>
                {
                    ["Message-ID"] = graphEmail.internetMessageId ?? graphEmail.id,
                    ["From"] = graphEmail.from?.emailAddress?.address ?? "",
                    ["Subject"] = graphEmail.subject ?? ""
                },
                ProcessingStatus = "received",
                Attachments = new List<EmailAttachment>()
            };

            // Calculate size
            emailMessage.TotalSize = (emailMessage.TextBody?.Length ?? 0) + (emailMessage.HtmlBody?.Length ?? 0);

            // TODO: Handle attachments if needed
            if (graphEmail.hasAttachments == true)
            {
                _logger.LogDebug("Email {MessageId} has attachments (not yet implemented)", emailMessage.MessageId);
            }

            return emailMessage;
        }

        private async Task<MimeKit.MimeMessage> ConvertToMimeMessage(IncomingEmailMessage email)
        {
            var message = new MimeKit.MimeMessage();
            
            // Set basic headers
            message.MessageId = email.MessageId;
            message.Subject = email.Subject;
            message.Date = email.SentAt;

            // Set From
            if (!string.IsNullOrEmpty(email.From))
            {
                message.From.Add(new MimeKit.MailboxAddress("", email.From));
            }

            // Set To
            foreach (var to in email.To)
            {
                message.To.Add(new MimeKit.MailboxAddress("", to));
            }

            // Set body
            var bodyBuilder = new MimeKit.BodyBuilder();
            bodyBuilder.TextBody = email.TextBody;
            bodyBuilder.HtmlBody = email.HtmlBody;
            message.Body = bodyBuilder.ToMessageBody();

            return message;
        }

        private class TokenResponse
        {
            public string access_token { get; set; } = "";
            public int expires_in { get; set; }
            public string token_type { get; set; } = "";
        }

        private class GraphEmailsResponse
        {
            public List<GraphEmail> value { get; set; } = new();
        }

        private class GraphEmail
        {
            public string id { get; set; } = "";
            public string subject { get; set; } = "";
            public GraphEmailAddress from { get; set; } = new();
            public List<GraphRecipient> toRecipients { get; set; } = new();
            public List<GraphRecipient> ccRecipients { get; set; } = new();
            public List<GraphRecipient> bccRecipients { get; set; } = new();
            public DateTime? receivedDateTime { get; set; }
            public DateTime? sentDateTime { get; set; }
            public GraphEmailBody body { get; set; } = new();
            public bool? hasAttachments { get; set; }
            public string internetMessageId { get; set; } = "";
        }

        private class GraphEmailAddress
        {
            public GraphEmailAddressDetail emailAddress { get; set; } = new();
        }

        private class GraphEmailAddressDetail
        {
            public string address { get; set; } = "";
            public string name { get; set; } = "";
        }

        private class GraphRecipient
        {
            public GraphEmailAddressDetail emailAddress { get; set; } = new();
        }

        private class GraphEmailBody
        {
            public string contentType { get; set; } = "";
            public string content { get; set; } = "";
        }
    }
}