using EmailProcessingService.Models;
using MimeKit;
using System.Security.Cryptography;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexTypes;
using System.Text;

namespace EmailProcessingService.Services
{
    public interface IEmailProcessingService
    {
        Task<EmailProcessingTask> ProcessIncomingEmailAsync(MimeMessage mimeMessage);
        Task<EmailProcessingTask> ProcessIncomingEmailAsync(string rawMessage);
        Task<AuthorizationRequest> GenerateAuthorizationRequestAsync(EmailProcessingTask task);
        Task<bool> ProcessAuthorizationResponseAsync(string taskId, string authorizationSignature);
        Task<WalletCreationResult> FinalizeWalletCreationAsync(EmailProcessingTask task);
        Task<EmailProcessingTask?> GetProcessingTaskAsync(string taskId);
        Task<List<EmailProcessingTask>> GetTasksForUserAsync(string walletAddress);
    }

    public class EmailProcessingService : IEmailProcessingService
    {
        private readonly ILogger<EmailProcessingService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEmailParserService _emailParserService;
        private readonly IEmailValidationService _emailValidationService;
        private readonly IWalletCreatorService _walletCreatorService;
        private readonly IAuthorizationService _authorizationService;
        private readonly INotificationService _notificationService;
        private readonly ITaskRepository _taskRepository;
        private readonly IProductionBlockchainService _blockchainService;

        public EmailProcessingService(
            ILogger<EmailProcessingService> logger,
            IConfiguration configuration,
            IEmailParserService emailParserService,
            IEmailValidationService emailValidationService,
            IWalletCreatorService walletCreatorService,
            IAuthorizationService authorizationService,
            INotificationService notificationService,
            ITaskRepository taskRepository,
            IProductionBlockchainService blockchainService)
        {
            _logger = logger;
            _configuration = configuration;
            _emailParserService = emailParserService;
            _emailValidationService = emailValidationService;
            _walletCreatorService = walletCreatorService;
            _authorizationService = authorizationService;
            _notificationService = notificationService;
            _taskRepository = taskRepository;
            _blockchainService = blockchainService;
        }

        public async Task<EmailProcessingTask> ProcessIncomingEmailAsync(MimeMessage mimeMessage)
        {
            var taskId = GenerateTaskId();
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInformation("Starting email processing for message {MessageId}, Task {TaskId}", 
                    mimeMessage.MessageId, taskId);

                // Step 1: Parse the email
                var email = await _emailParserService.ParseEmailFromMimeMessage(mimeMessage);
                
                var task = new EmailProcessingTask
                {
                    TaskId = taskId,
                    MessageId = email.MessageId,
                    Status = ProcessingStatus.Received,
                    CreatedAt = startTime
                };

                await LogProcessingStep(task, "email_parsing", "completed", "Email successfully parsed");

                // Step 2: Validate the email
                task.Status = ProcessingStatus.Validating;
                await _taskRepository.UpdateTaskAsync(task);

                var validationResult = await _emailValidationService.ValidateIncomingEmail(email);
                
                if (!validationResult.IsValid)
                {
                    task.Status = ProcessingStatus.Failed;
                    task.ErrorMessage = $"Validation failed: {string.Join(", ", validationResult.ValidationErrors)}";
                    await LogProcessingStep(task, "email_validation", "failed", task.ErrorMessage);
                    await _taskRepository.UpdateTaskAsync(task);
                    return task;
                }

                // Set the owner wallet address
                task.OwnerWalletAddress = validationResult.RegisteredWalletAddress ?? 
                    throw new InvalidOperationException("No valid wallet address found for user");

                await LogProcessingStep(task, "email_validation", "passed", 
                    $"Email validated for wallet {task.OwnerWalletAddress}");

                // Step 3: Create temporary wallets
                task.Status = ProcessingStatus.Creating;
                await _taskRepository.UpdateTaskAsync(task);

                var walletCreationResult = await _walletCreatorService.CreateEmailDataWalletAsync(
                    email, validationResult.RegisteredWallet!);

                if (!walletCreationResult.Success)
                {
                    task.Status = ProcessingStatus.Failed;
                    task.ErrorMessage = $"Wallet creation failed: {walletCreationResult.ErrorMessage}";
                    await LogProcessingStep(task, "wallet_creation", "failed", task.ErrorMessage);
                    await _taskRepository.UpdateTaskAsync(task);
                    return task;
                }

                // Store temporary wallet IDs
                task.TemporaryEmailWalletId = walletCreationResult.EmailWalletId!;
                task.TemporaryAttachmentWalletIds = walletCreationResult.AttachmentWalletIds;
                task.EstimatedCredits = walletCreationResult.CreditsUsed;

                await LogProcessingStep(task, "wallet_creation", "completed", 
                    $"Created email wallet {task.TemporaryEmailWalletId} with {task.TemporaryAttachmentWalletIds.Count} attachments");

                // Step 4: Generate authorization request
                task.Status = ProcessingStatus.PendingAuthorization;
                
                var authRequest = await GenerateAuthorizationRequestAsync(task);
                task.AuthorizationToken = authRequest.Token;
                task.AuthorizationUrl = authRequest.AuthorizationUrl;

                await _taskRepository.UpdateTaskAsync(task);

                // Step 5: Send authorization notification
                await _notificationService.SendAuthorizationNotificationAsync(authRequest);

                await LogProcessingStep(task, "authorization_request", "sent", 
                    $"Authorization request sent to {task.OwnerWalletAddress}");

                _logger.LogInformation("Email processing task {TaskId} created and pending authorization", taskId);

                return task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing incoming email for task {TaskId}", taskId);
                
                var task = await _taskRepository.GetTaskAsync(taskId) ?? new EmailProcessingTask
                {
                    TaskId = taskId,
                    Status = ProcessingStatus.Failed,
                    ErrorMessage = ex.Message,
                    CreatedAt = startTime
                };

                task.Status = ProcessingStatus.Failed;
                task.ErrorMessage = ex.Message;
                await _taskRepository.UpdateTaskAsync(task);
                
                return task;
            }
        }

