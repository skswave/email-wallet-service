using EmailProcessingService.Models;
using EmailProcessingService.Services;
using Nethereum.Web3;
using Nethereum.Contracts;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace EmailProcessingService.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly ILogger<AuthorizationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ConcurrentDictionary<string, AuthorizationRequest> _authRequests = new();

        public AuthorizationService(
            ILogger<AuthorizationService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public Task StoreAuthorizationRequestAsync(AuthorizationRequest request)
        {
            try
            {
                _authRequests.AddOrUpdate(request.TaskId, request, (key, existing) => request);
                
                _logger.LogInformation("Stored authorization request for task {TaskId} and wallet {WalletAddress}", 
                    request.TaskId, request.WalletAddress);
                
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing authorization request for task {TaskId}", request.TaskId);
                throw;
            }
        }

        public Task<bool> ValidateAuthorizationSignatureAsync(string taskId, string signature, string walletAddress)
        {
            try
            {
                _logger.LogInformation("Validating authorization signature for task {TaskId}", taskId);

                // Get the stored authorization request
                if (!_authRequests.TryGetValue(taskId, out var authRequest))
                {
                    _logger.LogWarning("Authorization request not found for task {TaskId}", taskId);
                    return Task.FromResult(false);
                }

                // Check if expired
                if (authRequest.IsExpired)
                {
                    _logger.LogWarning("Authorization request expired for task {TaskId}", taskId);
                    return Task.FromResult(false);
                }

                // Check wallet address matches
                if (!authRequest.WalletAddress.Equals(walletAddress, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Wallet address mismatch for task {TaskId}. Expected: {Expected}, Got: {Actual}", 
                        taskId, authRequest.WalletAddress, walletAddress);
                    return Task.FromResult(false);
                }

                // For MVP - simplified signature validation
                var isValidSignature = !string.IsNullOrEmpty(signature) && signature.Length > 10;
                
                if (isValidSignature)
                {
                    _logger.LogInformation("Authorization signature validated successfully for task {TaskId}", taskId);
                }
                else
                {
                    _logger.LogWarning("Invalid authorization signature for task {TaskId}", taskId);
                }

                return Task.FromResult(isValidSignature);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating authorization signature for task {TaskId}", taskId);
                return Task.FromResult(false);
            }
        }

        public Task<AuthorizationRequest?> GetAuthorizationRequestAsync(string taskId)
        {
            try
            {
                _authRequests.TryGetValue(taskId, out var request);
                return Task.FromResult(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving authorization request for task {TaskId}", taskId);
                return Task.FromResult<AuthorizationRequest?>(null);
            }
        }

        public Task RevokeAuthorizationAsync(string taskId)
        {
            try
            {
                if (_authRequests.TryRemove(taskId, out var removed))
                {
                    _logger.LogInformation("Revoked authorization for task {TaskId}", taskId);
                }
                else
                {
                    _logger.LogWarning("No authorization found to revoke for task {TaskId}", taskId);
                }
                
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking authorization for task {TaskId}", taskId);
                throw;
            }
        }
    }

    // Simple notification service for MVP
    public class SimpleNotificationService : INotificationService
    {
        private readonly ILogger<SimpleNotificationService> _logger;
        private readonly IConfiguration _configuration;

        public SimpleNotificationService(ILogger<SimpleNotificationService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task SendAuthorizationNotificationAsync(AuthorizationRequest request)
        {
            try
            {
                _logger.LogInformation("📧 AUTHORIZATION REQUIRED for task {TaskId}", request.TaskId);
                _logger.LogInformation("   Wallet: {WalletAddress}", request.WalletAddress);
                _logger.LogInformation("   Email: {EmailSubject} from {EmailSender}", request.EmailSubject, request.EmailSender);
                _logger.LogInformation("   Attachments: {AttachmentCount}", request.AttachmentCount);
                _logger.LogInformation("   Credits: {EstimatedCredits}", request.EstimatedCredits);
                _logger.LogInformation("   Authorization URL: {AuthorizationUrl}", request.AuthorizationUrl);
                _logger.LogInformation("   Expires: {ExpiresAt}", request.ExpiresAt);

                await Task.Delay(100); // Simulate notification sending
                
                _logger.LogInformation("✅ Authorization notification sent for task {TaskId}", request.TaskId);
                
                // 🚀 AUTO-AUTHORIZATION FOR TESTING
                var autoAuthorize = _configuration.GetValue<bool>("EmailProcessing:AutoAuthorizeForTesting", false);
                if (autoAuthorize)
                {
                    _logger.LogInformation("🤖 AUTO-AUTHORIZATION enabled for testing - automatically approving task {TaskId}", request.TaskId);
                    
                    // Trigger auto-authorization after a short delay
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(2000); // 2 second delay to simulate user approval
                        
                        try
                        {
                            var httpClient = new HttpClient();
                            // Skip SSL certificate validation for localhost testing
                            httpClient.DefaultRequestHeaders.Add("User-Agent", "EmailProcessingService-AutoAuth");
                            
                            var authRequest = new
                            {
                                authorizationSignature = $"auto_approved_{DateTime.UtcNow:yyyyMMddHHmmss}_{request.TaskId}"
                            };
                            
                            var json = System.Text.Json.JsonSerializer.Serialize(authRequest);
                            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                            
                            // Use HTTP instead of HTTPS for auto-authorization to avoid SSL issues
                            var response = await httpClient.PostAsync(
                                $"http://localhost:5000/api/EmailProcessing/task/{request.TaskId}/authorize", 
                                content);
                            
                            if (response.IsSuccessStatusCode)
                            {
                                _logger.LogInformation("✅ AUTO-AUTHORIZATION successful for task {TaskId}", request.TaskId);
                            }
                            else
                            {
                                _logger.LogWarning("⚠️ AUTO-AUTHORIZATION failed for task {TaskId}: {StatusCode}", request.TaskId, response.StatusCode);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ AUTO-AUTHORIZATION error for task {TaskId}", request.TaskId);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending authorization notification for task {TaskId}", request.TaskId);
            }
        }

        public async Task SendCompletionNotificationAsync(EmailProcessingTask task)
        {
            try
            {
                _logger.LogInformation("🎉 EMAIL PROCESSING COMPLETED for task {TaskId}", task.TaskId);
                _logger.LogInformation("   Wallet: {WalletAddress}", task.OwnerWalletAddress);
                _logger.LogInformation("   Email Wallet ID: {EmailWalletId}", task.TemporaryEmailWalletId);
                _logger.LogInformation("   Attachment Wallets: {AttachmentCount}", task.TemporaryAttachmentWalletIds.Count);
                _logger.LogInformation("   Credits Used: {CreditsUsed}", task.ActualCreditsUsed);
                _logger.LogInformation("   Completed: {CompletedAt}", task.CompletedAt);

                await Task.Delay(100);
                
                _logger.LogInformation("✅ Completion notification sent for task {TaskId}", task.TaskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending completion notification for task {TaskId}", task.TaskId);
            }
        }

        public async Task SendErrorNotificationAsync(EmailProcessingTask task, string error)
        {
            try
            {
                _logger.LogError("❌ EMAIL PROCESSING FAILED for task {TaskId}", task.TaskId);
                _logger.LogError("   Wallet: {WalletAddress}", task.OwnerWalletAddress);
                _logger.LogError("   Error: {Error}", error);
                _logger.LogError("   Status: {Status}", task.Status);

                await Task.Delay(100);
                
                _logger.LogInformation("✅ Error notification sent for task {TaskId}", task.TaskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending error notification for task {TaskId}", task.TaskId);
            }
        }
    }
}
