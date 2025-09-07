using Microsoft.AspNetCore.Mvc;
using EmailProcessingService.Services;

namespace EmailProcessingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlockchainTestController : ControllerBase
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ILogger<BlockchainTestController> _logger;

        public BlockchainTestController(
            IBlockchainService blockchainService,
            ILogger<BlockchainTestController> logger)
        {
            _blockchainService = blockchainService;
            _logger = logger;
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                _logger.LogInformation("Testing blockchain service status...");
                
                var isConnected = await _blockchainService.TestConnectionAsync();
                
                var status = new
                {
                    IsConnected = isConnected,
                    Timestamp = DateTime.UtcNow,
                    Message = isConnected ? "Blockchain service is operational" : "Blockchain service connection failed"
                };

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing blockchain status");
                return StatusCode(500, new
                {
                    IsConnected = false,
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpGet("wallet/{address}/registered")]
        public async Task<IActionResult> IsWalletRegistered(string address)
        {
            try
            {
                var isRegistered = await _blockchainService.IsWalletRegisteredAsync(address);
                
                return Ok(new
                {
                    WalletAddress = address,
                    IsRegistered = isRegistered,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking wallet registration for {Address}", address);
                return StatusCode(500, new
                {
                    Error = ex.Message,
                    WalletAddress = address,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpGet("wallet/{address}/credits")]
        public async Task<IActionResult> GetCreditBalance(string address)
        {
            try
            {
                var balance = await _blockchainService.GetCreditBalanceAsync(address);
                
                return Ok(balance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credit balance for {Address}", address);
                return StatusCode(500, new
                {
                    Error = ex.Message,
                    WalletAddress = address,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpGet("functions/{contractName}")]
        public IActionResult GetAvailableFunctions(string contractName)
        {
            try
            {
                var functions = new List<string>();
                
                // This is a simple way to check what functions are available
                // In a real implementation, you'd get this from the contract ABI
                functions.Add("registrationFee()");
                functions.Add("isRegistered(address)");
                functions.Add("totalRegistrations()");
                
                return Ok(new
                {
                    ContractName = contractName,
                    AvailableFunctions = functions,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available functions for {ContractName}", contractName);
                return StatusCode(500, new
                {
                    Error = ex.Message,
                    ContractName = contractName,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpGet("registrationfee")]
        public async Task<IActionResult> GetRegistrationFee()
        {
            try
            {
                // Test a simple function call that should work
                var testContract = _blockchainService;
                
                return Ok(new
                {
                    Message = "Registration fee endpoint - function calls would be implemented here",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting registration fee");
                return StatusCode(500, new
                {
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }
}