using System;
using System.ComponentModel.DataAnnotations;

namespace RouterDetector.Models
{
    public class AuditTrail
    {
        [Key]
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
} 