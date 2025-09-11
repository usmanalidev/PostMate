namespace PostmateAPI.DTOs
{
    public class WhatsAppWebhookRequest
    {
        public string? Message { get; set; }
        public string? From { get; set; }
        public string? To { get; set; }
    }
}
