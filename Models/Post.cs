using System.ComponentModel.DataAnnotations;

namespace PostmateAPI.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string Topic { get; set; } = string.Empty;
        
        public string? Draft { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Posted
        
        public DateTime? ScheduledAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
