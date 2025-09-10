using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexTypes;
using EmailProcessingService.Contracts;
using EmailProcessingService.Models;

namespace EmailProcessingService.Services
{
    /// <summary>
    /// Production-ready blockchain service with proper error handling and scalability
    /// </summary>
    public interface IProductionBlockchainService
    {
        Task<bool> TestConnectionAsync();
        Task<bool> IsWalletRegisteredAsync(string walletAddress);
        Task<uint> GetCreditBalanceAsync(string walletAddress);
        Task<string> RecordEmailWalletAsync(string taskId, string ipfsHash);
        Task<bool> VerifyEmailWalletAsync(string walletId, string contentHash);
        Task<BlockchainServiceStats> GetServiceStatsAsync();
    }

    public class ProductionBlockchainService : IProductionBlockchainService
    {
        private readonly ILogger<ProductionBlockchainService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IContractAbiService _abiService;
        private readonly Web3 _web3;
        private readonly Dictionary<string, Nethereum.Contracts.Contract> _contracts;
        private readonly bool _enableRealTransactions;

        public ProductionBlockchainService(
            ILogger<ProductionBlockchainService> logger,
            IConfiguration configuration,
            IContractAbiService abiService)
        {
            _logger = logger;
            _configuration = configuration;
            _abiService = abiService;
            _contracts = new Dictionary<string, Nethereum.Contracts.Contract>();
            
            _enableRealTransactions = _configuration.GetValue<bool>("Blockchain:EnableRealTransactions", false);
            
            // Initialize Web3 connection
            _web3 = InitializeWeb3();
            
            // Initialize contracts
            InitializeContracts();
            
            _logger.LogInformation("Production blockchain service initialized. Real transactions: {Enabled}", _enableRealTransactions);
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var blockNumber = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                _logger.LogInformation("Blockchain connection test successful. Current block: {BlockNumber}", blockNumber.Value);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Blockchain connection test failed");
                return false;
            }
        }

        public async Task<bool> IsWalletRegisteredAsync(string walletAddress)
        {
            try
            {
                var contract = GetContract("EmailWalletRegistration");
                var isRegisteredFunction = contract.GetFunction("isRegistered");
                var result = await isRegisteredFunction.CallAsync<bool>(walletAddress);
                
                _logger.LogDebug("Wallet registration check for {WalletAddress}: {IsRegistered}", walletAddress, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking wallet registration for {WalletAddress}", walletAddress);
                return false;
            }
        }

        public async Task<uint> GetCreditBalanceAsync(string walletAddress)
        {
            try
            {
                var contract = GetContract("EmailWalletRegistration");
                var getCreditBalanceFunction = contract.GetFunction("getCreditBalance");
                var balance = await getCreditBalanceFunction.CallAsync<uint>(walletAddress);
                
                _logger.LogDebug("Credit balance for {WalletAddress}: {Balance}", walletAddress, balance);
                return balance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credit balance for {WalletAddress}", walletAddress);
                return 0;
            }
        }

        public async Task<string> RecordEmailWalletAsync(string taskId, string ipfsHash)
        {
            try
            {
                if (!_enableRealTransactions)
                {
                    _logger.LogInformation("Simulating blockchain transaction for task {TaskId}", taskId);
                    await Task.Delay(1000); // Simulate network delay
                    var simulatedTxHash = $"0x{Guid.NewGuid().ToString("N")}";
                    _logger.LogInformation("Simulated transaction hash: {TxHash}", simulatedTxHash);
                    return simulatedTxHash;
                }

                var contract = GetContract("EmailDataWallet");
                var updateFunction = contract.GetFunction("updateIPFSHash");
                
                // Generate wallet ID from task ID
                var walletIdBytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(taskId));
                var walletIdHex = "0x" + Convert.ToHexString(walletIdBytes).ToLower();

                _logger.LogInformation("Recording email wallet on blockchain: TaskId={TaskId}, WalletId={WalletId}, IPFS={IpfsHash}", 
                    taskId, walletIdHex, ipfsHash);

                // Get account for signing
                var account = GetSigningAccount();
                if (account == null)
                {
                    throw new InvalidOperationException("No signing account available for blockchain transactions");
                }

                // Estimate gas
                var gasEstimate = await updateFunction.EstimateGasAsync(
                    account.Address,
                    null, null, null,
                    walletIdHex,
                    ipfsHash);

                _logger.LogDebug("Gas estimate for updateIPFSHash: {GasEstimate}", gasEstimate.Value);

                // Send transaction
                var receipt = await updateFunction.SendTransactionAndWaitForReceiptAsync(
                    account.Address,
                    new HexBigInteger(gasEstimate.Value * 120 / 100), // 20% buffer
                    null, null,
                    walletIdHex,
                    ipfsHash);

                _logger.LogInformation("Blockchain transaction confirmed: Hash={TxHash}, Block={BlockNumber}, Gas={GasUsed}", 
                    receipt.TransactionHash, receipt.BlockNumber.Value, receipt.GasUsed.Value);

                return receipt.TransactionHash;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording email wallet on blockchain for task {TaskId}", taskId);
                throw;
            }
        }

