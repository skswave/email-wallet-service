using Microsoft.AspNetCore.Mvc;
using EmailProcessingService.Services;

namespace EmailProcessingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductionBlockchainController : ControllerBase
    {
        private readonly IProductionBlockchainService _blockchainService;
        private readonly ILogger<ProductionBlockchainController> _logger;

        public ProductionBlockchainController(
            IProductionBlockchainService blockchainService,
            ILogger<ProductionBlockchainController> logger)
        {
            _blockchainService = blockchainService;
            _logger = logger;
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                var stats = await _blockchainService.GetServiceStatsAsync();
                return Ok(new
                {
                    Status = "Production blockchain service operational",
                    Timestamp = DateTime.UtcNow,
                    Stats = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blockchain service status");
                return StatusCode(500, new { Error = "Failed to get service status", Details = ex.Message });
            }
        }

        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var isConnected = await _blockchainService.TestConnectionAsync();
                return Ok(new
                {
                    Connected = isConnected,
                    Timestamp = DateTime.UtcNow,
                    Message = isConnected ? "Blockchain connection successful" : "Blockchain connection failed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing blockchain connection");
                return StatusCode(500, new { Error = "Connection test failed", Details = ex.Message });
            }
        }

        [HttpGet("wallet/{address}/registered")]
        public async Task<IActionResult> CheckWalletRegistration(string address)
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
                return StatusCode(500, new { Error = "Failed to check wallet registration", Details = ex.Message });
            }
        }

        [HttpGet("wallet/{address}/credits")]
        public async Task<IActionResult> GetCreditBalance(string address)
        {
            try
            {
                var balance = await _blockchainService.GetCreditBalanceAsync(address);
                return Ok(new
                {
                    WalletAddress = address,
                    CreditBalance = balance,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credit balance for {Address}", address);
                return StatusCode(500, new { Error = "Failed to get credit balance", Details = ex.Message });
            }
        }

        [HttpPost("test-transaction")]
        public async Task<IActionResult> TestTransaction([FromBody] TestTransactionRequest request)
        {
            try
            {
                var transactionHash = await _blockchainService.RecordEmailWalletAsync(
                    request.TaskId ?? $"test_{Guid.NewGuid():N}",
                    request.IpfsHash ?? $"QmTest{Guid.NewGuid():N}"
                );

                return Ok(new
                {
                    Success = true,
                    TransactionHash = transactionHash,
                    TaskId = request.TaskId,
                    IpfsHash = request.IpfsHash,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing test transaction");
                return StatusCode(500, new { Error = "Test transaction failed", Details = ex.Message });
            }
        }
    }

    public class TestTransactionRequest
    {
        public string? TaskId { get; set; }
        public string? IpfsHash { get; set; }
    }
}