        public async Task<EmailProcessingTask> ProcessIncomingEmailAsync(string rawMessage)
        {
            try
            {
                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(rawMessage));
                var mimeMessage = await MimeMessage.LoadAsync(stream);
                return await ProcessIncomingEmailAsync(mimeMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing raw email message");
                throw;
            }
        }

        public async Task<AuthorizationRequest> GenerateAuthorizationRequestAsync(EmailProcessingTask task)
        {
            try
            {
                _logger.LogInformation("Generating authorization request for task {TaskId}", task.TaskId);

                var token = GenerateAuthorizationToken();
                var expiresAt = DateTime.UtcNow.AddHours(24); // 24-hour expiration

                // Get email details for the authorization request
                var emailDetails = await GetEmailDetailsFromTask(task);

                var authRequest = new AuthorizationRequest
                {
                    TaskId = task.TaskId,
                    WalletAddress = task.OwnerWalletAddress,
                    Token = token,
                    ExpiresAt = expiresAt,
                    EmailSubject = emailDetails.Subject,
                    EmailSender = emailDetails.Sender,
                    AttachmentCount = task.TemporaryAttachmentWalletIds.Count,
                    EstimatedCredits = task.EstimatedCredits,
                    AttachmentSummaries = emailDetails.AttachmentSummaries,
                    AuthorizationUrl = GenerateAuthorizationUrl(task.TaskId, token)
                };

                // Store authorization request
                await _authorizationService.StoreAuthorizationRequestAsync(authRequest);

                _logger.LogInformation("Authorization request generated for task {TaskId} with token {Token}", 
                    task.TaskId, token[..8] + "...");

                return authRequest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating authorization request for task {TaskId}", task.TaskId);
                throw;
            }
        }

