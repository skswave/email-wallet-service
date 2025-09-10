using EmailProcessingService.Models;
using EmailProcessingService.Utils;
using Microsoft.Extensions.Options;
using Nethereum.Web3;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.Util;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using Newtonsoft.Json;
using System.Numerics;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using Nethereum.Web3.Accounts;

namespace EmailProcessingService.Services
{
    public interface IBlockchainService
    {
        Task<WalletRegistrationResult> RegisterEmailWalletAsync(WalletRegistrationRequest request);
        Task<ContractCallResult> StoreEmailDataAsync(EmailDataStorageRequest request);
        Task<ContractCallResult> StoreAttachmentDataAsync(AttachmentStorageRequest request);
        Task<AuthorizationResponse> RequestAuthorizationAsync(BlockchainAuthorizationRequest request);
        Task<bool> ValidateAuthorizationAsync(string walletAddress, string resourceId, AuthorizationType type);
        Task<CreditBalance> GetCreditBalanceAsync(string walletAddress);
        Task<ContractCallResult> DeductCreditsAsync(CreditOperation operation);
        Task<DataVerificationResult> VerifyDataAsync(DataVerificationRequest request);
        Task<bool> IsWalletRegisteredAsync(string walletAddress);
        Task<List<BlockchainEvent>> GetWalletEventsAsync(string walletAddress, DateTime? fromDate = null);
        Task<string> GetTransactionStatusAsync(string transactionHash);
        Task<bool> TestConnectionAsync();
    }

    public class BlockchainService : IBlockchainService
    {
        private readonly ILogger<BlockchainService> _logger;
        private readonly BlockchainConfiguration _config;
        private readonly Web3 _web3;
        private readonly Contract _registrationContract;
        private readonly Contract _emailDataContract;
        private readonly Contract _attachmentContract;
        private readonly Contract _authManagerContract;

        private readonly Account _testAccount;

