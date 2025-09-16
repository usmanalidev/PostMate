namespace PostmateAPI.Services
{
    public interface IWhatsAppService
    {
        Task<bool> SendMessageAsync(string to, string message);
        Task<bool> SendConfirmationMessageAsync(string to, string postTopic, string draft, string postType = "educational");
        Task<bool> SendSchedulingOptionsAsync(string to, string postTopic);
        Task<bool> SendStatusUpdateAsync(string to, string status, string postTopic);
        Task<bool> SendScheduledTimeAsync(string to, string postTopic, DateTime scheduledAt);
        Task<bool> SendPostTypeHelpMessageAsync(string to);
    }
}
