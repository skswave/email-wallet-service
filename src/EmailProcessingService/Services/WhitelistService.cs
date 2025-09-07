using EmailProcessingService.Models;
using EmailProcessingService.Data;

namespace EmailProcessingService.Services
{
    public class WhitelistService : IWhitelistService
    {
        private readonly EmailProcessingDbContext _context;
        private readonly ILogger<WhitelistService> _logger;
        
        public WhitelistService(EmailProcessingDbContext context, ILogger<WhitelistService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> IsEmailWhitelistedForUser(string userWallet, string email)
        {
            try
            {
                // MVP: Always return true for demo purposes
                _logger.LogInformation("MVP: Whitelist check for {Email} from wallet {UserWallet} - always returning true", email, userWallet);
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IsEmailWhitelistedForUser for email: {Email}", email);
                return false;
            }
        }
    }
}
