using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PostmateAPI.Services
{
    public class OpenAIService : IOpenAIService
    {
        private const string CursorApiBaseUrl = "https://api.cursor.com/v1";
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan MaxWaitTime = TimeSpan.FromMinutes(3);

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
                return await GenerateWithCursorAsync(topic, postType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cursor API failed for topic: {Topic} with postType: {PostType}, using template fallback", topic, postType);
                return GenerateFallbackPost(topic);
            }
        }

        private async Task<string> GenerateWithCursorAsync(string topic, string postType)
        {
            var apiKey = _configuration["Cursor:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Cursor API key is not configured. Please set Cursor:ApiKey in appsettings.json");
            }

            var modelId = _configuration["Cursor:Model"];
            if (string.IsNullOrWhiteSpace(modelId))
            {
                modelId = "composer-2.5";
            }

            using var httpClient = CreateAuthenticatedClient(apiKey);

            var prompt = BuildPrompt(topic, postType);
            var createRequest = new
            {
                prompt = new { text = prompt },
                model = new { id = modelId }
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createRequest, jsonOptions),
                Encoding.UTF8,
                "application/json");

            var createResponse = await httpClient.PostAsync($"{CursorApiBaseUrl}/agents", createContent);
            var createResponseBody = await createResponse.Content.ReadAsStringAsync();

            if (!createResponse.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Cursor API create agent failed: {createResponse.StatusCode} - {createResponseBody}");
            }

            var createResult = JsonSerializer.Deserialize<CursorCreateAgentResponse>(createResponseBody, jsonOptions)
                ?? throw new InvalidOperationException("Cursor API returned an empty create-agent response");

            var agentId = createResult.Agent?.Id ?? throw new InvalidOperationException("Cursor API did not return an agent id");
            var runId = createResult.Run?.Id ?? createResult.Agent?.LatestRunId
                ?? throw new InvalidOperationException("Cursor API did not return a run id");

            _logger.LogInformation("Cursor agent created: AgentId={AgentId}, RunId={RunId}", agentId, runId);

            var result = await PollRunUntilCompleteAsync(httpClient, agentId, runId, jsonOptions);
            if (string.IsNullOrWhiteSpace(result))
            {
                throw new InvalidOperationException("No content generated from Cursor API");
            }

            return result.Trim();
        }

        private static async Task<string?> PollRunUntilCompleteAsync(
            HttpClient httpClient,
            string agentId,
            string runId,
            JsonSerializerOptions jsonOptions)
        {
            var deadline = DateTime.UtcNow.Add(MaxWaitTime);
            var terminalStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "FINISHED", "ERROR", "CANCELLED", "EXPIRED"
            };

            while (DateTime.UtcNow < deadline)
            {
                var runResponse = await httpClient.GetAsync($"{CursorApiBaseUrl}/agents/{agentId}/runs/{runId}");
                var runBody = await runResponse.Content.ReadAsStringAsync();

                if (!runResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Cursor API get run failed: {runResponse.StatusCode} - {runBody}");
                }

                var run = JsonSerializer.Deserialize<CursorRunResponse>(runBody, jsonOptions)
                    ?? throw new InvalidOperationException("Cursor API returned an empty run response");

                if (run.Status != null && terminalStatuses.Contains(run.Status))
                {
                    if (string.Equals(run.Status, "FINISHED", StringComparison.OrdinalIgnoreCase))
                    {
                        return run.Result;
                    }

                    throw new InvalidOperationException(
                        $"Cursor API run ended with status {run.Status}: {run.Result ?? "no result text"}");
                }

                await Task.Delay(PollInterval);
            }

            throw new TimeoutException($"Cursor API run {runId} did not complete within {MaxWaitTime.TotalMinutes} minutes");
        }

        private static HttpClient CreateAuthenticatedClient(string apiKey)
        {
            var httpClient = new HttpClient();
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{apiKey}:"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            return httpClient;
        }

        private static string BuildPrompt(string topic, string postType) =>
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

Return only the final LinkedIn post text. Do not use tools, edit files, or add commentary.
""";

        private static string GenerateFallbackPost(string topic)
        {
            return "Currently server is busy please try again later, sorry for inconvenience!";
        }
    }

    public class CursorCreateAgentResponse
    {
        public CursorAgentInfo? Agent { get; set; }
        public CursorRunResponse? Run { get; set; }
    }

    public class CursorAgentInfo
    {
        public string? Id { get; set; }
        public string? LatestRunId { get; set; }
    }

    public class CursorRunResponse
    {
        public string? Id { get; set; }
        public string? AgentId { get; set; }
        public string? Status { get; set; }
        public string? Result { get; set; }
    }
}
