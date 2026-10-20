using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RouterDetector.Models
{
    [Table("CyberAgents")]
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string StaffPosition { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int SaccoId { get; set; }
        [ForeignKey("SaccoId")]
        public SystemConfiguration? Sacco { get; set; }
    }
} 