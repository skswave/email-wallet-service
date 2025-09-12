using Microsoft.AspNetCore.Mvc;
using EmailProcessingService.Services;
using EmailProcessingService.Models;
using EmailProcessingService.Models.Blockchain;
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
                _logger.LogInformation("BLOCKCHAIN: Processing registration for wallet {WalletAddress} and email {EmailAddress}", 
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

                // Test blockchain connectivity first
                try
                {
                    await _blockchainService.TestConnectionAsync();
                    _logger.LogInformation("BLOCKCHAIN: Connectivity confirmed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "BLOCKCHAIN: Connectivity test failed");
                    return StatusCode(500, new { 
                        success = false,
                        message = "Blockchain service unavailable. Please try again later.",
                        error = "Unable to connect to Polygon Amoy network"
                    });
                }

                // Check if user is already registered on blockchain
                var isAlreadyRegistered = await _blockchainService.IsWalletRegisteredAsync(request.WalletAddress);
                if (isAlreadyRegistered)
                {
                    _logger.LogInformation("BLOCKCHAIN: Wallet {WalletAddress} is already registered", request.WalletAddress);
                    
                    return Ok(new
                    {
                        success = true,
                        message = "Wallet is already registered on blockchain",
                        alreadyRegistered = true,
                        walletAddress = request.WalletAddress,
                        emailAddress = request.EmailAddress,
                        displayName = request.DisplayName,
                        creditsAllocated = 60,
                        blockchainNetwork = "Polygon Amoy Testnet",
                        registrationType = "Blockchain Registration Contract",
                        timestamp = DateTime.UtcNow
                    });
                }

                // Check if email is already bound to another wallet
                var existingWallet = await _blockchainService.GetWalletFromEmailAsync(request.EmailAddress);
                if (!string.IsNullOrEmpty(existingWallet) && existingWallet != "0x0000000000000000000000000000000000000000")
                {
                    return Conflict(new { 
                        success = false,
                        message = $"Email address is already registered to wallet {existingWallet}" 
                    });
                }

                // Get registration fee from contract
                var registrationFee = await _blockchainService.GetRegistrationFeeAsync();
                _logger.LogInformation("BLOCKCHAIN: Registration fee: {RegistrationFee} wei", registrationFee);

                // Prepare registration parameters
                var registrationParams = new BlockchainRegistrationParams
                {
                    UserRegistrationWalletAddress = request.WalletAddress,
                    PrimaryEmailAddress = request.EmailAddress.ToLowerInvariant(),
                    AdditionalEmailAddresses = new List<string>(),
                    ParentCorporateWalletAddress = request.CorporateWallet ?? "0x0000000000000000000000000000000000000000",
                    AuthorizationTransactionHashes = new List<string>(),
                    WhitelistedEmailDomains = new List<string>(),
                    AutoProcessCCEmails = false,
                    RegistrationFeeInWei = registrationFee
                };

                // Register on blockchain via service wallet (owner can register for users)
                _logger.LogInformation("BLOCKCHAIN: Registering wallet {WalletAddress} with email {Email} on contract", 
                    request.WalletAddress, request.EmailAddress);

                var blockchainResult = await _blockchainService.RegisterEmailWalletAsync(registrationParams);

                if (!blockchainResult.IsRegistrationSuccessful)
                {
                    _logger.LogError("BLOCKCHAIN: Registration failed: {Error}", blockchainResult.RegistrationErrorMessage);
                    return StatusCode(500, new {
                        success = false,
                        message = "Blockchain registration failed",
                        error = blockchainResult.RegistrationErrorMessage,
                        timestamp = DateTime.UtcNow
                    });
                }

                _logger.LogInformation("BLOCKCHAIN: Registration successful for wallet {WalletAddress} with transaction {TransactionHash}", 
                    request.WalletAddress, blockchainResult.PolygonTransactionHash);

                // Store in local cache for fast lookups (optional)
                try
                {
                    var localRegistration = new UserRegistration
                    {
                        WalletAddress = request.WalletAddress,
                        EmailAddress = request.EmailAddress.ToLowerInvariant(),
                        DisplayName = request.DisplayName,
                        ParentCorporateWallet = request.CorporateWallet,
                        IsVerified = true,
                        VerifiedAt = DateTime.UtcNow,
                        RegisteredAt = DateTime.UtcNow,
                        IsActive = true,
                        RegistrationTx = blockchainResult.PolygonTransactionHash,
                        Settings = new UserRegistrationSettings
                        {
                            AutoProcessWhitelistedEmails = false,
                            RequireExplicitAuth = true,
                            MaxEmailSize = 25 * 1024 * 1024,
                            MaxAttachmentCount = 10,
                            AllowedFileTypes = new List<string> { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".jpg", ".png" },
                            EnableVirusScanning = true,
                            NotificationEmail = request.EmailAddress,
                            TimeZone = "UTC"
                        }
                    };

                    await _userRegistrationService.CreateRegistrationAsync(localRegistration);
                    _logger.LogInformation("BLOCKCHAIN: Local cache updated for wallet {WalletAddress}", request.WalletAddress);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "BLOCKCHAIN: Failed to update local cache, but blockchain registration successful");
                }

                return Ok(new
                {
                    success = true,
                    message = "Registration successful on Polygon blockchain! You now have 60 credits to create EMAIL_WALLETs.",
                    transactionHash = blockchainResult.PolygonTransactionHash,
                    walletAddress = request.WalletAddress,
                    emailAddress = request.EmailAddress,
                    displayName = request.DisplayName,
                    creditsAllocated = 60,
                    registrationFee = registrationFee,
                    blockchainNetwork = "Polygon Amoy Testnet",
                    registrationType = "Blockchain Registration Contract",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BLOCKCHAIN: Error during registration for wallet {WalletAddress}", request?.WalletAddress ?? "unknown");
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

                _logger.LogInformation("BLOCKCHAIN: Checking registration for wallet {WalletAddress}", walletAddress);

                // Check blockchain first (canonical source)
                var isRegisteredOnChain = await _blockchainService.IsWalletRegisteredAsync(walletAddress);
                
                if (!isRegisteredOnChain)
                {
                    return Ok(new { 
                        isRegistered = false,
                        walletAddress = walletAddress,
                        source = "blockchain" 
                    });
                }

                return Ok(new
                {
                    isRegistered = true,
                    walletAddress = walletAddress,
                    isActive = true,
                    isVerified = true,
                    source = "blockchain"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BLOCKCHAIN: Error checking registration for wallet {WalletAddress}", walletAddress);
                return StatusCode(500, new { message = "Error checking registration", error = ex.Message });
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

                _logger.LogInformation("BLOCKCHAIN: Checking registration for email {Email}", email);

                var normalizedEmail = email.ToLowerInvariant();
                
                // Check blockchain for email binding
                var walletAddress = await _blockchainService.GetWalletFromEmailAsync(normalizedEmail);
                
                if (string.IsNullOrEmpty(walletAddress) || walletAddress == "0x0000000000000000000000000000000000000000")
                {
                    return Ok(new { 
                        isRegistered = false,
                        emailAddress = email,
                        source = "blockchain" 
                    });
                }

                return Ok(new
                {
                    isRegistered = true,
                    walletAddress = walletAddress,
                    emailAddress = email,
                    isActive = true,
                    isVerified = true,
                    source = "blockchain"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BLOCKCHAIN: Error checking email registration for {Email}", email);
                return StatusCode(500, new { message = "Error checking email registration", error = ex.Message });
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

    // Models now imported from EmailProcessingService.Models.Blockchain namespace
}
