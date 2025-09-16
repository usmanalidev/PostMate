namespace PostmateAPI.DTOs
{
    public class PostResponse
    {
        public int Id { get; set; }
        public string Topic { get; set; } = string.Empty;
        public string PostType { get; set; } = string.Empty;
        public string? Draft { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? ScheduledAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
