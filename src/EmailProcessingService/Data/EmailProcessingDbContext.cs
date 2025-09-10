using EmailProcessingService.Models;
using Microsoft.EntityFrameworkCore;

namespace EmailProcessingService.Data
{
    public class EmailProcessingDbContext : DbContext
    {
        public EmailProcessingDbContext(DbContextOptions<EmailProcessingDbContext> options) : base(options) { }

        public DbSet<UserRegistration> UserRegistrations { get; set; } = null!;
        public DbSet<EmailProcessingTask> ProcessingTasks { get; set; } = null!;
        public DbSet<WhitelistEntry> WhitelistEntries { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure UserRegistration
            modelBuilder.Entity<UserRegistration>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.WalletAddress).IsUnique();
                entity.HasIndex(e => e.EmailAddress).IsUnique();
                entity.Property(e => e.Settings).HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<UserRegistrationSettings>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new());
                entity.Property(e => e.WhitelistedDomains).HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new());
            });

            // Configure EmailProcessingTask - Enhanced for IPFS
            modelBuilder.Entity<EmailProcessingTask>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.TaskId).IsUnique();
                entity.HasIndex(e => e.OwnerWalletAddress);
                entity.Property(e => e.TemporaryAttachmentWalletIds).HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new());
                entity.Property(e => e.ProcessingLog).HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<ProcessingLogEntry>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new());
            });

            // Configure EnhancedEmailProcessingTask for IPFS support
            modelBuilder.Entity<EnhancedEmailProcessingTask>(entity =>
            {
                entity.HasBaseType<EmailProcessingTask>();
                entity.Property(e => e.AttachmentIpfsHashes).HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<AttachmentIpfsInfo>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new());
                entity.Property(e => e.IpfsStorage).HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<IpfsStorageInfo>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new());
            });

            // Configure WhitelistEntry
            modelBuilder.Entity<WhitelistEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.OwnerWalletAddress, e.Domain });
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
