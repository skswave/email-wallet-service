using EmailProcessingService.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EmailProcessingService.HealthChecks
{
    public class IpfsHealthCheck : IHealthCheck
    {
        private readonly IIpfsService _ipfsService;
        private readonly ILogger<IpfsHealthCheck> _logger;

        public IpfsHealthCheck(IIpfsService ipfsService, ILogger<IpfsHealthCheck> logger)
        {
            _ipfsService = ipfsService;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var testData = $"health-check-{DateTime.UtcNow:yyyyMMddHHmmss}";
                var result = await _ipfsService.UploadFileAsync(
                    System.Text.Encoding.UTF8.GetBytes(testData),
                    "health-check.txt");

                if (result.Success)
                {
                    return HealthCheckResult.Healthy(
                        $"IPFS is healthy. Test upload successful: {result.IpfsHash}");
                }
                else
                {
                    return HealthCheckResult.Degraded(
                        $"IPFS upload failed: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IPFS health check failed");
                return HealthCheckResult.Unhealthy(
                    $"IPFS health check failed: {ex.Message}");
            }
        }
    }
}
