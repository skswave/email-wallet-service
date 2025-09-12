using EmailProcessingService.Models;
using EmailProcessingService.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace EmailProcessingService.Services
{
    public interface IUserRegistrationService
    {
        Task<UserRegistration?> GetRegistrationByEmailAsync(string email);
        Task<UserRegistration?> GetRegistrationByWalletAsync(string walletAddress);
        Task CreateRegistrationAsync(UserRegistration registration);
        Task UpdateRegistrationAsync(UserRegistration registration);
        Task<List<UserRegistration>> GetAllRegistrationsAsync();
        Task<bool> ValidateCorporateAuthorizationAsync(string corporateWallet, string userWallet);
    }

    public class UserRegistrationService : IUserRegistrationService
    {
        private readonly EmailProcessingDbContext _context;
        private readonly ILogger<UserRegistrationService> _logger;
        private readonly ConcurrentDictionary<string, UserRegistration> _memoryCache = new();
        private readonly bool _useInMemoryDatabase;
        
        public UserRegistrationService(EmailProcessingDbContext context, ILogger<UserRegistrationService> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            
            // Check if we're using InMemory database (for MVP/testing)
            _useInMemoryDatabase = configuration.GetConnectionString("DefaultConnection")?.Contains("InMemory") ?? true;
            
            if (_useInMemoryDatabase)
            {
                _logger.LogInformation("Using in-memory database for user registration storage - NO DEMO DATA");
            }
            else
            {
                _logger.LogInformation("Using persistent database for user registration storage");
            }
            
            // PRODUCTION: NO InitializeDemoData() call - system starts clean
        }

        public async Task<UserRegistration?> GetRegistrationByEmailAsync(string email)
        {
            try
            {
                var normalizedEmail = email.ToLowerInvariant();
                
                if (_useInMemoryDatabase)
                {
                    // Try memory cache first
                    var cacheResult = _memoryCache.Values.FirstOrDefault(r => r.EmailAddress.ToLowerInvariant() == normalizedEmail);
                    if (cacheResult != null)
                    {
                        _logger.LogInformation("Found registration for email: {Email} in memory cache", email);
                        return await Task.FromResult(cacheResult);
                    }
                }
                else
                {
                    // Query persistent database
                    var dbResult = await _context.UserRegistrations
                        .Where(r => r.EmailAddress.ToLower() == normalizedEmail && r.IsActive)
                        .FirstOrDefaultAsync();
                    
                    if (dbResult != null)
                    {
                        _logger.LogInformation("Found registration for email: {Email} in database", email);
                        return dbResult;
                    }
                }

                _logger.LogInformation("No registration found for email: {Email}", email);
                return null;
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
                var normalizedWallet = walletAddress.ToLowerInvariant();
                
                if (_useInMemoryDatabase)
                {
                    // Try memory cache first
                    var cacheResult = _memoryCache.Values.FirstOrDefault(r => r.WalletAddress.ToLowerInvariant() == normalizedWallet);
                    if (cacheResult != null)
                    {
                        _logger.LogInformation("Found registration for wallet: {WalletAddress} in memory cache", walletAddress);
                        return await Task.FromResult(cacheResult);
                    }
                }
                else
                {
                    // Query persistent database
                    var dbResult = await _context.UserRegistrations
                        .Where(r => r.WalletAddress.ToLower() == normalizedWallet && r.IsActive)
                        .FirstOrDefaultAsync();
                    
                    if (dbResult != null)
                    {
                        _logger.LogInformation("Found registration for wallet: {WalletAddress} in database", walletAddress);
                        return dbResult;
                    }
                }

                _logger.LogInformation("No registration found for wallet: {WalletAddress}", walletAddress);
                return null;
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
                // Ensure normalized addresses
                registration.WalletAddress = registration.WalletAddress.ToLowerInvariant();
                registration.EmailAddress = registration.EmailAddress.ToLowerInvariant();
                
                if (_useInMemoryDatabase)
                {
                    // Store in memory cache - use wallet address as primary key to avoid conflicts
                    var cacheKey = registration.WalletAddress;
                    _memoryCache.AddOrUpdate(cacheKey, registration, (key, existing) => registration);
                }
                else
                {
                    // Store in persistent database
                    _context.UserRegistrations.Add(registration);
                    await _context.SaveChangesAsync();
                }
                
                _logger.LogInformation("Created registration for wallet {WalletAddress} and email {EmailAddress}", 
                    registration.WalletAddress, registration.EmailAddress);
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
                // Ensure normalized addresses
                registration.WalletAddress = registration.WalletAddress.ToLowerInvariant();
                registration.EmailAddress = registration.EmailAddress.ToLowerInvariant();
                
                if (_useInMemoryDatabase)
                {
                    // Update memory cache
                    var cacheKey = registration.WalletAddress;
                    _memoryCache.AddOrUpdate(cacheKey, registration, (key, existing) => registration);
                }
                else
                {
                    // Update persistent database
                    _context.UserRegistrations.Update(registration);
                    await _context.SaveChangesAsync();
                }
                
                _logger.LogInformation("Updated registration for wallet {WalletAddress}", registration.WalletAddress);
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
                if (_useInMemoryDatabase)
                {
                    // Return all registrations from memory cache
                    var registrations = _memoryCache.Values
                        .Where(r => r.IsActive)
                        .OrderByDescending(r => r.RegisteredAt)
                        .ToList();
                        
                    return await Task.FromResult(registrations);
                }
                else
                {
                    // Return all registrations from persistent database
                    var registrations = await _context.UserRegistrations
                        .Where(r => r.IsActive)
                        .OrderByDescending(r => r.RegisteredAt)
                        .ToListAsync();
                        
                    return registrations;
                }
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
                // TODO: Implement proper corporate authorization validation
                // For now, return true for MVP purposes
                _logger.LogInformation("Corporate authorization check for {CorporateWallet} -> {UserWallet} (MVP: allowing)", 
                    corporateWallet, userWallet);
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
