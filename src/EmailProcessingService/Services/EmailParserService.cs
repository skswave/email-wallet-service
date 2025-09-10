using EmailProcessingService.Models;
using MimeKit;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace EmailProcessingService.Services
{
    public interface IEmailParserService
    {
        Task<IncomingEmailMessage> ParseEmailFromMimeMessage(MimeMessage mimeMessage);
        Task<IncomingEmailMessage> ParseEmailFromRawMessage(string rawMessage);
        Task<List<EmailAttachment>> ExtractAttachments(MimeMessage mimeMessage);
        Task<string> GenerateEmailHash(IncomingEmailMessage email);
        Task<string> GenerateContentHash(byte[] content);
        Task<Dictionary<string, string>> ExtractEmailHeaders(MimeMessage mimeMessage);
        Task<string> ExtractForwardedBy(MimeMessage mimeMessage);
    }

    public class EmailParserService : IEmailParserService
    {
        private readonly ILogger<EmailParserService> _logger;
        private readonly IConfiguration _configuration;

        public EmailParserService(ILogger<EmailParserService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<IncomingEmailMessage> ParseEmailFromMimeMessage(MimeMessage mimeMessage)
        {
            try
            {
                _logger.LogInformation("Parsing email from MimeMessage: {MessageId}", mimeMessage.MessageId);

                var email = new IncomingEmailMessage
                {
                    MessageId = mimeMessage.MessageId ?? Guid.NewGuid().ToString(),
                    From = mimeMessage.From.ToString(),
                    Subject = mimeMessage.Subject ?? string.Empty,
                    ReceivedAt = DateTime.UtcNow,
                    SentAt = mimeMessage.Date.DateTime,
                    Headers = await ExtractEmailHeaders(mimeMessage),
                    ForwardedBy = await ExtractForwardedBy(mimeMessage)
                };

                // Extract recipients
                email.To = mimeMessage.To.Select(addr => addr.ToString()).ToList();
                email.Cc = mimeMessage.Cc.Select(addr => addr.ToString()).ToList();
                email.Bcc = mimeMessage.Bcc.Select(addr => addr.ToString()).ToList();

                // Extract body content
                await ExtractBodyContent(mimeMessage, email);

                // Extract attachments
                email.Attachments = await ExtractAttachments(mimeMessage);

                // Calculate sizes and hashes
                email.TotalSize = CalculateTotalSize(email);
                email.RawMessage = await SerializeToRawMessage(mimeMessage);

                _logger.LogInformation("Successfully parsed email: {Subject} from {From} with {AttachmentCount} attachments", 
                    email.Subject, email.From, email.Attachments.Count);

                return email;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing email from MimeMessage");
                throw;
            }
        }

        public async Task<IncomingEmailMessage> ParseEmailFromRawMessage(string rawMessage)
        {
            try
            {
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawMessage));
                var mimeMessage = await MimeMessage.LoadAsync(stream);
                return await ParseEmailFromMimeMessage(mimeMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing email from raw message");
                throw;
            }
        }

        public async Task<List<EmailAttachment>> ExtractAttachments(MimeMessage mimeMessage)
        {
            var attachments = new List<EmailAttachment>();
            var attachmentIndex = 0;

            try
            {
                await Task.Run(() =>
                {
                    foreach (var attachment in mimeMessage.Attachments)
                    {
                        if (attachment is MimePart mimePart)
                        {
                            var emailAttachment = new EmailAttachment
                            {
                                FileName = mimePart.FileName ?? $"attachment_{attachmentIndex}",
                                ContentType = mimePart.ContentType.MimeType,
                                ContentId = mimePart.ContentId ?? string.Empty,
                                IsInline = mimePart.ContentDisposition?.Disposition == ContentDisposition.Inline,
                                ContentDisposition = mimePart.ContentDisposition?.Disposition ?? string.Empty,
                                AttachmentIndex = attachmentIndex++
                            };

                            // Extract content
                            using var memory = new MemoryStream();
                            mimePart.Content.DecodeTo(memory);
                            emailAttachment.Content = memory.ToArray();
                            emailAttachment.Size = emailAttachment.Content.Length;

                            // Generate content hash
                            emailAttachment.ContentHash = GenerateContentHashSync(emailAttachment.Content);

                            attachments.Add(emailAttachment);

                            _logger.LogDebug("Extracted attachment: {FileName} ({Size} bytes)", 
                                emailAttachment.FileName, emailAttachment.Size);
                        }
                    }
                });

                _logger.LogInformation("Extracted {Count} attachments from email", attachments.Count);
                return attachments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting attachments");
                throw;
            }
        }

        public async Task<string> GenerateEmailHash(IncomingEmailMessage email)
        {
            try
            {
                var hashInput = string.Join("|", new[]
                {
                    email.Subject ?? string.Empty,
                    email.From ?? string.Empty,
                    string.Join(",", email.To),
                    email.TextBody ?? string.Empty,
                    email.MessageId ?? string.Empty,
                    email.ReceivedAt.ToString("O")
                });

                return await GenerateContentHash(Encoding.UTF8.GetBytes(hashInput));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating email hash");
                throw;
            }
        }

        public async Task<string> GenerateContentHash(byte[] content)
        {
            try
            {
                using var sha256 = SHA256.Create();
                var hashBytes = await Task.Run(() => sha256.ComputeHash(content));
                return "sha256:" + Convert.ToHexString(hashBytes).ToLower();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating content hash");
                throw;
            }
        }

        public async Task<Dictionary<string, string>> ExtractEmailHeaders(MimeMessage mimeMessage)
        {
            try
            {
                var headers = new Dictionary<string, string>();

                await Task.Run(() =>
                {
                    foreach (var header in mimeMessage.Headers)
                    {
                        var key = header.Field.ToString().ToLower();
                        
                        // Handle multiple headers with same name
                        if (headers.ContainsKey(key))
                        {
                            headers[key] += "; " + header.Value;
                        }
                        else
                        {
                            headers[key] = header.Value;
                        }
                    }
                });

                return headers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting email headers");
                return new Dictionary<string, string>();
            }
        }

        public async Task<string> ExtractForwardedBy(MimeMessage mimeMessage)
        {
            try
            {
                // Try to determine who forwarded this email by examining headers and content
                var forwardedBy = string.Empty;

                await Task.Run(() =>
                {
                    // Check X-Forwarded-For header
                    if (mimeMessage.Headers.Contains("X-Forwarded-For"))
                    {
                        forwardedBy = mimeMessage.Headers["X-Forwarded-For"];
                    }
                    // Check if email was forwarded based on subject line
                    else if (mimeMessage.Subject?.StartsWith("Fwd:", StringComparison.OrdinalIgnoreCase) == true ||
                             mimeMessage.Subject?.StartsWith("FW:", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        // Extract from the From field as it's likely the forwarder
                        forwardedBy = mimeMessage.From.ToString();
                    }
                    // Check body for forwarding patterns
                    else
                    {
                        var body = mimeMessage.TextBody ?? mimeMessage.HtmlBody ?? string.Empty;
                        var forwardPattern = @"From:\s*([^\r\n]+)";
                        var match = Regex.Match(body, forwardPattern, RegexOptions.IgnoreCase);
                        
                        if (match.Success)
                        {
                            // This might be a forwarded email, use the From header as forwarder
                            forwardedBy = mimeMessage.From.ToString();
                        }
                        else
                        {
                            // Default to the From address
                            forwardedBy = mimeMessage.From.ToString();
                        }
                    }
                });

                return forwardedBy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting forwarded by information");
                return mimeMessage.From.ToString(); // Fallback to From address
            }
        }

        private async Task ExtractBodyContent(MimeMessage mimeMessage, IncomingEmailMessage email)
        {
            await Task.Run(() =>
            {
                try
                {
                    // Extract text body
                    email.TextBody = mimeMessage.TextBody ?? string.Empty;
                    
                    // Extract HTML body
                    email.HtmlBody = mimeMessage.HtmlBody ?? string.Empty;
                    
                    // If no text body but HTML exists, try to extract text from HTML
                    if (string.IsNullOrEmpty(email.TextBody) && !string.IsNullOrEmpty(email.HtmlBody))
                    {
                        email.TextBody = ExtractTextFromHtml(email.HtmlBody);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error extracting body content");
                    email.TextBody = string.Empty;
                    email.HtmlBody = string.Empty;
                }
            });
        }

        private string ExtractTextFromHtml(string html)
        {
            try
            {
                // Simple HTML to text conversion - remove tags
                var text = Regex.Replace(html, "<[^>]*>", " ");
                text = Regex.Replace(text, @"\s+", " ");
                return text.Trim();
            }
            catch
            {
                return html; // Return original if conversion fails
            }
        }

        private long CalculateTotalSize(IncomingEmailMessage email)
        {
            long size = Encoding.UTF8.GetByteCount(email.TextBody ?? string.Empty);
            size += Encoding.UTF8.GetByteCount(email.HtmlBody ?? string.Empty);
            size += Encoding.UTF8.GetByteCount(email.Subject ?? string.Empty);
            size += email.Attachments.Sum(a => a.Size);
            
            return size;
        }

        private async Task<string> SerializeToRawMessage(MimeMessage mimeMessage)
        {
            try
            {
                using var stream = new MemoryStream();
                await mimeMessage.WriteToAsync(stream);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serializing MimeMessage to raw format");
                return string.Empty;
            }
        }

        private string GenerateContentHashSync(byte[] content)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(content);
            return "sha256:" + Convert.ToHexString(hashBytes).ToLower();
        }
    }
}
