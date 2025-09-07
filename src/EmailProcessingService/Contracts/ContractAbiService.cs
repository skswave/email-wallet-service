using System.Text.Json;

namespace EmailProcessingService.Contracts
{
    /// <summary>
    /// Production-ready contract ABI management service
    /// Handles loading and caching of contract ABIs for blockchain interactions
    /// </summary>
    public interface IContractAbiService
    {
        string GetContractAbi(string contractName);
        Task<string> GetContractAbiAsync(string contractName);
        bool IsContractSupported(string contractName);
        IEnumerable<string> GetSupportedContracts();
    }

    public class ContractAbiService : IContractAbiService
    {
        private readonly ILogger<ContractAbiService> _logger;
        private readonly Dictionary<string, string> _cachedAbis;
        private readonly string _contractsDirectory;

        public ContractAbiService(ILogger<ContractAbiService> logger)
        {
            _logger = logger;
            _cachedAbis = new Dictionary<string, string>();
            _contractsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Contracts", "abis");
            
            LoadAllAbis();
        }

        public string GetContractAbi(string contractName)
        {
            if (!_cachedAbis.TryGetValue(contractName, out var abi))
            {
                throw new ArgumentException($"Contract '{contractName}' is not supported. Available contracts: {string.Join(", ", GetSupportedContracts())}");
            }
            
            return abi;
        }

        public async Task<string> GetContractAbiAsync(string contractName)
        {
            return await Task.FromResult(GetContractAbi(contractName));
        }

        public bool IsContractSupported(string contractName)
        {
            return _cachedAbis.ContainsKey(contractName);
        }

        public IEnumerable<string> GetSupportedContracts()
        {
            return _cachedAbis.Keys;
        }

        private void LoadAllAbis()
        {
            try
            {
                // Load embedded/hardcoded ABIs for production reliability
                _cachedAbis["EmailWalletRegistration"] = GetEmailWalletRegistrationAbi();
                _cachedAbis["EmailDataWallet"] = GetEmailDataWalletAbi();
                _cachedAbis["AttachmentWallet"] = GetAttachmentWalletAbi();
                _cachedAbis["AuthorizationManager"] = GetAuthorizationManagerAbi();

                _logger.LogInformation("Loaded ABIs for {Count} contracts: {Contracts}", 
                    _cachedAbis.Count, string.Join(", ", _cachedAbis.Keys));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load contract ABIs");
                throw;
            }
        }

        /// <summary>
        /// Production ABI for EmailWalletRegistration contract
        /// Contains only the functions we actually use for better maintainability
        /// </summary>
        private string GetEmailWalletRegistrationAbi()
        {
            return @"[
                {
                    ""type"": ""function"",
                    ""name"": ""isRegistered"",
                    ""inputs"": [
                        {""name"": ""wallet"", ""type"": ""address"", ""internalType"": ""address""}
                    ],
                    ""outputs"": [
                        {""name"": """", ""type"": ""bool"", ""internalType"": ""bool""}
                    ],
                    ""stateMutability"": ""view""
                },
                {
                    ""type"": ""function"",
                    ""name"": ""getCreditBalance"",
                    ""inputs"": [
                        {""name"": ""wallet"", ""type"": ""address"", ""internalType"": ""address""}
                    ],
                    ""outputs"": [
                        {""name"": """", ""type"": ""uint256"", ""internalType"": ""uint256""}
                    ],
                    ""stateMutability"": ""view""
                },
                {
                    ""type"": ""function"",
                    ""name"": ""deductCredits"",
                    ""inputs"": [
                        {""name"": ""wallet"", ""type"": ""address"", ""internalType"": ""address""},
                        {""name"": ""amount"", ""type"": ""uint256"", ""internalType"": ""uint256""}
                    ],
                    ""outputs"": [],
                    ""stateMutability"": ""nonpayable""
                }
            ]";
        }

        /// <summary>
        /// Production ABI for EmailDataWallet contract
        /// Focuses on functions we can reliably use without complex types
        /// </summary>
        private string GetEmailDataWalletAbi()
        {
            return @"[
                {
                    ""type"": ""function"",
                    ""name"": ""updateIPFSHash"",
                    ""inputs"": [
                        {""name"": ""walletId"", ""type"": ""bytes32"", ""internalType"": ""bytes32""},
                        {""name"": ""newIpfsHash"", ""type"": ""string"", ""internalType"": ""string""}
                    ],
                    ""outputs"": [],
                    ""stateMutability"": ""nonpayable""
                },
                {
                    ""type"": ""function"",
                    ""name"": ""verifyEmailWallet"",
                    ""inputs"": [
                        {""name"": ""walletId"", ""type"": ""bytes32"", ""internalType"": ""bytes32""},
                        {""name"": ""providedHash"", ""type"": ""bytes32"", ""internalType"": ""bytes32""}
                    ],
                    ""outputs"": [
                        {""name"": """", ""type"": ""bool"", ""internalType"": ""bool""}
                    ],
                    ""stateMutability"": ""view""
                },
                {
                    ""type"": ""function"",
                    ""name"": ""walletExists"",
                    ""inputs"": [
                        {""name"": """", ""type"": ""bytes32"", ""internalType"": ""bytes32""}
                    ],
                    ""outputs"": [
                        {""name"": """", ""type"": ""bool"", ""internalType"": ""bool""}
                    ],
                    ""stateMutability"": ""view""
                },
                {
                    ""type"": ""function"",
                    ""name"": ""totalEmailWallets"",
                    ""inputs"": [],
                    ""outputs"": [
                        {""name"": """", ""type"": ""uint256"", ""internalType"": ""uint256""}
                    ],
                    ""stateMutability"": ""view""
                }
            ]";
        }

        /// <summary>
        /// Production ABI for AttachmentWallet contract
        /// </summary>
        private string GetAttachmentWalletAbi()
        {
            return @"[
                {
                    ""type"": ""function"",
                    ""name"": ""walletExists"",
                    ""inputs"": [
                        {""name"": """", ""type"": ""bytes32"", ""internalType"": ""bytes32""}
                    ],
                    ""outputs"": [
                        {""name"": """", ""type"": ""bool"", ""internalType"": ""bool""}
                    ],
                    ""stateMutability"": ""view""
                },
                {
                    ""type"": ""function"",
                    ""name"": ""totalAttachmentWallets"",
                    ""inputs"": [],
                    ""outputs"": [
                        {""name"": """", ""type"": ""uint256"", ""internalType"": ""uint256""}
                    ],
                    ""stateMutability"": ""view""
                }
            ]";
        }

        /// <summary>
        /// Production ABI for AuthorizationManager contract
        /// </summary>
        private string GetAuthorizationManagerAbi()
        {
            return @"[
                {
                    ""type"": ""function"",
                    ""name"": ""isRequestValid"",
                    ""inputs"": [
                        {""name"": ""requestId"", ""type"": ""bytes32"", ""internalType"": ""bytes32""}
                    ],
                    ""outputs"": [
                        {""name"": """", ""type"": ""bool"", ""internalType"": ""bool""}
                    ],
                    ""stateMutability"": ""view""
                },
                {
                    ""type"": ""function"",
                    ""name"": ""totalRequests"",
                    ""inputs"": [],
                    ""outputs"": [
                        {""name"": """", ""type"": ""uint256"", ""internalType"": ""uint256""}
                    ],
                    ""stateMutability"": ""view""
                }
            ]";
        }
    }
}
