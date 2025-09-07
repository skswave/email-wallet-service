using EmailProcessingService.Models;
using EmailProcessingService.Data;
using System.Collections.Concurrent;

namespace EmailProcessingService.Services
{
    public class UserRegistrationService : IUserRegistrationService
    {
        private readonly EmailProcessingDbContext _context;
        private readonly ILogger<UserRegistrationService> _logger;
        private readonly ConcurrentDictionary<string, UserRegistration> _registrations = new();
        
        public UserRegistrationService(EmailProcessingDbContext context, ILogger<UserRegistrationService> logger)
        {
            _context = context;
            _logger = logger;
            
            // Initialize with demo data
            InitializeDemoData();
        }

        private void InitializeDemoData()
        {
            var demoRegistration = new UserRegistration
            {
                Id = 1,
                WalletAddress = "0x107C5655ce50AB9744Fc36A4e9935E30d4923d0b",
                EmailAddress = "demo@techcorp.com",
                IsVerified = true,
                IsActive = true,
                RegisteredAt = DateTime.UtcNow.AddDays(-30),
                VerifiedAt = DateTime.UtcNow.AddDays(-29),
                ProcessedEmailCount = 5,
                TotalCreditsUsed = 25,
                Settings = new UserRegistrationSettings
                {
                    AutoProcessWhitelistedEmails = false,
                    RequireExplicitAuth = true,
                    MaxEmailSize = 25 * 1024 * 1024,
                    MaxAttachmentCount = 10,
                    AllowedFileTypes = new List<string> { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".jpg", ".png" },
                    EnableVirusScanning = true,
                    NotificationEmail = "demo@techcorp.com",
                    TimeZone = "UTC"
                },
                WhitelistedDomains = new List<string> { "example.com", "techcorp.com", "rootz.global" }
            };

            _registrations.TryAdd(demoRegistration.WalletAddress.ToLowerInvariant(), demoRegistration);
            _registrations.TryAdd(demoRegistration.EmailAddress.ToLowerInvariant(), demoRegistration);
        }

        public async Task<UserRegistration?> GetRegistrationByEmailAsync(string email)
        {
            try
            {
                // Try to find existing registration
                if (_registrations.TryGetValue(email.ToLowerInvariant(), out var registration))
                {
                    _logger.LogInformation("Found registration for email: {Email}", email);
                    return await Task.FromResult(registration);
                }

                _logger.LogInformation("No registration found for email: {Email}", email);
                return await Task.FromResult<UserRegistration?>(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRegistrationByEmailAsync for email: {Email}", email);
                return null;
            }
        }

        public async Task<UserRegistration?> GetRegistrationByWalletAsync(string walletAddress)
        {
            try
            {
                // Try to find existing registration
                if (_registrations.TryGetValue(walletAddress.ToLowerInvariant(), out var registration))
                {
                    _logger.LogInformation("Found registration for wallet: {WalletAddress}", walletAddress);
                    return await Task.FromResult(registration);
                }

                _logger.LogInformation("No registration found for wallet: {WalletAddress}", walletAddress);
                return await Task.FromResult<UserRegistration?>(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRegistrationByWalletAsync for wallet: {WalletAddress}", walletAddress);
                return null;
            }
        }

        public async Task CreateRegistrationAsync(UserRegistration registration)
        {
            try
            {
                // Store by both wallet address and email for lookup
                _registrations.TryAdd(registration.WalletAddress.ToLowerInvariant(), registration);
                _registrations.TryAdd(registration.EmailAddress.ToLowerInvariant(), registration);
                
                _logger.LogInformation("Created registration for wallet {WalletAddress} and email {EmailAddress}", 
                    registration.WalletAddress, registration.EmailAddress);
                    
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating registration for wallet: {WalletAddress}", registration.WalletAddress);
                throw;
            }
        }

        public async Task UpdateRegistrationAsync(UserRegistration registration)
        {
            try
            {
                // Update both entries
                _registrations.AddOrUpdate(registration.WalletAddress.ToLowerInvariant(), registration, (key, existing) => registration);
                _registrations.AddOrUpdate(registration.EmailAddress.ToLowerInvariant(), registration, (key, existing) => registration);
                
                _logger.LogInformation("Updated registration for wallet {WalletAddress}", registration.WalletAddress);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating registration for wallet: {WalletAddress}", registration.WalletAddress);
                throw;
            }
        }

        public async Task<List<UserRegistration>> GetAllRegistrationsAsync()
        {
            try
            {
                // Return unique registrations (filter out duplicates from dual-key storage)
                var uniqueRegistrations = _registrations.Values
                    .GroupBy(r => r.WalletAddress)
                    .Select(g => g.First())
                    .OrderByDescending(r => r.RegisteredAt)
                    .ToList();
                    
                return await Task.FromResult(uniqueRegistrations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all registrations");
                return new List<UserRegistration>();
            }
        }

        public async Task<bool> ValidateCorporateAuthorizationAsync(string corporateWallet, string userWallet)
        {
            try
            {
                // MVP: Always return true for demo purposes
                _logger.LogInformation("MVP: Corporate authorization check - always returning true for {CorporateWallet} -> {UserWallet}", corporateWallet, userWallet);
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ValidateCorporateAuthorizationAsync");
                return false;
            }
        }
    }
}
