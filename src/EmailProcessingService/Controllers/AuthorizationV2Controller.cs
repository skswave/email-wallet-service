using EmailProcessingService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Nethereum.Web3;
using Nethereum.Contracts;
using System.Numerics;
using Newtonsoft.Json;

namespace EmailProcessingService.Controllers
{
    [ApiController]
    [Route("api/authorization")]
    [EnableCors]
    public class AuthorizationV2Controller : ControllerBase
    {
        private readonly ILogger<AuthorizationV2Controller> _logger;
        private readonly Web3 _web3;
        private readonly string _enhancedContractAddress;
        private readonly string _enhancedContractAbi;

        public AuthorizationV2Controller(ILogger<AuthorizationV2Controller> logger)
        {
            _logger = logger;
            
            // Enhanced Contract Configuration
            _enhancedContractAddress = "0xec695CcD7BC3f084356DD7Cf62ddE56b484A8A1a";
            _web3 = new Web3("https://rpc-amoy.polygon.technology");
            
            // Enhanced Contract ABI for Version 2.0
            _enhancedContractAbi = @"[
                {
                    ""type"": ""function"",
                    ""name"": ""getUserRequests"",
                    ""inputs"": [{""name"": ""userWallet"", ""type"": ""address"", ""internalType"": ""address""}],
                    ""outputs"": [{""name"": """", ""type"": ""bytes32[]"", ""internalType"": ""bytes32[]""}],
                    ""stateMutability"": ""view""
                },
                {
                    ""type"": ""function"",
                    ""name"": ""authorizationRequests"",
                    ""inputs"": [{""name"": ""requestId"", ""type"": ""bytes32"", ""internalType"": ""bytes32""}],
                    ""outputs"": [
                        {""name"": ""requestId"", ""type"": ""bytes32"", ""internalType"": ""bytes32""},
                        {""name"": ""userWallet"", ""type"": ""address"", ""internalType"": ""address""},
                        {""name"": ""authToken"", ""type"": ""string"", ""internalType"": ""string""},
                        {""name"": ""emailHash"", ""type"": ""bytes32"", ""internalType"": ""bytes32""},
                        {""name"": ""totalCost"", ""type"": ""uint256"", ""internalType"": ""uint256""},
                        {""name"": ""createdAt"", ""type"": ""uint256"", ""internalType"": ""uint256""},
                        {""name"": ""expiresAt"", ""type"": ""uint256"", ""internalType"": ""uint256""},
                        {""name"": ""status"", ""type"": ""uint8"", ""internalType"": ""uint8""},
                        {""name"": ""emailSubject"", ""type"": ""string"", ""internalType"": ""string""},
                        {""name"": ""emailSender"", ""type"": ""string"", ""internalType"": ""string""},
                        {""name"": ""attachmentCount"", ""type"": ""uint256"", ""internalType"": ""uint256""}
                    ],
                    ""stateMutability"": ""view""
                },
                {
                    ""type"": ""function"",
                    ""name"": ""getUserCredits"",
                    ""inputs"": [{""name"": ""userWallet"", ""type"": ""address"", ""internalType"": ""address""}],
                    ""outputs"": [{""name"": """", ""type"": ""uint256"", ""internalType"": ""uint256""}],
                    ""stateMutability"": ""view""
                },
                {
                    ""type"": ""function"",
                    ""name"": ""isRequestExpired"",
                    ""inputs"": [{""name"": ""requestId"", ""type"": ""bytes32"", ""internalType"": ""bytes32""}],
                    ""outputs"": [{""name"": """", ""type"": ""bool"", ""internalType"": ""bool""}],
                    ""stateMutability"": ""view""
                },
                {
                    ""type"": ""function"",
                    ""name"": ""createAuthorizationRequest"",
                    ""inputs"": [
                        {""name"": ""userWallet"", ""type"": ""address"", ""internalType"": ""address""},
                        {""name"": ""authToken"", ""type"": ""string"", ""internalType"": ""string""},
                        {""name"": ""emailSender"", ""type"": ""string"", ""internalType"": ""string""},
                        {""name"": ""emailSubject"", ""type"": ""string"", ""internalType"": ""string""},
                        {""name"": ""attachmentCount"", ""type"": ""uint256"", ""internalType"": ""uint256""},
                        {""name"": ""expiresAt"", ""type"": ""uint256"", ""internalType"": ""uint256""}
                    ],
                    ""outputs"": [{""name"": ""requestId"", ""type"": ""bytes32"", ""internalType"": ""bytes32""}],
                    ""stateMutability"": ""nonpayable""
                }
            ]";
        }

        /// <summary>
        /// Get authorization request details by request ID
        /// </summary>
        [HttpGet("requests/{requestId}")]
        public async Task<IActionResult> GetAuthorizationRequest(string requestId)
        {
            try
            {
                _logger.LogInformation("Getting authorization request details for {RequestId}", requestId);

                var contract = _web3.Eth.GetContract(_enhancedContractAbi, _enhancedContractAddress);
                var authRequestsFunction = contract.GetFunction("authorizationRequests");

                var result = await authRequestsFunction.CallDeserializingToObjectAsync<AuthorizationRequestData>(requestId);

                if (result.RequestId == "0x0000000000000000000000000000000000000000000000000000000000000000")
                {
                    return NotFound(new { message = "Authorization request not found" });
                }

                var response = new
                {
                    requestId = result.RequestId,
                    userWallet = result.UserWallet,
                    authToken = result.AuthToken,
                    emailHash = result.EmailHash,
                    totalCost = result.TotalCost.ToString(),
                    createdAt = DateTimeOffset.FromUnixTimeSeconds((long)result.CreatedAt).DateTime,
                    expiresAt = DateTimeOffset.FromUnixTimeSeconds((long)result.ExpiresAt).DateTime,
                    status = (int)result.Status,
                    statusName = GetStatusName((int)result.Status),
                    emailSubject = result.EmailSubject,
                    emailSender = result.EmailSender,
                    attachmentCount = result.AttachmentCount.ToString(),
                    isExpired = DateTimeOffset.UtcNow.ToUnixTimeSeconds() > (long)result.ExpiresAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting authorization request {RequestId}", requestId);
                return StatusCode(500, new { message = "Failed to get authorization request", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all authorization requests for a specific user
        /// </summary>
        [HttpGet("user/{userAddress}/requests")]
        public async Task<IActionResult> GetUserRequests(string userAddress)
        {
            try
            {
                _logger.LogInformation("Getting authorization requests for user {UserAddress}", userAddress);

                var contract = _web3.Eth.GetContract(_enhancedContractAbi, _enhancedContractAddress);
                var getUserRequestsFunction = contract.GetFunction("getUserRequests");

                var requestIds = await getUserRequestsFunction.CallAsync<List<string>>(userAddress);
                var requests = new List<object>();

                foreach (var requestId in requestIds)
                {
                    try
                    {
                        var authRequestsFunction = contract.GetFunction("authorizationRequests");
                        var result = await authRequestsFunction.CallDeserializingToObjectAsync<AuthorizationRequestData>(requestId);

                        if (result.RequestId != "0x0000000000000000000000000000000000000000000000000000000000000000")
                        {
                            requests.Add(new
                            {
                                requestId = result.RequestId,
                                userWallet = result.UserWallet,
                                authToken = result.AuthToken,
                                totalCost = result.TotalCost.ToString(),
                                createdAt = DateTimeOffset.FromUnixTimeSeconds((long)result.CreatedAt).DateTime,
                                expiresAt = DateTimeOffset.FromUnixTimeSeconds((long)result.ExpiresAt).DateTime,
                                status = (int)result.Status,
                                statusName = GetStatusName((int)result.Status),
                                emailSubject = result.EmailSubject,
                                emailSender = result.EmailSender,
                                attachmentCount = result.AttachmentCount.ToString(),
                                isExpired = DateTimeOffset.UtcNow.ToUnixTimeSeconds() > (long)result.ExpiresAt
                            });
                        }
                    }
                    catch (Exception reqEx)
                    {
                        _logger.LogWarning(reqEx, "Failed to load request details for {RequestId}", requestId);
                    }
                }

                return Ok(new { requests = requests.OrderByDescending(r => ((DateTime)((dynamic)r).createdAt)) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user requests for {UserAddress}", userAddress);
                return StatusCode(500, new { message = "Failed to get user requests", error = ex.Message });
            }
        }

        /// <summary>
        /// Get user credit balance
        /// </summary>
        [HttpGet("user/{userAddress}/credits")]
        public async Task<IActionResult> GetUserCredits(string userAddress)
        {
            try
            {
                _logger.LogInformation("Getting credit balance for user {UserAddress}", userAddress);

                var contract = _web3.Eth.GetContract(_enhancedContractAbi, _enhancedContractAddress);
                var getUserCreditsFunction = contract.GetFunction("getUserCredits");

                var credits = await getUserCreditsFunction.CallAsync<BigInteger>(userAddress);

                // Also get native POL balance
                var polBalance = await _web3.Eth.GetBalance.SendRequestAsync(userAddress);
                var polBalanceEther = Web3.Convert.FromWei(polBalance.Value);

                return Ok(new
                {
                    userAddress = userAddress,
                    credits = credits.ToString(),
                    polBalance = polBalanceEther.ToString("F6"),
                    lastUpdated = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user credits for {UserAddress}", userAddress);
                return StatusCode(500, new { message = "Failed to get user credits", error = ex.Message });
            }
        }

        /// <summary>
        /// Process user authorization (frontend calls this after MetaMask signature)
        /// </summary>
        [HttpPost("authorize")]
        public async Task<IActionResult> AuthorizeRequest([FromBody] AuthorizeRequestModel model)
        {
            try
            {
                _logger.LogInformation("Processing authorization for request {RequestId} from user {UserAddress}", 
                    model.RequestId, model.UserAddress);

                // Validate the signature
                if (string.IsNullOrEmpty(model.Signature))
                {
                    return BadRequest(new { message = "Signature is required" });
                }

                // In a real implementation, you would:
                // 1. Verify the signature is valid for the request ID
                // 2. Submit the authorization transaction to the blockchain
                // 3. Wait for confirmation
                
                // For now, return success - the actual transaction happens in the frontend
                return Ok(new
                {
                    success = true,
                    message = "Authorization recorded successfully",
                    requestId = model.RequestId,
                    userAddress = model.UserAddress,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing authorization for request {RequestId}", model.RequestId);
                return StatusCode(500, new { message = "Failed to process authorization", error = ex.Message });
            }
        }

        /// <summary>
        /// Process user rejection (frontend calls this after rejection modal)
        /// </summary>
        [HttpPost("reject")]
        public async Task<IActionResult> RejectRequest([FromBody] RejectRequestModel model)
        {
            try
            {
                _logger.LogInformation("Processing rejection for request {RequestId} from user {UserAddress}", 
                    model.RequestId, model.UserAddress);

                // In a real implementation, you would:
                // 1. Submit the rejection transaction to the blockchain
                // 2. Record the rejection reason and opt-out preferences
                // 3. Wait for confirmation

                return Ok(new
                {
                    success = true,
                    message = "Rejection recorded successfully",
                    requestId = model.RequestId,
                    userAddress = model.UserAddress,
                    reason = model.Reason,
                    customReason = model.CustomReason,
                    optOut = model.OptOut,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing rejection for request {RequestId}", model.RequestId);
                return StatusCode(500, new { message = "Failed to process rejection", error = ex.Message });
            }
        }

        /// <summary>
        /// Admin endpoint: Create new authorization request
        /// </summary>
        [HttpPost("admin/create")]
        public async Task<IActionResult> CreateAuthorizationRequest([FromBody] CreateAuthorizationRequestModel model)
        {
            try
            {
                _logger.LogInformation("Creating authorization request for user {UserAddress}", model.UserAddress);

                // In a real implementation, you would:
                // 1. Validate the admin has proper permissions
                // 2. Call the contract's createAuthorizationRequest function
                // 3. Return the new request ID

                // Generate a demo request ID for now
                var requestId = $"0x{Guid.NewGuid().ToString("N")[..64]}";

                return Ok(new
                {
                    success = true,
                    message = "Authorization request created successfully",
                    requestId = requestId,
                    userAddress = model.UserAddress,
                    emailSender = model.EmailSender,
                    emailSubject = model.EmailSubject,
                    attachmentCount = model.AttachmentCount,
                    expiresAt = model.ExpiresAt,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating authorization request for user {UserAddress}", model.UserAddress);
                return StatusCode(500, new { message = "Failed to create authorization request", error = ex.Message });
            }
        }

        private string GetStatusName(int status)
        {
            return status switch
            {
                0 => "Pending",
                1 => "Authorized",
                2 => "Rejected",
                3 => "Processed",
                4 => "Expired",
                _ => "Unknown"
            };
        }
    }

    // Data models for the enhanced contract
    public class AuthorizationRequestData
    {
        public string RequestId { get; set; }
        public string UserWallet { get; set; }
        public string AuthToken { get; set; }
        public string EmailHash { get; set; }
        public BigInteger TotalCost { get; set; }
        public BigInteger CreatedAt { get; set; }
        public BigInteger ExpiresAt { get; set; }
        public int Status { get; set; }
        public string EmailSubject { get; set; }
        public string EmailSender { get; set; }
        public BigInteger AttachmentCount { get; set; }
    }

    public class AuthorizeRequestModel
    {
        public string RequestId { get; set; }
        public string UserAddress { get; set; }
        public string Signature { get; set; }
    }

    public class RejectRequestModel
    {
        public string RequestId { get; set; }
        public string UserAddress { get; set; }
        public int Reason { get; set; }
        public string CustomReason { get; set; }
        public bool OptOut { get; set; }
    }

    public class CreateAuthorizationRequestModel
    {
        public string UserAddress { get; set; }
        public string AuthToken { get; set; }
        public string EmailSender { get; set; }
        public string EmailSubject { get; set; }
        public int AttachmentCount { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}