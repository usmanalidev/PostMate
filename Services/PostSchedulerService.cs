using Microsoft.EntityFrameworkCore;
using PostmateAPI.Data;
using PostmateAPI.Models;

namespace PostmateAPI.Services
{
    public class PostSchedulerService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PostSchedulerService> _logger;

        public PostSchedulerService(IServiceProvider serviceProvider, ILogger<PostSchedulerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task ProcessScheduledPosts()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var postsToPublish = await context.Posts
                    .Where(p => p.Status == "Approved" && 
                               p.ScheduledAt.HasValue && 
                               p.ScheduledAt <= DateTime.UtcNow)
                    .ToListAsync();

                foreach (var post in postsToPublish)
                {
                    try
                    {
                        // Simulate posting to LinkedIn
                        await PostToLinkedIn(post);

                        // Update post status
                        post.Status = "Posted";
                        await context.SaveChangesAsync();

                        // Send WhatsApp confirmation (simulated)
                        await SendWhatsAppConfirmation(post);

                        _logger.LogInformation("Successfully posted and updated post {PostId}", post.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing post {PostId}", post.Id);
                    }
                }

                if (postsToPublish.Any())
                {
                    _logger.LogInformation("Processed {Count} scheduled posts", postsToPublish.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessScheduledPosts");
            }
        }

        private async Task PostToLinkedIn(Post post)
        {
            using var scope = _serviceProvider.CreateScope();
            var linkedInService = scope.ServiceProvider.GetRequiredService<ILinkedInService>();
            
            var success = await linkedInService.PostToLinkedInAsync(post);
            if (!success)
            {
                throw new Exception($"Failed to post to LinkedIn for post {post.Id}");
            }
        }

        private async Task SendWhatsAppConfirmation(Post post)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
                
                // For now, we'll use a default phone number. In a real implementation,
                // you would store the user's phone number with the post
                var defaultPhoneNumber = "966555914872"; // You can make this configurable
                
                await whatsAppService.SendStatusUpdateAsync(defaultPhoneNumber, "Posted", post.Topic);
                
                _logger.LogInformation("WhatsApp confirmation sent for post {PostId}", post.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp confirmation for post {PostId}", post.Id);
            }
        }
    }
}
