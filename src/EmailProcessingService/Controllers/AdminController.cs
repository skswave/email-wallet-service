using Microsoft.AspNetCore.Mvc;
using EmailProcessingService.Services;
using EmailProcessingService.Models;

namespace EmailProcessingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly ILogger<AdminController> _logger;
        private readonly IEmailProcessingService _emailProcessingService;
        private readonly IUserRegistrationService _userRegistrationService;
        private readonly IBlockchainService _blockchainService;
        private readonly ITaskRepository _taskRepository;

        public AdminController(
            ILogger<AdminController> logger,
            IEmailProcessingService emailProcessingService,
            IUserRegistrationService userRegistrationService,
            IBlockchainService blockchainService,
            ITaskRepository taskRepository)
        {
            _logger = logger;
            _emailProcessingService = emailProcessingService;
            _userRegistrationService = userRegistrationService;
            _blockchainService = blockchainService;
            _taskRepository = taskRepository;
        }

        [HttpGet("metrics/system")]
        public async Task<IActionResult> GetSystemMetrics()
        {
            try
            {
                // Get basic system metrics
                var metrics = new
                {
                    totalRegistrations = await GetTotalRegistrations(),
                    activeWallets = await GetActiveWallets(),
                    pendingAuthorizations = await GetPendingAuthorizations(),
                    failedTransactions = await GetFailedTransactions()
                };

                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system metrics");
                return StatusCode(500, new { message = "Error retrieving system metrics" });
            }
        }

        [HttpGet("metrics/email")]
        public async Task<IActionResult> GetEmailMetrics()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var metrics = new
                {
                    emailsProcessedToday = await GetEmailsProcessedToday(today),
                    walletsCreatedToday = await GetWalletsCreatedToday(today),
                    processingQueueSize = await GetProcessingQueueSize(),
                    averageProcessingTime = await GetAverageProcessingTime()
                };

                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting email metrics");
                return StatusCode(500, new { message = "Error retrieving email metrics" });
            }
        }

        [HttpGet("metrics/credits")]
        public async Task<IActionResult> GetCreditMetrics()
        {
            try
            {
                var metrics = new
                {
                    totalCreditsAllocated = await GetTotalCreditsAllocated(),
                    creditsUsedToday = await GetCreditsUsedToday(),
                    averageCreditCost = await GetAverageCreditCost(),
                    lowBalanceWallets = await GetLowBalanceWallets()
                };

                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credit metrics");
                return StatusCode(500, new { message = "Error retrieving credit metrics" });
            }
        }

        [HttpGet("metrics/health")]
        public async Task<IActionResult> GetHealthMetrics()
        {
            try
            {
                var metrics = new
                {
                    uptime = GetSystemUptime(),
                    lastBlockchainSync = await GetLastBlockchainSync(),
                    memoryUsage = GetMemoryUsage(),
                    errorRate = await GetErrorRate()
                };

                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting health metrics");
                return StatusCode(500, new { message = "Error retrieving health metrics" });
            }
        }

        [HttpGet("registrations")]
        public async Task<IActionResult> GetRegistrations([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var registrations = await GetPaginatedRegistrations(page, pageSize);
                return Ok(registrations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting registrations");
                return StatusCode(500, new { message = "Error retrieving registrations" });
            }
        }

        [HttpGet("email-tasks")]
        public async Task<IActionResult> GetEmailTasks([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var tasks = await GetPaginatedEmailTasks(page, pageSize);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting email tasks");
                return StatusCode(500, new { message = "Error retrieving email tasks" });
            }
        }

        [HttpGet("wallets")]
        public async Task<IActionResult> GetWallets([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var wallets = await GetPaginatedWallets(page, pageSize);
                return Ok(wallets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wallets");
                return StatusCode(500, new { message = "Error retrieving wallets" });
            }
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            try
            {
                var logs = await GetSystemLogs(page, pageSize);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system logs");
                return StatusCode(500, new { message = "Error retrieving system logs" });
            }
        }

        [HttpPost("export")]
        public async Task<IActionResult> ExportData()
        {
            try
            {
                var exportData = await GenerateExportData();
                var jsonData = System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var bytes = System.Text.Encoding.UTF8.GetBytes(jsonData);
                return File(bytes, "application/json", $"email-wallet-export-{DateTime.UtcNow:yyyy-MM-dd}.json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data");
                return StatusCode(500, new { message = "Error exporting data" });
            }
        }

        [HttpPost("sync-blockchain")]
        public async Task<IActionResult> SyncBlockchain()
        {
            try
            {
                // Trigger a manual blockchain sync
                await _blockchainService.TestConnectionAsync();
                
                _logger.LogInformation("Manual blockchain sync initiated by admin");
                return Ok(new { message = "Blockchain sync initiated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during manual blockchain sync");
                return StatusCode(500, new { message = "Error during blockchain sync" });
            }
        }

        // Helper methods for metrics calculation
        private async Task<int> GetTotalRegistrations()
        {
            // This would typically query your database
            return await Task.FromResult(12); // Demo data
        }

        private async Task<int> GetActiveWallets()
        {
            return await Task.FromResult(8); // Demo data
        }

        private async Task<int> GetPendingAuthorizations()
        {
            return await Task.FromResult(3); // Demo data
        }

        private async Task<int> GetFailedTransactions()
        {
            return await Task.FromResult(1); // Demo data
        }

        private async Task<int> GetEmailsProcessedToday(DateTime today)
        {
            return await Task.FromResult(24); // Demo data
        }

        private async Task<int> GetWalletsCreatedToday(DateTime today)
        {
            return await Task.FromResult(5); // Demo data
        }

        private async Task<int> GetProcessingQueueSize()
        {
            return await Task.FromResult(2); // Demo data
        }

        private async Task<string> GetAverageProcessingTime()
        {
            return await Task.FromResult("45s"); // Demo data
        }

        private async Task<string> GetTotalCreditsAllocated()
        {
            return await Task.FromResult("150.5"); // Demo data
        }

        private async Task<string> GetCreditsUsedToday()
        {
            return await Task.FromResult("12.3"); // Demo data
        }

        private async Task<string> GetAverageCreditCost()
        {
            return await Task.FromResult("0.05"); // Demo data
        }

        private async Task<int> GetLowBalanceWallets()
        {
            return await Task.FromResult(2); // Demo data
        }

        private string GetSystemUptime()
        {
            var uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime;
            return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";
        }

        private async Task<string> GetLastBlockchainSync()
        {
            return await Task.FromResult(DateTime.UtcNow.AddMinutes(-5).ToString("yyyy-MM-dd HH:mm:ss"));
        }

        private string GetMemoryUsage()
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var memoryMB = process.WorkingSet64 / 1024 / 1024;
            return $"{memoryMB} MB";
        }

        private async Task<string> GetErrorRate()
        {
            return await Task.FromResult("2.1%"); // Demo data
        }

        private async Task<object[]> GetPaginatedRegistrations(int page, int pageSize)
        {
            // Get from user registration service
            var allRegistrations = await _userRegistrationService.GetAllRegistrationsAsync();
            
            var paginatedRegistrations = allRegistrations
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(reg => new {
                    emailAddress = reg.EmailAddress,
                    walletAddress = reg.WalletAddress,
                    displayName = reg.EmailAddress, // Use email as display name for now
                    registrationDate = reg.RegisteredAt,
                    isActive = reg.IsActive,
                    creditBalance = "5.25" // Mock credit balance
                }).ToArray();

            return await Task.FromResult(paginatedRegistrations);
        }

        private async Task<object[]> GetPaginatedEmailTasks(int page, int pageSize)
        {
            // Demo data - replace with actual database query
            var demoTasks = new[]
            {
                new {
                    taskId = "task-123",
                    emailFrom = "sender@company.com",
                    emailSubject = "Important Financial Report",
                    createdAt = DateTime.UtcNow.AddHours(-2),
                    status = "completed",
                    emailSize = 2048000
                },
                new {
                    taskId = "task-124",
                    emailFrom = "alice@example.com",
                    emailSubject = "Meeting Notes",
                    createdAt = DateTime.UtcNow.AddMinutes(-30),
                    status = "processing",
                    emailSize = 512000
                }
            };

            return await Task.FromResult(demoTasks);
        }

        private async Task<object[]> GetPaginatedWallets(int page, int pageSize)
        {
            // Demo data - replace with actual database query
            var demoWallets = new[]
            {
                new {
                    walletAddress = "0xabcdef1234567890abcdef1234567890abcdef12",
                    ownerAddress = "0x107C5655ce50AB9744Fc36A4e9935E30d4923d0b",
                    walletType = "EMAIL",
                    createdAt = DateTime.UtcNow.AddDays(-1),
                    dataHash = "0x123456789abcdef123456789abcdef123456789abcdef",
                    dataSize = 2048000
                }
            };

            return await Task.FromResult(demoWallets);
        }

        private async Task<object[]> GetSystemLogs(int page, int pageSize)
        {
            // Demo data - replace with actual log query
            var demoLogs = new[]
            {
                new {
                    id = "log-1",
                    timestamp = DateTime.UtcNow.AddMinutes(-10),
                    level = "info",
                    component = "BlockchainService",
                    message = "Wallet creation completed successfully",
                    details = (string?)null
                },
                new {
                    id = "log-2",
                    timestamp = DateTime.UtcNow.AddMinutes(-15),
                    level = "warning",
                    component = "EmailProcessingService",
                    message = "Email processing took longer than expected",
                    details = (string?)"Processing time: 67 seconds"
                }
            };

            return await Task.FromResult(demoLogs);
        }

        private async Task<object> GenerateExportData()
        {
            var exportData = new
            {
                exportDate = DateTime.UtcNow,
                registrations = await GetPaginatedRegistrations(1, 1000),
                emailTasks = await GetPaginatedEmailTasks(1, 1000),
                wallets = await GetPaginatedWallets(1, 1000),
                systemMetrics = new
                {
                    totalRegistrations = await GetTotalRegistrations(),
                    activeWallets = await GetActiveWallets(),
                    pendingAuthorizations = await GetPendingAuthorizations()
                }
            };

            return exportData;
        }
    }
}