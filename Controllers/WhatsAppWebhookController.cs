using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostmateAPI.Data;
using PostmateAPI.DTOs;
using PostmateAPI.Models;
using PostmateAPI.Services;
using System.Text.Json;

namespace PostmateAPI.Controllers
{
    [ApiController]
    [Route("api/webhook/whatsapp")]
    public class WhatsAppController : ControllerBase
    {
        private const string VERIFY_TOKEN = "4oHNU2EJAbNnM89bdM3k80QPyDBspmfsDWBdgS3U0fE="; // must match what you use in WhatsApp
        
        private readonly ApplicationDbContext _context;
        private readonly IOpenAIService _openAIService;
        private readonly IWhatsAppService _whatsAppService;
        private readonly ILogger<WhatsAppController> _logger;

        public WhatsAppController(
            ApplicationDbContext context, 
            IOpenAIService openAIService, 
            IWhatsAppService whatsAppService,
            ILogger<WhatsAppController> logger)
        {
            _context = context;
            _openAIService = openAIService;
            _whatsAppService = whatsAppService;
            _logger = logger;
        }

        // GET: Verification
        [HttpGet]
        public IActionResult VerifyWebhook([FromQuery(Name = "hub.mode")] string mode,
                                           [FromQuery(Name = "hub.verify_token")] string token,
                                           [FromQuery(Name = "hub.challenge")] string challenge)
        {
            if (mode == "subscribe" && token == VERIFY_TOKEN)
            {
                _logger.LogInformation("WhatsApp webhook verified successfully");
                return Ok(challenge); // respond with challenge
            }

            _logger.LogWarning("WhatsApp webhook verification failed. Mode: {Mode}, Token: {Token}", mode, token);
            return Forbid();
        }

