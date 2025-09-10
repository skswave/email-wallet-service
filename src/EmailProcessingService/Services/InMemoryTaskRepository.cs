using EmailProcessingService.Models;
using System.Collections.Concurrent;
using System.Text.Json;

namespace EmailProcessingService.Services
{
    public class InMemoryTaskRepository : ITaskRepository
    {
        private readonly ConcurrentDictionary<string, EmailProcessingTask> _tasks = new();
        private readonly ILogger<InMemoryTaskRepository> _logger;

        public InMemoryTaskRepository(ILogger<InMemoryTaskRepository> logger)
        {
            _logger = logger;
        }

        public Task<EmailProcessingTask?> GetTaskAsync(string taskId)
        {
            try
            {
                _tasks.TryGetValue(taskId, out var task);
                _logger.LogDebug("Retrieved task {TaskId}: {Found}", taskId, task != null);
                return Task.FromResult(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving task {TaskId}", taskId);
                return Task.FromResult<EmailProcessingTask?>(null);
            }
        }

        public Task UpdateTaskAsync(EmailProcessingTask task)
        {
            try
            {
                _tasks.AddOrUpdate(task.TaskId, task, (key, existingTask) => task);
                _logger.LogDebug("Updated task {TaskId} with status {Status}", task.TaskId, task.Status);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task {TaskId}", task.TaskId);
                throw;
            }
        }

        public Task CreateTaskAsync(EmailProcessingTask task)
        {
            try
            {
                if (!_tasks.TryAdd(task.TaskId, task))
                {
                    throw new InvalidOperationException($"Task with ID {task.TaskId} already exists");
                }
                
                _logger.LogInformation("Created new task {TaskId}", task.TaskId);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task {TaskId}", task.TaskId);
                throw;
            }
        }

        public Task<List<EmailProcessingTask>> GetTasksForUserAsync(string walletAddress)
        {
            try
            {
                var userTasks = _tasks.Values
                    .Where(t => t.OwnerWalletAddress?.Equals(walletAddress, StringComparison.OrdinalIgnoreCase) == true)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToList();

                _logger.LogDebug("Retrieved {Count} tasks for wallet {WalletAddress}", userTasks.Count, walletAddress);
                return Task.FromResult(userTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tasks for wallet {WalletAddress}", walletAddress);
                return Task.FromResult(new List<EmailProcessingTask>());
            }
        }

        public Task DeleteTaskAsync(string taskId)
        {
            try
            {
                var removed = _tasks.TryRemove(taskId, out _);
                _logger.LogDebug("Deleted task {TaskId}: {Success}", taskId, removed);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting task {TaskId}", taskId);
                throw;
            }
        }

        // Additional utility methods for MVP
        public Task<List<EmailProcessingTask>> GetTasksByStatusAsync(ProcessingStatus status)
        {
            try
            {
                var statusTasks = _tasks.Values
                    .Where(t => t.Status == status)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToList();

                return Task.FromResult(statusTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tasks by status {Status}", status);
                return Task.FromResult(new List<EmailProcessingTask>());
            }
        }

        public Task<int> GetTaskCountAsync()
        {
            return Task.FromResult(_tasks.Count);
        }

        public Task<Dictionary<ProcessingStatus, int>> GetTaskStatisticsAsync()
        {
            try
            {
                var stats = _tasks.Values
                    .GroupBy(t => t.Status)
                    .ToDictionary(g => g.Key, g => g.Count());

                return Task.FromResult(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task statistics");
                return Task.FromResult(new Dictionary<ProcessingStatus, int>());
            }
        }
    }
}
