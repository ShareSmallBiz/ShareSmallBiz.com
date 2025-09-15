using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareSmallBiz.Portal.Data.Entities
{
    public class LoginHistory : BaseEntity
    {
        // Id inherited from BaseEntity

        [Required]
    public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
    public virtual ShareSmallBizUser? User { get; set; }

        public DateTime LoginTime { get; set; }

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        public bool Success { get; set; } // Add this property

        public LoginHistory()
        {
            LoginTime = DateTime.UtcNow; // Default to current time on creation
            Success = false; // Default to false
        }
    }
}
