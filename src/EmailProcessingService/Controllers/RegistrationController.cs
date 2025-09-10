using Microsoft.AspNetCore.Mvc;
using EmailProcessingService.Services;
using EmailProcessingService.Models;
using System.ComponentModel.DataAnnotations;

namespace EmailProcessingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegistrationController : ControllerBase
    {
        private readonly ILogger<RegistrationController> _logger;
        private readonly IUserRegistrationService _userRegistrationService;
        private readonly IBlockchainService _blockchainService;

        public RegistrationController(
            ILogger<RegistrationController> logger,
            IUserRegistrationService userRegistrationService,
            IBlockchainService blockchainService)
        {
            _logger = logger;
            _userRegistrationService = userRegistrationService;
            _blockchainService = blockchainService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser([FromBody] UserRegistrationRequest request)
        {
            try
            {
                _logger.LogInformation("Processing user registration for wallet {WalletAddress} and email {EmailAddress}", 
                    request.WalletAddress, request.EmailAddress);

                // Validate the request
                if (string.IsNullOrEmpty(request.WalletAddress) || 
                    string.IsNullOrEmpty(request.EmailAddress) ||
                    string.IsNullOrEmpty(request.DisplayName))
                {
                    return BadRequest(new { 
                        success = false,
                        message = "Missing required fields: wallet address, email, and display name are all required" 
                    });
                }

                // Validate wallet address format
                if (!IsValidEthereumAddress(request.WalletAddress))
                {
                    return BadRequest(new { 
                        success = false,
                        message = "Invalid wallet address format. Must be a valid Ethereum address starting with 0x" 
                    });
                }

                // Validate email address format
                if (!IsValidEmailAddress(request.EmailAddress))
                {
                    return BadRequest(new { 
                        success = false,
                        message = "Invalid email address format" 
                    });
                }

                // Check if user is already registered
                var existingRegistration = await _userRegistrationService.GetRegistrationByWalletAsync(request.WalletAddress);
                if (existingRegistration != null)
                {
                    _logger.LogInformation("Wallet {WalletAddress} is already registered, returning existing registration info", request.WalletAddress);
                    return Ok(new
                    {
                        success = true,
                        message = "Wallet is already registered",
                        alreadyRegistered = true,
                        transactionHash = existingRegistration.RegistrationTx,
                        walletAddress = existingRegistration.WalletAddress,
                        emailAddress = existingRegistration.EmailAddress,
                        displayName = existingRegistration.DisplayName ?? "User",
                        registeredAt = existingRegistration.RegisteredAt,
                        creditsAllocated = 60,
                        timestamp = DateTime.UtcNow
                    });
                }

                // Check if email is already registered
                var existingEmailRegistration = await _userRegistrationService.GetRegistrationByEmailAsync(request.EmailAddress);
                if (existingEmailRegistration != null)
                {
                    return Conflict(new { 
                        success = false,
                        message = "Email address is already registered with a different wallet" 
                    });
                }

                // Test blockchain connectivity and check balance
                try
                {
                    await _blockchainService.TestConnectionAsync();
                    _logger.LogInformation("Blockchain connectivity confirmed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Blockchain connectivity test failed");
                    return StatusCode(500, new { 
                        success = false,
                        message = "Blockchain service unavailable. Please try again later.",
                        error = "Unable to connect to Polygon Amoy network"
                    });
                }

                // Create user registration (local database only for now)
                var registration = new UserRegistration
                {
                    WalletAddress = request.WalletAddress,
                    EmailAddress = request.EmailAddress.ToLowerInvariant(),
                    DisplayName = request.DisplayName,
                    ParentCorporateWallet = request.CorporateWallet,
                    IsVerified = true, // For MVP, mark as verified immediately
                    VerifiedAt = DateTime.UtcNow,
                    RegisteredAt = DateTime.UtcNow,
                    IsActive = true,
                    Settings = new UserRegistrationSettings
                    {
                        AutoProcessWhitelistedEmails = false,
                        RequireExplicitAuth = true,
                        MaxEmailSize = 25 * 1024 * 1024, // 25MB
                        MaxAttachmentCount = 10,
                        AllowedFileTypes = new List<string> { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".jpg", ".png" },
                        EnableVirusScanning = true,
                        NotificationEmail = request.EmailAddress,
                        TimeZone = "UTC"
                    }
                };

                // Generate a registration transaction reference (not an actual blockchain tx for now)
                var transactionHash = $"reg_local_{DateTime.UtcNow.Ticks}_{request.WalletAddress[2..8]}";
                registration.RegistrationTx = transactionHash;

                // Store registration in local database
                await _userRegistrationService.CreateRegistrationAsync(registration);

                _logger.LogInformation("User registration successful for wallet {WalletAddress} with local transaction {TransactionHash}", 
                    request.WalletAddress, transactionHash);

                return Ok(new
                {
                    success = true,
                    message = "Registration successful! You now have 60 credits to create EMAIL_WALLETs.",
                    transactionHash = transactionHash,
                    walletAddress = request.WalletAddress,
                    emailAddress = request.EmailAddress,
                    displayName = request.DisplayName,
                    creditsAllocated = 60,
                    blockchainNetwork = "Polygon Amoy Testnet",
                    registrationType = "Local Database (Blockchain integration pending)",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration for wallet {WalletAddress}", request?.WalletAddress ?? "unknown");
                return StatusCode(500, new { 
                    success = false,
                    message = "Registration failed due to an internal error. Please try again.",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpGet("check/{walletAddress}")]
        public async Task<IActionResult> CheckRegistration(string walletAddress)
        {
            try
            {
                if (!IsValidEthereumAddress(walletAddress))
                {
                    return BadRequest(new { message = "Invalid wallet address format" });
                }

                var registration = await _userRegistrationService.GetRegistrationByWalletAsync(walletAddress);
                
                if (registration == null)
                {
                    return Ok(new { 
                        isRegistered = false,
                        walletAddress = walletAddress 
                    });
                }

                return Ok(new
                {
                    isRegistered = true,
                    walletAddress = registration.WalletAddress,
                    emailAddress = registration.EmailAddress,
                    isActive = registration.IsActive,
                    isVerified = registration.IsVerified,
                    registeredAt = registration.RegisteredAt,
                    processedEmailCount = registration.ProcessedEmailCount,
                    totalCreditsUsed = registration.TotalCreditsUsed
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking registration for wallet {WalletAddress}", walletAddress);
                return StatusCode(500, new { message = "Error checking registration" });
            }
        }

        [HttpGet("check-email/{email}")]
        public async Task<IActionResult> CheckEmailRegistration(string email)
        {
            try
            {
                if (!IsValidEmailAddress(email))
                {
                    return BadRequest(new { message = "Invalid email address format" });
                }

                var registration = await _userRegistrationService.GetRegistrationByEmailAsync(email.ToLowerInvariant());
                
                if (registration == null)
                {
                    return Ok(new { 
                        isRegistered = false,
                        emailAddress = email 
                    });
                }

                return Ok(new
                {
                    isRegistered = true,
                    walletAddress = registration.WalletAddress,
                    emailAddress = registration.EmailAddress,
                    isActive = registration.IsActive,
                    isVerified = registration.IsVerified,
                    registeredAt = registration.RegisteredAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email registration for {Email}", email);
                return StatusCode(500, new { message = "Error checking email registration" });
            }
        }

        [HttpPut("{walletAddress}")]
        public async Task<IActionResult> UpdateRegistration(string walletAddress, [FromBody] UpdateRegistrationRequest request)
        {
            try
            {
                if (!IsValidEthereumAddress(walletAddress))
                {
                    return BadRequest(new { message = "Invalid wallet address format" });
                }

                var registration = await _userRegistrationService.GetRegistrationByWalletAsync(walletAddress);
                if (registration == null)
                {
                    return NotFound(new { message = "Registration not found" });
                }

                // Update allowed fields
                if (!string.IsNullOrEmpty(request.DisplayName))
                {
                    // Update display name in settings or additional field
                }

                if (request.Settings != null)
                {
                    registration.Settings = request.Settings;
                }

                if (request.WhitelistedDomains != null)
                {
                    registration.WhitelistedDomains = request.WhitelistedDomains;
                }

                await _userRegistrationService.UpdateRegistrationAsync(registration);

                _logger.LogInformation("Registration updated for wallet {WalletAddress}", walletAddress);

                return Ok(new
                {
                    success = true,
                    message = "Registration updated successfully",
                    walletAddress = registration.WalletAddress,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating registration for wallet {WalletAddress}", walletAddress);
                return StatusCode(500, new { message = "Error updating registration" });
            }
        }

        [HttpDelete("{walletAddress}")]
        public async Task<IActionResult> DeactivateRegistration(string walletAddress)
        {
            try
            {
                if (!IsValidEthereumAddress(walletAddress))
                {
                    return BadRequest(new { message = "Invalid wallet address format" });
                }

                var registration = await _userRegistrationService.GetRegistrationByWalletAsync(walletAddress);
                if (registration == null)
                {
                    return NotFound(new { message = "Registration not found" });
                }

                registration.IsActive = false;
                await _userRegistrationService.UpdateRegistrationAsync(registration);

                _logger.LogInformation("Registration deactivated for wallet {WalletAddress}", walletAddress);

                return Ok(new
                {
                    success = true,
                    message = "Registration deactivated successfully",
                    walletAddress = walletAddress,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating registration for wallet {WalletAddress}", walletAddress);
                return StatusCode(500, new { message = "Error deactivating registration" });
            }
        }

        // Helper methods
        private bool IsValidEthereumAddress(string address)
        {
            return !string.IsNullOrEmpty(address) && 
                   address.Length == 42 && 
                   address.StartsWith("0x") &&
                   address[2..].All(c => char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
        }

        private bool IsValidEmailAddress(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }

    // Request models
    public class UserRegistrationRequest
    {
        [Required]
        public string WalletAddress { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string EmailAddress { get; set; } = string.Empty;

        [Required]
        public string DisplayName { get; set; } = string.Empty;

        public string? CorporateWallet { get; set; }

        public string NetworkId { get; set; } = "80002"; // Polygon Amoy default
    }

    public class UpdateRegistrationRequest
    {
        public string? DisplayName { get; set; }
        public UserRegistrationSettings? Settings { get; set; }
        public List<string>? WhitelistedDomains { get; set; }
    }
}