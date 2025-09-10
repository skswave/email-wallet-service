using EmailProcessingService.Models;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace EmailProcessingService.Services
{
    public interface IEmailValidationService
    {
        Task<EmailValidationResult> ValidateIncomingEmail(IncomingEmailMessage email);
        Task<bool> ValidateSender(string emailAddress);
        Task<UserRegistration?> GetSenderRegistration(string emailAddress);
        Task<EmailAuthenticationCheck> ValidateEmailAuthenticity(IncomingEmailMessage email);
        Task<bool> CheckWhitelistAuthorization(IncomingEmailMessage email);
        Task<bool> ValidateCorporateAuthorization(UserRegistration userRegistration, IncomingEmailMessage email);
        Task<bool> ValidateEmailSize(IncomingEmailMessage email, UserRegistrationSettings? settings = null);
        Task<List<string>> ValidateAttachments(List<EmailAttachment> attachments, UserRegistrationSettings? settings = null);
    }

    public class EmailValidationService : IEmailValidationService
    {
        private readonly ILogger<EmailValidationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IUserRegistrationService _userRegistrationService;
        private readonly IWhitelistService _whitelistService;

        public EmailValidationService(
            ILogger<EmailValidationService> logger,
            IConfiguration configuration,
            IUserRegistrationService userRegistrationService,
            IWhitelistService whitelistService)
        {
            _logger = logger;
            _configuration = configuration;
            _userRegistrationService = userRegistrationService;
            _whitelistService = whitelistService;
        }

        public async Task<EmailValidationResult> ValidateIncomingEmail(IncomingEmailMessage email)
        {
            var result = new EmailValidationResult();
            
            try
            {
                _logger.LogInformation("Validating incoming email from {From} with subject: {Subject}", 
                    email.From, email.Subject);

                // Step 1: Validate sender registration
                var senderEmail = ExtractEmailAddress(email.ForwardedBy ?? email.From);
                var senderRegistration = await GetSenderRegistration(senderEmail);
                
                if (senderRegistration != null)
                {
                    result.IsSenderRegistered = true;
                    result.RegisteredWalletAddress = senderRegistration.WalletAddress;
                    result.RegisteredWallet = senderRegistration;
                    
                    _logger.LogInformation("Sender {Email} is registered with wallet {Wallet}", 
                        senderEmail, senderRegistration.WalletAddress);
                }

                // Step 2: Check whitelist authorization if not registered
                if (!result.IsSenderRegistered)
                {
                    var originalSenderEmail = ExtractEmailAddress(email.From);
                    var isWhitelistAuthorized = await CheckWhitelistAuthorization(email);
                    result.IsWhitelistAuthorized = isWhitelistAuthorized;
                    
                    if (isWhitelistAuthorized)
                    {
                        result.WhitelistReason = $"Domain authorized for {senderEmail}";
                        _logger.LogInformation("Email from {Email} authorized via whitelist", originalSenderEmail);
                    }
                    else
                    {
                        result.ValidationErrors.Add($"Sender {originalSenderEmail} is not registered and not whitelisted");
                        _logger.LogWarning("Email from {Email} rejected: not registered or whitelisted", originalSenderEmail);
                    }
                }

                // Step 3: Validate email authenticity (SPF/DKIM/DMARC)
                var authCheck = await ValidateEmailAuthenticity(email);
                result.SPFPass = authCheck.SPFPass;
                result.DKIMValid = authCheck.DKIMValid;
                result.DMARCPass = authCheck.DMARCPass;
                
                if (!authCheck.IsFullyAuthenticated)
                {
                    result.SecurityWarnings.Add($"Email authentication incomplete (Score: {authCheck.AuthenticationScore}/3)");
                    _logger.LogWarning("Email authentication incomplete for {Email}: SPF={SPF}, DKIM={DKIM}, DMARC={DMARC}", 
                        email.From, authCheck.SPFPass, authCheck.DKIMValid, authCheck.DMARCPass);
                }

                // Step 4: Check corporate authorization if applicable
                if (result.RegisteredWallet?.ParentCorporateWallet != null)
                {
                    result.CorporateAuthValid = await ValidateCorporateAuthorization(result.RegisteredWallet, email);
                    
                    if (!result.CorporateAuthValid)
                    {
                        result.ValidationErrors.Add("Corporate authorization required but not valid");
                        _logger.LogWarning("Corporate authorization failed for {Email}", senderEmail);
                    }
                }

                // Step 5: Validate email size limits
                var settings = result.RegisteredWallet?.Settings;
                if (!await ValidateEmailSize(email, settings))
                {
                    result.ValidationErrors.Add($"Email size exceeds limit: {email.TotalSize} bytes");
                    _logger.LogWarning("Email size validation failed: {Size} bytes", email.TotalSize);
                }

                // Step 6: Validate attachments
                var attachmentErrors = await ValidateAttachments(email.Attachments, settings);
                result.ValidationErrors.AddRange(attachmentErrors);

                _logger.LogInformation("Email validation completed for {Email}: Valid={Valid}, Errors={ErrorCount}, Warnings={WarningCount}", 
                    email.From, result.IsValid, result.ValidationErrors.Count, result.SecurityWarnings.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating incoming email from {From}", email.From);
                result.ValidationErrors.Add($"Validation error: {ex.Message}");
                return result;
            }
        }

        public async Task<bool> ValidateSender(string emailAddress)
        {
            try
            {
                var registration = await GetSenderRegistration(emailAddress);
                return registration != null && registration.IsActive && registration.IsVerified;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating sender {Email}", emailAddress);
                return false;
            }
        }

        public async Task<UserRegistration?> GetSenderRegistration(string emailAddress)
        {
            try
            {
                var cleanEmail = ExtractEmailAddress(emailAddress).ToLowerInvariant();
                return await _userRegistrationService.GetRegistrationByEmailAsync(cleanEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sender registration for {Email}", emailAddress);
                return null;
            }
        }

        public async Task<EmailAuthenticationCheck> ValidateEmailAuthenticity(IncomingEmailMessage email)
        {
            var authCheck = new EmailAuthenticationCheck();
            
            try
            {
                // Extract authentication headers
                var authResults = email.Headers.Where(h => 
                    h.Key.Contains("authentication-results") || 
                    h.Key.Contains("received-spf") ||
                    h.Key.Contains("dkim-signature") ||
                    h.Key.Contains("arc-authentication-results")).ToList();

                // Parse SPF
                authCheck.SPFPass = await ParseSPFResult(email.Headers);
                
                // Parse DKIM
                authCheck.DKIMValid = await ParseDKIMResult(email.Headers);
                
                // Parse DMARC
                authCheck.DMARCPass = await ParseDMARCResult(email.Headers);

                // Add warnings for failed authentication
                if (!authCheck.SPFPass)
                    authCheck.AuthenticationWarnings.Add("SPF validation failed or missing");
                
                if (!authCheck.DKIMValid)
                    authCheck.AuthenticationWarnings.Add("DKIM validation failed or missing");
                
                if (!authCheck.DMARCPass)
                    authCheck.AuthenticationWarnings.Add("DMARC validation failed or missing");

                _logger.LogDebug("Email authentication check for {Email}: SPF={SPF}, DKIM={DKIM}, DMARC={DMARC}", 
                    email.From, authCheck.SPFPass, authCheck.DKIMValid, authCheck.DMARCPass);

                return authCheck;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating email authenticity for {Email}", email.From);
                authCheck.AuthenticationWarnings.Add($"Authentication validation error: {ex.Message}");
                return authCheck;
            }
        }

        public async Task<bool> CheckWhitelistAuthorization(IncomingEmailMessage email)
        {
            try
            {
                var forwarderEmail = ExtractEmailAddress(email.ForwardedBy ?? email.From);
                var originalSenderEmail = ExtractEmailAddress(email.From);
                
                // Get the user registration for the forwarder (the person sending to our service)
                var forwarderRegistration = await GetSenderRegistration(forwarderEmail);
                
                if (forwarderRegistration == null)
                {
                    _logger.LogDebug("No registration found for forwarder {Email}", forwarderEmail);
                    return false;
                }

                // Check if the original sender's domain/email is whitelisted for this user
                var isWhitelisted = await _whitelistService.IsEmailWhitelistedForUser(
                    forwarderRegistration.WalletAddress, originalSenderEmail);

                if (isWhitelisted)
                {
                    _logger.LogInformation("Original sender {OriginalSender} is whitelisted for user {Forwarder}", 
                        originalSenderEmail, forwarderEmail);
                }

                return isWhitelisted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking whitelist authorization for email");
                return false;
            }
        }

        public async Task<bool> ValidateCorporateAuthorization(UserRegistration userRegistration, IncomingEmailMessage email)
        {
            try
            {
                if (string.IsNullOrEmpty(userRegistration.ParentCorporateWallet))
                {
                    return true; // No corporate authorization required
                }

                // Check if corporate wallet has authorized this user for email processing
                // This would typically involve checking the blockchain or a corporate authorization service
                var corporateAuth = await _userRegistrationService.ValidateCorporateAuthorizationAsync(
                    userRegistration.ParentCorporateWallet, userRegistration.WalletAddress);

                _logger.LogDebug("Corporate authorization check for user {User} under corporate {Corporate}: {Result}", 
                    userRegistration.WalletAddress, userRegistration.ParentCorporateWallet, corporateAuth);

                return corporateAuth;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating corporate authorization");
                return false;
            }
        }

        public async Task<bool> ValidateEmailSize(IncomingEmailMessage email, UserRegistrationSettings? settings = null)
        {
            try
            {
                var maxSize = settings?.MaxEmailSize ?? GetDefaultMaxEmailSize();
                var isValid = email.TotalSize <= maxSize;
                
                if (!isValid)
                {
                    _logger.LogWarning("Email size validation failed: {Size} > {MaxSize}", email.TotalSize, maxSize);
                }

                return await Task.FromResult(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating email size");
                return false;
            }
        }

        public async Task<List<string>> ValidateAttachments(List<EmailAttachment> attachments, UserRegistrationSettings? settings = null)
        {
            var errors = new List<string>();
            
            try
            {
                var maxCount = settings?.MaxAttachmentCount ?? GetDefaultMaxAttachmentCount();
                var allowedTypes = settings?.AllowedFileTypes ?? GetDefaultAllowedFileTypes();

                if (attachments.Count > maxCount)
                {
                    errors.Add($"Too many attachments: {attachments.Count} > {maxCount}");
                }

                foreach (var attachment in attachments)
                {
                    // Validate file type
                    var extension = Path.GetExtension(attachment.FileName).ToLowerInvariant();
                    if (!allowedTypes.Contains(extension))
                    {
                        errors.Add($"File type not allowed: {attachment.FileName} ({extension})");
                    }

                    // Validate file size (individual attachment limit)
                    var maxAttachmentSize = GetMaxAttachmentSize();
                    if (attachment.Size > maxAttachmentSize)
                    {
                        errors.Add($"Attachment too large: {attachment.FileName} ({attachment.Size} bytes)");
                    }

                    // Basic file signature validation
                    if (!await ValidateFileSignature(attachment))
                    {
                        errors.Add($"Invalid file signature: {attachment.FileName}");
                    }
                }

                return errors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating attachments");
                errors.Add($"Attachment validation error: {ex.Message}");
                return errors;
            }
        }

        private async Task<bool> ParseSPFResult(Dictionary<string, string> headers)
        {
            return await Task.Run(() =>
            {
                var spfHeaders = headers.Where(h => 
                    h.Key.Contains("received-spf") || 
                    h.Value.ToLower().Contains("spf=pass")).ToList();
                
                return spfHeaders.Any(h => h.Value.ToLower().Contains("pass"));
            });
        }

        private async Task<bool> ParseDKIMResult(Dictionary<string, string> headers)
        {
            return await Task.Run(() =>
            {
                var dkimHeaders = headers.Where(h => 
                    h.Key.Contains("dkim") || 
                    h.Value.ToLower().Contains("dkim=pass")).ToList();
                
                return dkimHeaders.Any(h => h.Value.ToLower().Contains("pass"));
            });
        }

        private async Task<bool> ParseDMARCResult(Dictionary<string, string> headers)
        {
            return await Task.Run(() =>
            {
                var dmarcHeaders = headers.Where(h => 
                    h.Value.ToLower().Contains("dmarc=pass")).ToList();
                
                return dmarcHeaders.Any(h => h.Value.ToLower().Contains("pass"));
            });
        }

        private async Task<bool> ValidateFileSignature(EmailAttachment attachment)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (attachment.Content.Length < 4)
                        return false;

                    var signature = BitConverter.ToUInt32(attachment.Content.Take(4).ToArray());
                    var extension = Path.GetExtension(attachment.FileName).ToLowerInvariant();

                    // Basic file signature validation
                    return extension switch
                    {
                        ".pdf" => attachment.Content.Take(4).SequenceEqual(new byte[] { 0x25, 0x50, 0x44, 0x46 }), // %PDF
                        ".jpg" or ".jpeg" => attachment.Content.Take(3).SequenceEqual(new byte[] { 0xFF, 0xD8, 0xFF }),
                        ".png" => attachment.Content.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
                        ".zip" => attachment.Content.Take(4).SequenceEqual(new byte[] { 0x50, 0x4B, 0x03, 0x04 }),
                        _ => true // Allow other types without signature validation
                    };
                }
                catch
                {
                    return false;
                }
            });
        }

        private string ExtractEmailAddress(string emailString)
        {
            if (string.IsNullOrEmpty(emailString))
                return string.Empty;

            // Extract email from format like "Name <email@domain.com>" or just "email@domain.com"
            var emailMatch = Regex.Match(emailString, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
            return emailMatch.Success ? emailMatch.Value : emailString.Trim();
        }

        private int GetDefaultMaxEmailSize() => 
            _configuration.GetValue<int>("EmailProcessing:MaxEmailSize", 25 * 1024 * 1024); // 25MB

        private int GetDefaultMaxAttachmentCount() => 
            _configuration.GetValue<int>("EmailProcessing:MaxAttachmentCount", 10);

        private List<string> GetDefaultAllowedFileTypes() => 
            _configuration.GetSection("EmailProcessing:AllowedFileTypes").Get<List<string>>() ?? 
            new List<string> { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".jpg", ".png" };

        private long GetMaxAttachmentSize() => 
            _configuration.GetValue<long>("EmailProcessing:MaxAttachmentSize", 10 * 1024 * 1024); // 10MB per attachment
    }

    public interface IWhitelistService
    {
        Task<bool> IsEmailWhitelistedForUser(string userWallet, string email);
    }
}
