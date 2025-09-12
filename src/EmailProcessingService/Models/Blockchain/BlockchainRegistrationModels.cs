using System.ComponentModel.DataAnnotations;

namespace EmailProcessingService.Models.Blockchain
{
    /// <summary>
    /// AI_CONTEXT: Parameters for blockchain email wallet registration
    /// USAGE: Service wallet registers users on Polygon Amoy blockchain
    /// CREDENTIALS: Requires ServiceWalletPrivateKey for transaction signing
    /// </summary>
    public class BlockchainRegistrationParams
    {
        public string UserRegistrationWalletAddress { get; set; } = string.Empty;
        public string PrimaryEmailAddress { get; set; } = string.Empty;
        public List<string> AdditionalEmailAddresses { get; set; } = new();
        public string ParentCorporateWalletAddress { get; set; } = string.Empty;
        public List<string> AuthorizationTransactionHashes { get; set; } = new();
        public List<string> WhitelistedEmailDomains { get; set; } = new();
        public bool AutoProcessCCEmails { get; set; } = false;
        public decimal RegistrationFeeInWei { get; set; }
    }

    /// <summary>
    /// AI_CONTEXT: Result of blockchain email wallet registration
    /// SUCCESS_CASE: Contains transaction hash from Polygon Amoy blockchain  
    /// FAILURE_CASE: Contains error message explaining registration failure
    /// </summary>
    public class BlockchainRegistrationResult
    {
        public bool IsRegistrationSuccessful { get; set; }
        public string PolygonTransactionHash { get; set; } = string.Empty;
        public string BlockchainRegistrationId { get; set; } = string.Empty;
        public string RegistrationErrorMessage { get; set; } = string.Empty;
        public DateTime RegistrationCompletedAt { get; set; }
        public long GasUsedForRegistration { get; set; }
    }

    /// <summary>
    /// AI_CONTEXT: Request model for user registration API endpoint
    /// VALIDATION: Email and wallet address format validation required
    /// BLOCKCHAIN_FLOW: Triggers blockchain registration via service wallet
    /// </summary>
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

    /// <summary>
    /// AI_CONTEXT: Update request for existing user registration
    /// SCOPE: Modifies user preferences without blockchain transaction
    /// </summary>
    public class UpdateRegistrationRequest
    {
        public string? DisplayName { get; set; }
        public UserRegistrationSettings? Settings { get; set; }
        public List<string>? WhitelistedDomains { get; set; }
    }
}