        public async Task<bool> ProcessAuthorizationResponseAsync(string taskId, string authorizationSignature)
        {
            try
            {
                _logger.LogInformation("Processing authorization response for task {TaskId}", taskId);

                var task = await _taskRepository.GetTaskAsync(taskId);
                if (task == null)
                {
                    _logger.LogWarning("Task {TaskId} not found for authorization", taskId);
                    return false;
                }

                if (task.Status != ProcessingStatus.PendingAuthorization)
                {
                    _logger.LogWarning("Task {TaskId} is not in pending authorization status: {Status}", 
                        taskId, task.Status);
                    return false;
                }

                // Validate the authorization signature
                var isValidSignature = await _authorizationService.ValidateAuthorizationSignatureAsync(
                    taskId, authorizationSignature, task.OwnerWalletAddress);

                if (!isValidSignature)
                {
                    _logger.LogWarning("Invalid authorization signature for task {TaskId}", taskId);
                    await LogProcessingStep(task, "authorization", "failed", "Invalid signature");
                    return false;
                }

                // Update task status
                task.Status = ProcessingStatus.Authorized;
                task.AuthorizedAt = DateTime.UtcNow;
                await _taskRepository.UpdateTaskAsync(task);

                await LogProcessingStep(task, "authorization", "completed", 
                    "Authorization signature validated successfully");

                // Trigger wallet finalization
                _ = Task.Run(async () => await FinalizeWalletCreationAsync(task));

                _logger.LogInformation("Authorization processed successfully for task {TaskId}", taskId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing authorization for task {TaskId}", taskId);
                return false;
            }
        }

        public async Task<WalletCreationResult> FinalizeWalletCreationAsync(EmailProcessingTask task)
        {
            try
            {
                _logger.LogInformation("Finalizing wallet creation for task {TaskId}", task.TaskId);

                task.Status = ProcessingStatus.Processing;
                await _taskRepository.UpdateTaskAsync(task);

                await LogProcessingStep(task, "finalization", "started", "Starting wallet finalization process");

                // Step 1: Upload to IPFS
                task.Status = ProcessingStatus.StoringToIPFS;
                await _taskRepository.UpdateTaskAsync(task);

                var ipfsResult = await UploadWalletsToIPFS(task);
                if (!ipfsResult.Success)
                {
                    task.Status = ProcessingStatus.Failed;
                    task.ErrorMessage = $"IPFS upload failed: {ipfsResult.ErrorMessage}";
                    await _taskRepository.UpdateTaskAsync(task);
                    return new WalletCreationResult { Success = false, ErrorMessage = task.ErrorMessage };
                }

                await LogProcessingStep(task, "ipfs_upload", "completed", 
                    $"Wallets uploaded to IPFS: {ipfsResult.Hash}");

                // Step 2: Record on blockchain
                task.Status = ProcessingStatus.VerifyingOnBlockchain;
                await _taskRepository.UpdateTaskAsync(task);

                var blockchainResult = await RecordOnBlockchain(task, ipfsResult);
                if (!blockchainResult.Success)
                {
                    task.Status = ProcessingStatus.Failed;
                    task.ErrorMessage = $"Blockchain recording failed: {blockchainResult.ErrorMessage}";
                    await _taskRepository.UpdateTaskAsync(task);
                    return new WalletCreationResult { Success = false, ErrorMessage = task.ErrorMessage };
                }

                await LogProcessingStep(task, "blockchain_verification", "completed", 
                    $"Recorded on blockchain: {blockchainResult.TransactionHash}");

                // Step 3: Finalize task
                task.Status = ProcessingStatus.Completed;
                task.CompletedAt = DateTime.UtcNow;
                task.ActualCreditsUsed = task.EstimatedCredits;
                await _taskRepository.UpdateTaskAsync(task);

                await LogProcessingStep(task, "completion", "completed", 
                    "Email data wallet creation completed successfully");

                // Send completion notification
                await _notificationService.SendCompletionNotificationAsync(task);

                var result = new WalletCreationResult
                {
                    Success = true,
                    EmailWalletId = task.TemporaryEmailWalletId,
                    AttachmentWalletIds = task.TemporaryAttachmentWalletIds,
                    CreditsUsed = task.ActualCreditsUsed,
                    VerificationInfo = new VerificationInfo
                    {
                        ContentHash = ipfsResult.Hash,
                        BlockchainTx = blockchainResult.TransactionHash,
                        BlockNumber = blockchainResult.BlockNumber,
                        VerifiedAt = DateTime.UtcNow,
                        Network = blockchainResult.Network
                    }
                };

                _logger.LogInformation("Wallet creation finalized successfully for task {TaskId}", task.TaskId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finalizing wallet creation for task {TaskId}", task.TaskId);
                
                task.Status = ProcessingStatus.Failed;
                task.ErrorMessage = ex.Message;
                await _taskRepository.UpdateTaskAsync(task);

                return new WalletCreationResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<EmailProcessingTask?> GetProcessingTaskAsync(string taskId)
        {
            try
            {
                return await _taskRepository.GetTaskAsync(taskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving task {TaskId}", taskId);
                return null;
            }
        }

        public async Task<List<EmailProcessingTask>> GetTasksForUserAsync(string walletAddress)
        {
            try
            {
                return await _taskRepository.GetTasksForUserAsync(walletAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tasks for user {WalletAddress}", walletAddress);
                return new List<EmailProcessingTask>();
            }
        }

        private async Task LogProcessingStep(EmailProcessingTask task, string step, string status, string message)
        {
            var logEntry = new ProcessingLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Step = step,
                Status = status,
                Message = message
            };

            task.ProcessingLog.Add(logEntry);
            
            _logger.LogInformation("Task {TaskId} - {Step}: {Status} - {Message}", 
                task.TaskId, step, status, message);
        }

        private string GenerateTaskId()
        {
            return $"task_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{Guid.NewGuid().ToString("N")[..8]}";
        }

        private string GenerateAuthorizationToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("/", "_").Replace("+", "-").TrimEnd('=');
        }

        private string GenerateAuthorizationUrl(string taskId, string token)
        {
            var baseUrl = _configuration["EmailProcessing:AuthorizationBaseUrl"] ?? "https://auth.rootz.global";
            return $"{baseUrl}/authorize?task={taskId}&token={token}";
        }

        private async Task<EmailDetails> GetEmailDetailsFromTask(EmailProcessingTask task)
        {
            // This would typically retrieve the email details from storage
            // For now, return placeholder data
            return await Task.FromResult(new EmailDetails
            {
                Subject = "Email processing request",
                Sender = "sender@example.com",
                AttachmentSummaries = task.TemporaryAttachmentWalletIds.Select((id, index) => 
                    new AttachmentSummary
                    {
                        FileName = $"attachment_{index + 1}",
                        FileSize = 1024,
                        FileType = ".pdf",
                        ContentHash = "hash_placeholder",
                        VirusScanPassed = true
                    }).ToList()
            });
        }

        private async Task<IPFSUploadResult> UploadWalletsToIPFS(EmailProcessingTask task)
        {
            // Placeholder for IPFS upload implementation
            return await Task.FromResult(new IPFSUploadResult
            {
                Success = true,
                Hash = $"QmHash_{task.TaskId}",
                Size = 1024,
                Gateway = "https://ipfs.rootz.global/ipfs/",
                UploadTime = TimeSpan.FromSeconds(5)
            });
        }

        private async Task<BlockchainTransactionResult> RecordOnBlockchain(EmailProcessingTask task, IPFSUploadResult ipfsResult)
        {
            try
            {
                _logger.LogInformation("Recording blockchain transaction using production service for task {TaskId}", task.TaskId);

                var transactionHash = await _blockchainService.RecordEmailWalletAsync(task.TaskId, ipfsResult.Hash);

                _logger.LogInformation("Production blockchain transaction completed: Hash={TxHash}, TaskId={TaskId}", 
                    transactionHash, task.TaskId);

                return new BlockchainTransactionResult
                {
                    Success = true,
                    TransactionHash = transactionHash,
                    BlockNumber = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), // Will be updated with real block number in future versions
                    Network = "polygon-amoy",
                    GasUsed = 150000, // Estimated
                    GasCost = 0.001m,
                    ConfirmationTime = TimeSpan.FromSeconds(5)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Production blockchain transaction failed for task {TaskId}", task.TaskId);
                return new BlockchainTransactionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private class EmailDetails
        {
            public string Subject { get; set; } = string.Empty;
            public string Sender { get; set; } = string.Empty;
            public List<AttachmentSummary> AttachmentSummaries { get; set; } = new();
        }
    }

    // Extended interface for user registration
    public interface IUserRegistrationService
    {
        Task<UserRegistration?> GetRegistrationByEmailAsync(string email);
        Task<UserRegistration?> GetRegistrationByWalletAsync(string walletAddress);
        Task<bool> ValidateCorporateAuthorizationAsync(string corporateWallet, string userWallet);
        Task CreateRegistrationAsync(UserRegistration registration);
        Task UpdateRegistrationAsync(UserRegistration registration);
        Task<List<UserRegistration>> GetAllRegistrationsAsync();
    }

    // Placeholder interfaces for dependencies
    public interface IAuthorizationService
    {
        Task StoreAuthorizationRequestAsync(AuthorizationRequest request);
        Task<bool> ValidateAuthorizationSignatureAsync(string taskId, string signature, string walletAddress);
    }

    public interface INotificationService
    {
        Task SendAuthorizationNotificationAsync(AuthorizationRequest request);
        Task SendCompletionNotificationAsync(EmailProcessingTask task);
    }

    public interface ITaskRepository
    {
        Task<EmailProcessingTask?> GetTaskAsync(string taskId);
        Task UpdateTaskAsync(EmailProcessingTask task);
        Task<List<EmailProcessingTask>> GetTasksForUserAsync(string walletAddress);
        Task CreateTaskAsync(EmailProcessingTask task);
        Task DeleteTaskAsync(string taskId);
    }
}