        public BlockchainService(ILogger<BlockchainService> logger, IOptions<BlockchainConfiguration> config)
        {
            _logger = logger;
            _config = config.Value;

            try
            {
                // Initialize test account if configured
                if (_config.EnableRealTransactions && !string.IsNullOrEmpty(_config.TestWallet?.PrivateKey))
                {
                    _testAccount = new Account(_config.TestWallet.PrivateKey);
                    _logger.LogInformation("Test account initialized: {Address}", _testAccount.Address);
                    
                    // Initialize Web3 with test account
                    _web3 = new Web3(_testAccount, _config.RpcUrl);
                    _logger.LogInformation("Web3 initialized with test account for real transactions");
                }
                else
                {
                    // Initialize Web3 without account (read-only)
                    _web3 = new Web3(_config.RpcUrl);
                    _logger.LogInformation("Web3 initialized in read-only mode");
                }

                // Load contract ABIs and initialize contracts
                _registrationContract = InitializeContract("EmailWalletRegistration", _config.ContractAddresses.EmailWalletRegistration);
                _emailDataContract = InitializeContract("EmailDataWallet", _config.ContractAddresses.EmailDataWallet);
                _attachmentContract = InitializeContract("AttachmentWallet", _config.ContractAddresses.AttachmentWallet);
                _authManagerContract = InitializeContract("AuthorizationManager", _config.ContractAddresses.AuthorizationManager);

                _logger.LogInformation("Blockchain service initialized for network {Network} (Chain ID: {ChainId})", 
                    _config.Network, _config.ChainId);
                    
                if (_testAccount != null)
                {
                    _logger.LogInformation("Real transactions enabled with test wallet: {Address}", _testAccount.Address);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize blockchain service");
                throw;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                _logger.LogInformation("Testing blockchain connection...");
                
                // Test basic connection
                var latestBlock = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                _logger.LogInformation("Latest block number: {BlockNumber}", latestBlock.Value);

                // Test contract calls
                if (_registrationContract != null)
                {
                    var registrationFeeFunction = _registrationContract.GetFunction("registrationFee");
                    var fee = await registrationFeeFunction.CallAsync<BigInteger>();
                    _logger.LogInformation("Registration fee: {Fee} wei", fee);
                }

                if (_emailDataContract != null)
                {
                    var totalWalletsFunction = _emailDataContract.GetFunction("totalEmailWallets");
                    var totalWallets = await totalWalletsFunction.CallAsync<BigInteger>();
                    _logger.LogInformation("Total email wallets: {Count}", totalWallets);
                }

                _logger.LogInformation("Blockchain connection test successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Blockchain connection test failed");
                return false;
            }
        }

        public async Task<WalletRegistrationResult> RegisterEmailWalletAsync(WalletRegistrationRequest request)
        {
            try
            {
                _logger.LogInformation("Registering email wallet for {WalletAddress} with primary email hash", 
                    request.WalletAddress);

                if (_testAccount == null)
                {
                    _logger.LogError("Cannot perform registration: No test account configured");
                    return new WalletRegistrationResult
                    {
                        Success = false,
                        ErrorMessage = "Real transactions not enabled or test account not configured"
                    };
                }

                // Convert registration fee to Wei
                var registrationFeeWei = Web3.Convert.ToWei(decimal.Parse(request.RegistrationFee));

                // Prepare function call
                var registerFunction = _registrationContract.GetFunction("registerEmailWallet");
                
                // For now, use SHA256 instead of Keccak for email hashing
                // In production, you should use proper Keccak256 hashing
                using var sha256 = SHA256.Create();
                var primaryEmailBytes = Encoding.UTF8.GetBytes(request.PrimaryEmail);
                var primaryEmailHash = sha256.ComputeHash(primaryEmailBytes);
                
                // Convert additional emails to hashes
                var additionalEmailHashes = request.AdditionalEmails
                    .Select(email => sha256.ComputeHash(Encoding.UTF8.GetBytes(email)))
                    .ToArray();

                _logger.LogInformation("Executing wallet registration transaction with test account: {Address}", 
                    _testAccount.Address);

                // Estimate gas
                var gasEstimate = await registerFunction.EstimateGasAsync(
                    new object[]
                    {
                        request.PrimaryEmail,
                        request.AdditionalEmails.ToArray(),
                        request.ParentCorporateWallet ?? "0x0000000000000000000000000000000000000000",
                        request.AuthorizationTxs.Select(tx => Encoding.UTF8.GetBytes(tx)).ToArray(),
                        request.WhitelistedDomains.ToArray(),
                        request.AutoProcessCC
                    });

                _logger.LogInformation("Gas estimate for registration: {GasEstimate}", gasEstimate.Value);

                // Execute transaction with test account
                var receipt = await registerFunction.SendTransactionAndWaitForReceiptAsync(
                    _testAccount.Address,
                    new HexBigInteger(gasEstimate.Value * new BigInteger(_config.GasLimitMultiplier)),
                    new HexBigInteger(registrationFeeWei),
                    null,
                    request.PrimaryEmail,
                    request.AdditionalEmails.ToArray(),
                    request.ParentCorporateWallet ?? "0x0000000000000000000000000000000000000000",
                    request.AuthorizationTxs.Select(tx => Encoding.UTF8.GetBytes(tx)).ToArray(),
                    request.WhitelistedDomains.ToArray(),
                    request.AutoProcessCC);

                // Extract registration ID from event logs
                var registrationId = ExtractRegistrationIdFromReceipt(receipt);

                _logger.LogInformation("Email wallet registered successfully. Registration ID: {RegistrationId}, TX: {TxHash}",
                    registrationId, receipt.TransactionHash);

                return new WalletRegistrationResult
                {
                    Success = true,
                    RegistrationId = registrationId,
                    TransactionHash = receipt.TransactionHash,
                    RegisteredAt = DateTime.UtcNow,
                    GasUsed = (long)receipt.GasUsed.Value
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering email wallet for {WalletAddress}", request.WalletAddress);
                return new WalletRegistrationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<ContractCallResult> StoreEmailDataAsync(EmailDataStorageRequest request)
        {
            try
            {
                _logger.LogInformation("Storing email data for wallet {EmailWalletId}", request.EmailWalletId);

                var storeFunction = _emailDataContract.GetFunction("storeEmailData");

                var gasEstimate = await storeFunction.EstimateGasAsync(
                    request.WalletAddress,
                    new object[]
                    {
                        request.EmailWalletId,
                        request.DataHash,
                        request.IpfsHash,
                        request.MetadataHash,
                        request.DataSize,
                        request.AttachmentWalletIds.ToArray()
                    });

                var receipt = await storeFunction.SendTransactionAndWaitForReceiptAsync(
                    request.WalletAddress,
                    new HexBigInteger(gasEstimate.Value * new BigInteger(_config.GasLimitMultiplier)),
                    null,
                    null,
                    request.EmailWalletId,
                    request.DataHash,
                    request.IpfsHash,
                    request.MetadataHash,
                    request.DataSize,
                    request.AttachmentWalletIds.ToArray());

                _logger.LogInformation("Email data stored successfully. TX: {TxHash}", receipt.TransactionHash);

                return new ContractCallResult
                {
                    Success = true,
                    TransactionHash = receipt.TransactionHash,
                    GasUsed = (long)receipt.GasUsed.Value
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing email data for {EmailWalletId}", request.EmailWalletId);
                return new ContractCallResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<ContractCallResult> StoreAttachmentDataAsync(AttachmentStorageRequest request)
        {
            try
            {
                _logger.LogInformation("Storing attachment data for wallet {AttachmentWalletId}", request.AttachmentWalletId);

                var storeFunction = _attachmentContract.GetFunction("storeAttachment");

                var gasEstimate = await storeFunction.EstimateGasAsync(
                    request.WalletAddress,
                    new object[]
                    {
                        request.AttachmentWalletId,
                        request.FileHash,
                        request.IpfsHash,
                        request.FileName,
                        request.FileType,
                        request.FileSize,
                        request.ParentEmailWalletId
                    });

                var receipt = await storeFunction.SendTransactionAndWaitForReceiptAsync(
                    request.WalletAddress,
                    new HexBigInteger(gasEstimate.Value * new BigInteger(_config.GasLimitMultiplier)),
                    null,
                    null,
                    request.AttachmentWalletId,
                    request.FileHash,
                    request.IpfsHash,
                    request.FileName,
                    request.FileType,
                    request.FileSize,
                    request.ParentEmailWalletId);

                _logger.LogInformation("Attachment data stored successfully. TX: {TxHash}", receipt.TransactionHash);

                return new ContractCallResult
                {
                    Success = true,
                    TransactionHash = receipt.TransactionHash,
                    GasUsed = (long)receipt.GasUsed.Value
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing attachment data for {AttachmentWalletId}", request.AttachmentWalletId);
                return new ContractCallResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<AuthorizationResponse> RequestAuthorizationAsync(BlockchainAuthorizationRequest request)
        {
            try
            {
                _logger.LogInformation("Requesting authorization from {RequesterWallet} for resource {ResourceId}",
                    request.RequesterWallet, request.ResourceId);

                var requestFunction = _authManagerContract.GetFunction("requestAccess");

                var gasEstimate = await requestFunction.EstimateGasAsync(
                    request.RequesterWallet,
                    new object[]
                    {
                        request.TargetWallet,
                        request.ResourceId,
                        (int)request.AuthorizationType,
                        request.Reason,
                        request.ExpiresAt?.Ticks ?? 0
                    });

                var receipt = await requestFunction.SendTransactionAndWaitForReceiptAsync(
                    request.RequesterWallet,
                    new HexBigInteger(gasEstimate.Value * new BigInteger(_config.GasLimitMultiplier)),
                    null,
                    null,
                    request.TargetWallet,
                    request.ResourceId,
                    (int)request.AuthorizationType,
                    request.Reason,
                    request.ExpiresAt?.Ticks ?? 0);

                var authorizationId = ExtractAuthorizationIdFromReceipt(receipt);

                _logger.LogInformation("Authorization requested successfully. Auth ID: {AuthId}, TX: {TxHash}",
                    authorizationId, receipt.TransactionHash);

                return new AuthorizationResponse
                {
                    Authorized = true,
                    AuthorizationId = authorizationId,
                    TransactionHash = receipt.TransactionHash,
                    AuthorizedAt = DateTime.UtcNow,
                    ExpiresAt = request.ExpiresAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting authorization for {ResourceId}", request.ResourceId);
                return new AuthorizationResponse
                {
                    Authorized = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<bool> ValidateAuthorizationAsync(string walletAddress, string resourceId, AuthorizationType type)
        {
            try
            {
                var validateFunction = _authManagerContract.GetFunction("hasAccess");
                var result = await validateFunction.CallAsync<bool>(walletAddress, resourceId, (int)type);

                _logger.LogDebug("Authorization validation for {WalletAddress} on {ResourceId}: {Result}",
                    walletAddress, resourceId, result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating authorization for {WalletAddress} on {ResourceId}",
                    walletAddress, resourceId);
                return false;
            }
        }

        public async Task<CreditBalance> GetCreditBalanceAsync(string walletAddress)
        {
            try
            {
                _logger.LogInformation("Getting balance for wallet {WalletAddress}", walletAddress);
                
                // Get native POL balance first
                var nativeBalance = await _web3.Eth.GetBalance.SendRequestAsync(walletAddress);
                var polBalance = Web3.Convert.FromWei(nativeBalance.Value);
                
                _logger.LogInformation("Native POL balance for {WalletAddress}: {Balance} POL", 
                    walletAddress, polBalance);
                
                // Try to get contract credits balance (if available)
                long contractCredits = 0;
                try
                {
                    var balanceFunction = _registrationContract.GetFunction("getCreditBalance");
                    var contractBalance = await balanceFunction.CallAsync<BigInteger>(walletAddress);
                    contractCredits = (long)contractBalance;
                    _logger.LogInformation("Contract credits for {WalletAddress}: {Credits}", 
                        walletAddress, contractCredits);
                }
                catch (Exception contractEx)
                {
                    _logger.LogWarning(contractEx, "Could not get contract credits for {WalletAddress}, using POL balance only", 
                        walletAddress);
                }

                return new CreditBalance
                {
                    WalletAddress = walletAddress,
                    Balance = contractCredits,
                    NativeBalance = polBalance,
                    LastUpdated = DateTime.UtcNow,
                    RecentTransactions = new List<CreditTransaction>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credit balance for {WalletAddress}", walletAddress);
                throw;
            }
        }

        public async Task<ContractCallResult> DeductCreditsAsync(CreditOperation operation)
        {
            try
            {
                var deductFunction = _registrationContract.GetFunction("deductCredits");

                var gasEstimate = await deductFunction.EstimateGasAsync(
                    operation.WalletAddress,
                    new object[] { operation.WalletAddress, operation.Amount });

                var receipt = await deductFunction.SendTransactionAndWaitForReceiptAsync(
                    operation.WalletAddress,
                    new HexBigInteger(gasEstimate.Value * new BigInteger(_config.GasLimitMultiplier)),
                    null,
                    null,
                    operation.WalletAddress,
                    operation.Amount);

                return new ContractCallResult
                {
                    Success = true,
                    TransactionHash = receipt.TransactionHash,
                    GasUsed = (long)receipt.GasUsed.Value
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deducting credits for {WalletAddress}", operation.WalletAddress);
                return new ContractCallResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<DataVerificationResult> VerifyDataAsync(DataVerificationRequest request)
        {
            try
            {
                Contract contract = request.WalletType == WalletType.EMAIL_CONTAINER 
                    ? _emailDataContract 
                    : _attachmentContract;

                var verifyFunction = contract.GetFunction("verifyData");
                var result = await verifyFunction.CallAsync<bool>(request.WalletId, request.DataHash);

                return new DataVerificationResult
                {
                    IsValid = result,
                    OnChainMatch = result,
                    StoredHash = request.DataHash,
                    ComputedHash = request.DataHash,
                    VerifiedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying data for {WalletId}", request.WalletId);
                return new DataVerificationResult
                {
                    IsValid = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<bool> IsWalletRegisteredAsync(string walletAddress)
        {
            try
            {
                var isRegisteredFunction = _registrationContract.GetFunction("isRegistered");
                return await isRegisteredFunction.CallAsync<bool>(walletAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking wallet registration for {WalletAddress}", walletAddress);
                return false;
            }
        }

        public async Task<List<BlockchainEvent>> GetWalletEventsAsync(string walletAddress, DateTime? fromDate = null)
        {
            try
            {
                var events = new List<BlockchainEvent>();
                var fromBlock = fromDate.HasValue ? await GetBlockNumberFromDate(fromDate.Value) : 0;

                // Get registration events
                var regEvents = await GetEventLogs(_registrationContract, "EmailWalletRegistered", 
                    fromBlock, walletAddress);
                events.AddRange(regEvents);

                // Get email data events
                var emailEvents = await GetEventLogs(_emailDataContract, "EmailDataStored", 
                    fromBlock, walletAddress);
                events.AddRange(emailEvents);

                // Get attachment events
                var attachEvents = await GetEventLogs(_attachmentContract, "AttachmentStored", 
                    fromBlock, walletAddress);
                events.AddRange(attachEvents);

                return events.OrderByDescending(e => e.BlockNumber).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wallet events for {WalletAddress}", walletAddress);
                return new List<BlockchainEvent>();
            }
        }

        public async Task<string> GetTransactionStatusAsync(string transactionHash)
        {
            try
            {
                var receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                
                if (receipt == null)
                    return "Pending";
                
                return receipt.Status.Value == 1 ? "Confirmed" : "Failed";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction status for {TxHash}", transactionHash);
                return "Unknown";
            }
        }

        // Private helper methods
        private Contract InitializeContract(string contractName, string contractAddress)
        {
            try
            {
                _logger.LogInformation("Initializing contract {ContractName} at {Address}", contractName, contractAddress);
                
                // Load ABI from file
                var abiPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "abis", $"{contractName}.json");
                _logger.LogDebug("Looking for ABI file at: {AbiPath}", abiPath);
                
                string abi;
                if (File.Exists(abiPath))
                {
                    var abiContent = File.ReadAllText(abiPath);
                    _logger.LogDebug("ABI file found, content length: {Length} chars", abiContent.Length);
                    
                    // Always use the basic ABI for now to avoid parsing errors
                    // TODO: Implement proper Hardhat ABI conversion in production
                    _logger.LogInformation("Using basic ABI for {ContractName} to avoid Hardhat parsing issues", contractName);
                    abi = GetBasicContractAbi(contractName);
                }
                else
                {
                    // Fallback ABI for basic functions
                    _logger.LogWarning("ABI file not found for {ContractName} at {AbiPath}, using basic ABI", contractName, abiPath);
                    abi = GetBasicContractAbi(contractName);
                }

                _logger.LogDebug("Creating Nethereum contract with ABI length: {Length}", abi.Length);
                var contract = _web3.Eth.GetContract(abi, contractAddress);
                _logger.LogInformation("Successfully initialized contract {ContractName} at {Address}", 
                    contractName, contractAddress);
                
                return contract;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing contract {ContractName} at {Address}", 
                    contractName, contractAddress);
                throw;
            }
        }

        private string GetBasicContractAbi(string contractName)
        {
            // Return minimal ABI for basic operations with essential functions
            return contractName switch
            {
                "EmailWalletRegistration" => """
                [
                    {
                        "type": "function",
                        "name": "registrationFee",
                        "inputs": [],
                        "outputs": [{"name": "", "type": "uint256", "internalType": "uint256"}],
                        "stateMutability": "view"
                    },
                    {
                        "type": "function",
                        "name": "isRegistered",
                        "inputs": [{"name": "wallet", "type": "address", "internalType": "address"}],
                        "outputs": [{"name": "", "type": "bool", "internalType": "bool"}],
                        "stateMutability": "view"
                    },
                    {
                        "type": "function",
                        "name": "getCreditBalance",
                        "inputs": [{"name": "wallet", "type": "address", "internalType": "address"}],
                        "outputs": [{"name": "", "type": "uint256", "internalType": "uint256"}],
                        "stateMutability": "view"
                    },
                    {
                        "type": "function",
                        "name": "totalRegistrations",
                        "inputs": [],
                        "outputs": [{"name": "", "type": "uint256", "internalType": "uint256"}],
                        "stateMutability": "view"
                    }
                ]
                """,
                "EmailDataWallet" => """
                [
                    {
                        "type": "function",
                        "name": "totalEmailWallets",
                        "inputs": [],
                        "outputs": [{"name": "", "type": "uint256", "internalType": "uint256"}],
                        "stateMutability": "view"
                    }
                ]
                """,
                "AttachmentWallet" => "[]",
                "AuthorizationManager" => "[]",
                _ => "[]"
            };
        }

        private string ExtractRegistrationIdFromReceipt(Nethereum.RPC.Eth.DTOs.TransactionReceipt receipt)
        {
            // Extract registration ID from event logs
            // Implementation depends on your contract event structure
            return $"reg_{receipt.TransactionHash}_{DateTime.UtcNow.Ticks}";
        }

        private string ExtractAuthorizationIdFromReceipt(Nethereum.RPC.Eth.DTOs.TransactionReceipt receipt)
        {
            // Extract authorization ID from event logs
            return $"auth_{receipt.TransactionHash}_{DateTime.UtcNow.Ticks}";
        }

        private async Task<long> GetBlockNumberFromDate(DateTime date)
        {
            // Approximate block number from date
            // This is a simplified implementation
            var latestBlock = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            var blockTime = 2; // Average block time in seconds (Polygon)
            var secondsAgo = (DateTime.UtcNow - date).TotalSeconds;
            var blocksAgo = (long)(secondsAgo / blockTime);
            
            return Math.Max(0, (long)(latestBlock.Value - blocksAgo));
        }

        private Task<List<BlockchainEvent>> GetEventLogs(Contract contract, string eventName, 
            long fromBlock, string walletAddress)
        {
            try
            {
                // This is a simplified implementation
                // In production, you would use the actual event filters
                return Task.FromResult(new List<BlockchainEvent>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event logs for {EventName}", eventName);
                return Task.FromResult(new List<BlockchainEvent>());
            }
        }
    }
}