        public async Task<bool> VerifyEmailWalletAsync(string walletId, string contentHash)
        {
            try
            {
                var contract = GetContract("EmailDataWallet");
                var verifyFunction = contract.GetFunction("verifyEmailWallet");
                
                var walletIdBytes = Convert.FromHexString(walletId.StartsWith("0x") ? walletId[2..] : walletId);
                var contentHashBytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(contentHash));
                
                var result = await verifyFunction.CallAsync<bool>(walletIdBytes, contentHashBytes);
                
                _logger.LogDebug("Email wallet verification: WalletId={WalletId}, ContentHash={ContentHash}, Result={Result}", 
                    walletId, contentHash, result);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email wallet: WalletId={WalletId}", walletId);
                return false;
            }
        }

        public async Task<BlockchainServiceStats> GetServiceStatsAsync()
        {
            try
            {
                var stats = new BlockchainServiceStats
                {
                    IsConnected = await TestConnectionAsync(),
                    EnableRealTransactions = _enableRealTransactions,
                    SupportedContracts = _abiService.GetSupportedContracts().ToList(),
                    NetworkId = _configuration.GetValue<int>("Blockchain:ChainId", 80002),
                    RpcUrl = _configuration["Blockchain:RpcUrl"] ?? "unknown"
                };

                // Get contract stats
                try
                {
                    var emailDataWalletContract = GetContract("EmailDataWallet");
                    var totalWalletsFunction = emailDataWalletContract.GetFunction("totalEmailWallets");
                    stats.TotalEmailWallets = await totalWalletsFunction.CallAsync<uint>();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not retrieve total email wallets count");
                    stats.TotalEmailWallets = 0;
                }

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blockchain service stats");
                return new BlockchainServiceStats
                {
                    IsConnected = false,
                    EnableRealTransactions = _enableRealTransactions,
                    SupportedContracts = new List<string>(),
                    NetworkId = 0,
                    RpcUrl = "error"
                };
            }
        }

        private Web3 InitializeWeb3()
        {
            var rpcUrl = _configuration["Blockchain:RpcUrl"];
            if (string.IsNullOrEmpty(rpcUrl))
            {
                throw new InvalidOperationException("Blockchain RPC URL not configured");
            }

            var privateKey = _configuration["Blockchain:TestWallet:PrivateKey"];
            if (!string.IsNullOrEmpty(privateKey) && privateKey != "REPLACE_WITH_YOUR_TEST_WALLET_PRIVATE_KEY")
            {
                var chainId = _configuration.GetValue<int>("Blockchain:ChainId", 80002);
                var account = new Account(privateKey, chainId);
                return new Web3(account, rpcUrl);
            }

            return new Web3(rpcUrl);
        }

        private void InitializeContracts()
        {
            var contractAddresses = _configuration.GetSection("Blockchain:ContractAddresses");
            
            foreach (var contractName in _abiService.GetSupportedContracts())
            {
                try
                {
                    var address = contractAddresses[contractName];
                    if (string.IsNullOrEmpty(address))
                    {
                        _logger.LogWarning("No address configured for contract {ContractName}", contractName);
                        continue;
                    }

                    var abi = _abiService.GetContractAbi(contractName);
                    var contract = _web3.Eth.GetContract(abi, address);
                    
                    _contracts[contractName] = contract;
                    
                    _logger.LogDebug("Initialized contract {ContractName} at {Address}", contractName, address);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize contract {ContractName}", contractName);
                }
            }

            _logger.LogInformation("Initialized {Count} blockchain contracts", _contracts.Count);
        }

        private Nethereum.Contracts.Contract GetContract(string contractName)
        {
            if (!_contracts.TryGetValue(contractName, out var contract))
            {
                throw new ArgumentException($"Contract '{contractName}' not initialized. Available contracts: {string.Join(", ", _contracts.Keys)}");
            }
            return contract;
        }

        private Account? GetSigningAccount()
        {
            var privateKey = _configuration["Blockchain:TestWallet:PrivateKey"];
            if (string.IsNullOrEmpty(privateKey) || privateKey == "REPLACE_WITH_YOUR_TEST_WALLET_PRIVATE_KEY")
            {
                return null;
            }

            var chainId = _configuration.GetValue<int>("Blockchain:ChainId", 80002);
            return new Account(privateKey, chainId);
        }
    }

    public class BlockchainServiceStats
    {
        public bool IsConnected { get; set; }
        public bool EnableRealTransactions { get; set; }
        public List<string> SupportedContracts { get; set; } = new();
        public int NetworkId { get; set; }
        public string RpcUrl { get; set; } = string.Empty;
        public uint TotalEmailWallets { get; set; }
    }
}
