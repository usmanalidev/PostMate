using System.Text.Json;

namespace PostmateAPI.Services
{
    public class OpenAIService : IOpenAIService
    {
        private readonly ILogger<OpenAIService> _logger;
        private readonly IConfiguration _configuration;

        public OpenAIService(IConfiguration configuration, ILogger<OpenAIService> logger)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<string> GenerateLinkedInPostAsync(string topic)
        {
            try
            {
                // Use Google AI Studio (Gemini)
                return await GenerateWithGoogleAIAsync(topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google AI failed for topic: {Topic}, using template fallback", topic);
                return GenerateFallbackPost(topic);
            }
        }

        private async Task<string> GenerateWithGoogleAIAsync(string topic)
        {
            using var httpClient = new HttpClient();
            
            // Get API key from configuration
            var apiKey = _configuration["GoogleAI:ApiKey"];
            
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Google AI API key is not configured. Please set GoogleAI:ApiKey in appsettings.json");
            }

            var prompt = $"Write a professional LinkedIn post about {topic}. " +
                        "Tone: professional but human, engaging. " +
                        "Length: 5-7 sentences.It can be in 2 paragraphs if needed in context. for example if needed to add definition, example use proper headings according to the context. " +
                        "Not use Jargon words. " +
                        "Include 2-3 relevant hashtags after new line in the bottom. " +
                        "Make it shareable and thought-provoking.";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            // Add the API key as header (not query parameter)
            httpClient.DefaultRequestHeaders.Add("X-goog-api-key", apiKey);
            
            var response = await httpClient.PostAsync("https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Google AI API request failed: {response.StatusCode} - {errorContent}");
            }
            
            var responseContent = await response.Content.ReadAsStringAsync();
            // Gemini's response uses lowercase property names, so we need to use PropertyNameCaseInsensitive = true
            var result = JsonSerializer.Deserialize<GoogleAIResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text != null)
            {
                return result.Candidates!.First().Content!.Parts!.First().Text!.Trim();
            }
            
            throw new InvalidOperationException("No content generated from Google AI API");
        }


        private string GenerateFallbackPost(string topic)
        {
            return "Currently server is busy please try again later, sorry for inconvenience!";
        }
    }

    // Response models for Google AI API
    public class GoogleAIResponse
    {
        public Candidate[]? Candidates { get; set; }
    }

    public class Candidate
    {
        public Content? Content { get; set; }
    }

    public class Content
    {
        public Part[]? Parts { get; set; }
    }

    public class Part
    {
        public string? Text { get; set; }
    }

}
