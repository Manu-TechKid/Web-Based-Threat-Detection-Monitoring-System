using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RouterDetector.Models;

namespace RouterDetector.Data
{
    public class RouterDetectorContext : DbContext
    {
        public RouterDetectorContext (DbContextOptions<RouterDetectorContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<SystemConfiguration>().HasData(
                new SystemConfiguration { Id = 1, InstitutionName = "Default Sacco", StaffPosition = "Default", County = "Default County" }
            );
            modelBuilder.Entity<User>().HasData(
                new User {
                    Id = 1,
                    Username = "Admin",
                    PasswordHash = "O2Esdae1BIpDX7bsgeUv+S1teVqLWpwXBw9qY8l6U7I=", // hash for Admin123
                    Email = "admin@example.com",
                    StaffPosition = "Admin",
                    SaccoId = 1
                }
            );
        }

        public DbSet<RouterDetector.Models.User> Users { get; set; } = default!;
        public DbSet<RouterDetector.Models.EventLog> EventLogs { get; set; } = default!;
        public DbSet<RouterDetector.Models.SystemConfiguration> SystemConfiguration { get; set; } = default!;
        public DbSet<RouterDetector.Models.EventType> EventTypes { get; set; } = default!;
        public DbSet<RouterDetector.Models.RuleType> RuleTypes { get; set; } = default!;
        public DbSet<RouterDetector.Models.Severity> Severities { get; set; } = default!;
        public DbSet<RouterDetector.Models.ActionTaken> ActionTakens { get; set; } = default!;
        public DbSet<RouterDetector.Models.MessageType> MessageTypes { get; set; } = default!;
        public DbSet<RouterDetector.Models.AuditTrail> AuditTrails { get; set; } = default!;
    }
}