        // POST: Incoming messages
        [HttpPost]
        public async Task<IActionResult> ReceiveMessage([FromBody] WhatsAppWebhookMessage webhookMessage)
        {
            try
            {
                _logger.LogInformation("Received WhatsApp webhook: {WebhookData}", JsonSerializer.Serialize(webhookMessage));

                if (webhookMessage?.Entry?.FirstOrDefault()?.Changes?.FirstOrDefault()?.Value?.Messages?.FirstOrDefault() is not WhatsAppMessage message)
                {
                    _logger.LogWarning("No valid message found in webhook");
                    return Ok();
                }

                var from = message.From;
                var messageText = message.Text?.Body?.Trim() ?? "";

                _logger.LogInformation("Processing WhatsApp message from {From}: {Message}", from, messageText);

                // Handle different message types
                if (string.IsNullOrEmpty(messageText))
                {
                    _logger.LogWarning("Empty message received from {From}", from);
                    return Ok();
                }

                // Check if this is a help request
                if (messageText.ToLower() == "help" || messageText.ToLower() == "post types" || messageText.ToLower() == "types")
                {
                    await _whatsAppService.SendPostTypeHelpMessageAsync(from);
                }
                // Check if this is a confirmation response (1, 0, 2, or 3)
                else if (messageText == "1" || messageText == "0" || messageText == "2" || messageText == "3")
                {
                    await HandleConfirmationResponse(from, messageText);
                }
                // Check if this is a scheduling option
                else if (IsSchedulingOption(messageText))
                {
                    await HandleSchedulingResponse(from, messageText);
                }
                else
                {
                    // Check if this is a modified draft submission (starts with "3 ")
                    if (messageText.StartsWith("3 "))
                    {
                        var modifiedDraft = messageText.Substring(2).Trim();
                        await HandleModifiedDraftSubmission(from, modifiedDraft);
                    }
                    else
                    {
                        // Check if message contains post type specification
                        var (topic, postType) = ParseTopicAndPostType(messageText);
                        await HandleNewPostRequest(from, topic, postType);
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing WhatsApp webhook message");
                return StatusCode(500, "Internal server error");
            }
        }

        private (string topic, string postType) ParseTopicAndPostType(string messageText)
        {
            // Supported post types
            var validPostTypes = new[] { "educational", "listicle", "storytelling", "thought-leadership", "interview", "difference" };
            
            // Check if message starts with a post type
            foreach (var postType in validPostTypes)
            {
                if (messageText.ToLower().StartsWith($"{postType}:"))
                {
                    var topic = messageText.Substring(postType.Length + 1).Trim();
                    if (!string.IsNullOrEmpty(topic))
                    {
                        return (topic, postType);
                    }
                }
            }
            
            // If no post type specified, return the message as topic with default post type
            return (messageText, "educational");
        }

        private DateTime? ParseSchedulingTime(string schedulingInput)
        {
            var input = schedulingInput.ToLower().Trim();
            var now = DateTime.UtcNow;

            return input switch
            {
                "now" => now.AddMinutes(1), // Schedule for 1 minute from now to ensure it's in the future
                "1h" => now.AddHours(1),
                "4h" => now.AddHours(4),
                "12h" => now.AddHours(12),
                "1d" => now.AddDays(1),
                "2d" => now.AddDays(2),
                "1w" => now.AddDays(7),
                _ => null // Invalid input
            };
        }

        private bool IsSchedulingOption(string messageText)
        {
            var validOptions = new[] { "now", "1h", "4h", "12h", "1d", "2d", "1w" };
            return validOptions.Contains(messageText.ToLower().Trim());
        }

        private async Task HandleSchedulingResponse(string from, string schedulingOption)
        {
            try
            {
                _logger.LogInformation("Processing scheduling response from {From}: {Option}", from, schedulingOption);

                // Get the latest pending post that's waiting for scheduling
                var latestPendingPost = await _context.Posts
                    .Where(p => p.Status == "Pending" && p.ScheduledAt == null)
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefaultAsync();

                if (latestPendingPost == null)
                {
                    _logger.LogWarning("No pending post found for scheduling from {From}", from);
                    await _whatsAppService.SendMessageAsync(from, 
                        "❌ No pending posts found. Please create a new post first.");
                    return;
                }

                var scheduledTime = ParseSchedulingTime(schedulingOption);
                if (scheduledTime == null)
                {
                    await _whatsAppService.SendMessageAsync(from, 
                        "❌ Invalid scheduling option. Please use: now, 1h, 4h, 12h, 1d, 2d, or 1w");
                    return;
                }

                // Update the post with scheduled time and approve it
                latestPendingPost.ScheduledAt = scheduledTime.Value;
                latestPendingPost.Status = "Approved";
                await _context.SaveChangesAsync();

                // Send confirmation with scheduled time
                await _whatsAppService.SendScheduledTimeAsync(from, latestPendingPost.Topic, scheduledTime.Value);
                
                _logger.LogInformation("Post {PostId} scheduled by {From} for {ScheduledAt}", 
                    latestPendingPost.Id, from, scheduledTime.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling scheduling response from {From}", from);
                
                await _whatsAppService.SendMessageAsync(from, 
                    "❌ Sorry, there was an error processing your scheduling request. Please try again.");
            }
        }

        private async Task HandleNewPostRequest(string from, string topic, string postType = "educational")
        {
            try
            {
                _logger.LogInformation("Creating new post for user {From} with topic: {Topic} and post type: {PostType}", from, topic, postType);

                // Create new post with draft status
                var post = new Post
                {
                    Topic = topic,
                    PostType = postType,
                    Status = "Draft", // New status for initial creation
                    CreatedAt = DateTime.UtcNow
                };

                _context.Posts.Add(post);
                await _context.SaveChangesAsync();

                // Generate draft using OpenAI
                try
                {
                    var draft = await _openAIService.GenerateLinkedInPostAsync(topic, post.PostType);
                    post.Draft = draft;
                    post.Status = "Pending"; // Change to pending after draft is generated
                    await _context.SaveChangesAsync();

                    // Send confirmation message to user
                    await _whatsAppService.SendConfirmationMessageAsync(from, topic, draft, postType);

                    _logger.LogInformation("Post {PostId} created and confirmation sent to {From}", post.Id, from);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating draft for post {PostId}", post.Id);
                    
                    // Send error message to user
                    await _whatsAppService.SendMessageAsync(from, 
                        $"❌ Sorry, I couldn't generate a draft for your topic: {topic}\n\nPlease try again with a different topic.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling new post request from {From}", from);
                
                // Send error message to user
                await _whatsAppService.SendMessageAsync(from, 
                    "❌ Sorry, there was an error processing your request. Please try again.");
            }
        }

        private async Task HandleConfirmationResponse(string from, string response)
        {
            try
            {
                _logger.LogInformation("Processing confirmation response from {From}: {Response}", from, response);

                // Get the latest pending post (you might want to associate posts with users in a real implementation)
                var latestPendingPost = await _context.Posts
                    .Where(p => p.Status == "Pending")
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefaultAsync();

                if (latestPendingPost == null)
                {
                    _logger.LogWarning("No pending post found for confirmation from {From}", from);
                    await _whatsAppService.SendMessageAsync(from, 
                        "❌ No pending posts found. Please create a new post first.");
                    return;
                }

                if (response == "1") // Approve and schedule
                {
                    // Send scheduling options instead of immediately scheduling
                    await _whatsAppService.SendSchedulingOptionsAsync(from, latestPendingPost.Topic);
                    
                    _logger.LogInformation("Scheduling options sent to {From} for post {PostId}", from, latestPendingPost.Id);
                }
                else if (response == "0") // Reject
                {
                    latestPendingPost.Status = "Rejected";
                    await _context.SaveChangesAsync();

                    await _whatsAppService.SendStatusUpdateAsync(from, "Rejected", latestPendingPost.Topic);
                    
                    _logger.LogInformation("Post {PostId} rejected by {From}", latestPendingPost.Id, from);
                }
                else if (response == "2") // Regenerate draft
                {
                    try
                    {
                        var newDraft = await _openAIService.GenerateLinkedInPostAsync(latestPendingPost.Topic, latestPendingPost.PostType);
                        latestPendingPost.Draft = newDraft;
                        await _context.SaveChangesAsync();

                        await _whatsAppService.SendConfirmationMessageAsync(from, latestPendingPost.Topic, newDraft, latestPendingPost.PostType);
                        
                        _logger.LogInformation("Post {PostId} draft regenerated for {From}", latestPendingPost.Id, from);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error regenerating draft for post {PostId}", latestPendingPost.Id);
                        await _whatsAppService.SendMessageAsync(from, 
                            "❌ Sorry, I couldn't regenerate the draft. Please try again.");
                    }
                }
                else if (response == "3") // Submit with changes
                {
                    await _whatsAppService.SendMessageAsync(from, 
                        "📝 *Submit with Changes*\n\nPlease type your modified draft in the following format:\n\n*3 [your modified draft here]*\n\nExample:\n*3 This is my modified version of the post with my own changes.*");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling confirmation response from {From}", from);
                
                await _whatsAppService.SendMessageAsync(from, 
                    "❌ Sorry, there was an error processing your confirmation. Please try again.");
            }
        }

        private async Task HandleModifiedDraftSubmission(string from, string modifiedDraft)
        {
            try
            {
                _logger.LogInformation("Processing modified draft submission from {From}: {Draft}", from, modifiedDraft);

                // Get the latest pending post
                var latestPendingPost = await _context.Posts
                    .Where(p => p.Status == "Pending")
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefaultAsync();

                if (latestPendingPost == null)
                {
                    _logger.LogWarning("No pending post found for modified draft submission from {From}", from);
                    await _whatsAppService.SendMessageAsync(from, 
                        "❌ No pending posts found. Please create a new post first.");
                    return;
                }

                if (string.IsNullOrEmpty(modifiedDraft))
                {
                    await _whatsAppService.SendMessageAsync(from, 
                        "❌ Please provide your modified draft. Format: *3 [your modified draft here]*");
                    return;
                }

                // Update the post with the modified draft
                latestPendingPost.Draft = modifiedDraft;
                latestPendingPost.Status = "Approved";
                latestPendingPost.ScheduledAt = DateTime.UtcNow.AddMinutes(5); // Schedule for 5 minutes from now
                await _context.SaveChangesAsync();

                // Send confirmation with scheduled time
                await _whatsAppService.SendScheduledTimeAsync(from, latestPendingPost.Topic, latestPendingPost.ScheduledAt.Value);
                
                _logger.LogInformation("Post {PostId} updated with modified draft by {From} and scheduled for {ScheduledAt}", 
                    latestPendingPost.Id, from, latestPendingPost.ScheduledAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling modified draft submission from {From}", from);
                
                await _whatsAppService.SendMessageAsync(from, 
                    "❌ Sorry, there was an error processing your modified draft. Please try again.");
            }
        }
    }
}
