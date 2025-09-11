namespace PostmateAPI.Services
{
    public interface IOpenAIService
    {
        Task<string> GenerateLinkedInPostAsync(string topic);
    }
}
