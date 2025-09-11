using PostmateAPI.DTOs;
using System.Text;
using System.Text.Json;

namespace PostmateAPI.Services
{
    public class WhatsAppService : IWhatsAppService
    {
        private readonly ILogger<WhatsAppService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public WhatsAppService(ILogger<WhatsAppService> logger, IConfiguration configuration, HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<bool> SendMessageAsync(string to, string message)
        {
            try
            {
                var accessToken = _configuration["WhatsApp:AccessToken"];
                var phoneNumberId = _configuration["WhatsApp:PhoneNumberId"];

                if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(phoneNumberId))
                {
                    _logger.LogError("WhatsApp access token or phone number ID not configured");
                    return false;
                }

                var request = new WhatsAppSendMessageRequest
                {
                    To = to,
                    Text = new WhatsAppTextMessage { Body = message }
                };

                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var url = $"https://graph.facebook.com/v22.0/{phoneNumberId}/messages";

                _logger.LogInformation("Sending WhatsApp message to {To}: {Message}", to, message);

                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Successfully sent WhatsApp message to {To}, Response: {Response}", to, responseContent);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to send WhatsApp message to {To}, Status: {Status}, Error: {Error}", 
                        to, response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp message to {To}", to);
                return false;
            }
        }

        public async Task<bool> SendConfirmationMessageAsync(string to, string postTopic, string draft)
        {
            var message = $"📝 *New Post Created!*\n\n" +
                         $"*Topic:* {postTopic}\n\n" +
                         $"*Draft:*\n{draft}\n\n" +
                         $"Reply with:\n" +
                         $"• *1* to approve and schedule\n" +
                         $"• *0* to reject\n" +
                         $"• *2* to regenerate draft\n" +
                         $"• *3* to submit with changes (type '3 [your modified draft]')";

            return await SendMessageAsync(to, message);
        }

        public async Task<bool> SendStatusUpdateAsync(string to, string status, string postTopic)
        {
            string message;
            switch (status.ToLower())
            {
                case "approved":
                    message = $"✅ *Post Approved!*\n\nTopic: {postTopic}\n\nYour post has been approved and will be published shortly.";
                    break;
                case "rejected":
                    message = $"❌ *Post Rejected*\n\nTopic: {postTopic}\n\nYour post has been rejected. You can create a new one anytime.";
                    break;
                case "posted":
                    message = $"🚀 *Post Published!*\n\nTopic: {postTopic}\n\nYour post has been successfully published on LinkedIn!";
                    break;
                default:
                    message = $"📊 *Post Status Update*\n\nTopic: {postTopic}\n\nStatus: {status}";
                    break;
            }

            return await SendMessageAsync(to, message);
        }

        public async Task<bool> SendScheduledTimeAsync(string to, string postTopic, DateTime scheduledAt)
        {
            var scheduledTime = scheduledAt.ToString("dd/MM/yyyy HH:mm");
            var message = $"✅ *Post Scheduled!*\n\n" +
                         $"*Topic:* {postTopic}\n\n" +
                         $"*Scheduled Time:* {scheduledTime}\n\n" +
                         $"Your post will be published automatically at the scheduled time.";

            return await SendMessageAsync(to, message);
        }
    }
}
