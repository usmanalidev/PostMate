using PostmateAPI.Models;
using System.Text;
using System.Text.Json;

namespace PostmateAPI.Services
{
    public class LinkedInService : ILinkedInService
    {
        private readonly ILogger<LinkedInService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public LinkedInService(ILogger<LinkedInService> logger, IConfiguration configuration, HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<bool> PostToLinkedInAsync(Post post)
        {
            try
            {
                var accessToken = _configuration["LinkedIn:AccessToken"];
                var authorUrn = _configuration["LinkedIn:AuthorUrn"]; // e.g., "urn:li:person:xMR6YUcXmS"
                
                _logger.LogInformation("LinkedIn Access Token: {AccessToken}", accessToken);
                _logger.LogInformation("LinkedIn Author URN: {AuthorUrn}", authorUrn);
                
                if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(authorUrn))
                {
                    _logger.LogError("LinkedIn access token or author URN not configured");
                    return false;
                }

                var requestBody = new
                {
                    author = authorUrn,
                    lifecycleState = "PUBLISHED",
                    specificContent = new
                    {
                        com_linkedin_ugc_ShareContent = new
                        {
                            shareCommentary = new
                            {
                                text = post.Draft
                            },
                            shareMediaCategory = "NONE"
                        }
                    },
                    visibility = new
                    {
                        com_linkedin_ugc_MemberNetworkVisibility = "PUBLIC"
                    }
                };

                var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                _httpClient.DefaultRequestHeaders.Add("X-Restli-Protocol-Version", "2.0.0");

                _logger.LogInformation("Posting to LinkedIn: {Draft}", post.Draft);

                var response = await _httpClient.PostAsync("https://api.linkedin.com/v2/ugcPosts", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Successfully posted to LinkedIn: Post {PostId}, Response: {Response}", post.Id, responseContent);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to post to LinkedIn: Post {PostId}, Status: {Status}, Error: {Error}", 
                        post.Id, response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting to LinkedIn for post {PostId}", post.Id);
                return false;
            }
        }
    }
}
