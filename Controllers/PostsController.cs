using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostmateAPI.Data;
using PostmateAPI.DTOs;
using PostmateAPI.Models;
using PostmateAPI.Services;

namespace PostmateAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PostsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IOpenAIService _openAIService;
        private readonly ILogger<PostsController> _logger;

        public PostsController(ApplicationDbContext context, IOpenAIService openAIService, ILogger<PostsController> logger)
        {
            _context = context;
            _openAIService = openAIService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PostResponse>>> GetPosts()
        {
            try
            {
                var posts = await _context.Posts
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new PostResponse
                    {
                        Id = p.Id,
                        Topic = p.Topic,
                        PostType = p.PostType,
                        Draft = p.Draft,
                        Status = p.Status,
                        ScheduledAt = p.ScheduledAt,
                        CreatedAt = p.CreatedAt
                    })
                    .ToListAsync();

                return Ok(posts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving posts");
                return StatusCode(500, "An error occurred while retrieving posts");
            }
        }

        [HttpPost]
        public async Task<ActionResult<PostResponse>> CreatePost([FromBody] CreatePostRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Topic))
                {
                    return BadRequest("Topic is required");
                }

                var post = new Post
                {
                    Topic = request.Topic,
                    PostType = request.PostType,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Posts.Add(post);
                await _context.SaveChangesAsync();

                // Generate draft using OpenAI
                try
                {
                    var draft = await _openAIService.GenerateLinkedInPostAsync(request.Topic, request.PostType);
                    post.Draft = draft;
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating draft for post {PostId}", post.Id);
                    // Continue without draft - it can be generated later
                }

                var response = new PostResponse
                {
                    Id = post.Id,
                    Topic = post.Topic,
                    PostType = post.PostType,
                    Draft = post.Draft,
                    Status = post.Status,
                    ScheduledAt = post.ScheduledAt,
                    CreatedAt = post.CreatedAt
                };

                _logger.LogInformation("Created new post {PostId} with topic: {Topic}", post.Id, request.Topic);
                return CreatedAtAction(nameof(GetPosts), new { id = post.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating post with topic: {Topic}", request.Topic);
                return StatusCode(500, "An error occurred while creating the post");
            }
        }

        [HttpPost("{id}/approve")]
        public async Task<ActionResult> ApprovePost(int id)
        {
            try
            {
                var post = await _context.Posts.FindAsync(id);
                if (post == null)
                {
                    return NotFound("Post not found");
                }

                post.Status = "Approved";
                post.ScheduledAt = DateTime.UtcNow.AddMinutes(5); // Schedule for 5 minutes from now
                await _context.SaveChangesAsync();

                _logger.LogInformation("Post {PostId} approved and scheduled for {ScheduledAt}", id, post.ScheduledAt);
                return Ok(new { message = "Post approved and scheduled", scheduledAt = post.ScheduledAt });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving post {PostId}", id);
                return StatusCode(500, "An error occurred while approving the post");
            }
        }

        [HttpPost("{id}/reject")]
        public async Task<ActionResult> RejectPost(int id)
        {
            try
            {
                var post = await _context.Posts.FindAsync(id);
                if (post == null)
                {
                    return NotFound("Post not found");
                }

                post.Status = "Rejected";
                await _context.SaveChangesAsync();

                _logger.LogInformation("Post {PostId} rejected", id);
                return Ok(new { message = "Post rejected" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting post {PostId}", id);
                return StatusCode(500, "An error occurred while rejecting the post");
            }
        }
    }
}
