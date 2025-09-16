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

        public async Task<string> GenerateLinkedInPostAsync(string topic, string postType = "educational")
        {
            try
            {
                // Use Google AI Studio (Gemini)
                return await GenerateWithGoogleAIAsync(topic, postType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google AI failed for topic: {Topic} with postType: {PostType}, using template fallback", topic, postType);
                return GenerateFallbackPost(topic);
            }
        }

        private async Task<string> GenerateWithGoogleAIAsync(string topic, string postType)
        {
            using var httpClient = new HttpClient();
            
            // Get API key from configuration
            var apiKey = _configuration["GoogleAI:ApiKey"];
            
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Google AI API key is not configured. Please set GoogleAI:ApiKey in appsettings.json");
            }

            var prompt = 
$"""
You are an AI assistant that generates LinkedIn posts. 

The user will first choose a post type (educational, listicle, storytelling, thought-leadership, interview, difference) 
and then provide a topic.

Follow these rules:
If educational: Start with a hook, then explain the concept, give an example, share a key takeaway, and end with a call to action.
If listicle: Write a short intro, then provide 5 to 7 clear numbered points, and finish with an engaging conclusion.
If storytelling: Begin with a relatable opening, tell a short story, share the lesson learned, and invite readers to reflect.
If thought-leadership: Share an insight, provide context, pose a challenge or opportunity, and spark discussion.
If interview: Structure the post in a quick-revision style with a one-sentence definition, core concept with a simple analogy, 3 to 4 key points in bullet form, and a short Q&A with a common interview question and crisp answer. Keep the tone concise, engaging, and beginner-friendly. Format the text with emojis or bullets so it's easy to skim in a LinkedIn feed.
If difference: Create a comparison post that starts with a brief introduction, then presents a clear table format showing differences between two or more concepts. Use simple table structure with clear headers and concise comparison points. End with a summary of when to use each option. Make it easy to read and understand the key distinctions.

General rules for all post types:
Use a professional but human and engaging tone.
Keep it to 5 to 7 sentences, or 2 short paragraphs if needed.
Avoid jargon words and keep it clear and simple.
End with 2 to 3 relevant hashtags after a line break.
Make it shareable and thought-provoking.

Important writing style guidelines:
Do not use dashes, hyphens, or bullet points in your content.
Write in natural, conversational human language.
Use simple words and clear sentences.
Avoid technical formatting symbols.

Now, write the LinkedIn post.  
Topic: {topic}  
Post type: {postType}
""";

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
