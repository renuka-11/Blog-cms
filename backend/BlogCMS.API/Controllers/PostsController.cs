using BlogCMS.API.DTOs;
using BlogCMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Asp.Versioning;
using System.IdentityModel.Tokens.Jwt;  // Add this line

namespace BlogCMS.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class PostsController : ControllerBase
    {
        private readonly IPostService _postService;
        private readonly ILogger<PostsController> _logger;

        public PostsController(IPostService postService, ILogger<PostsController> logger)
        {
            _postService = postService;
            _logger = logger;
        }

        /// <summary>
        /// Get all published posts
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<PostResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<PostResponseDto>>> GetPosts([FromQuery] string? category)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(category))
                {
                    var posts = await _postService.GetPostsByCategoryAsync(category);
                    return Ok(posts);
                }

                var allPosts = await _postService.GetAllPostsAsync();
                return Ok(allPosts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting posts");
                return StatusCode(500, new { error = "An error occurred while retrieving posts" });
            }
        }

        /// <summary>
        /// Get a specific post by ID
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PostResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PostResponseDto>> GetPost(int id)
        {
            try
            {
                var post = await _postService.GetPostByIdAsync(id);
                if (post == null)
                {
                    return NotFound(new { error = $"Post with ID {id} not found" });
                }

                return Ok(post);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting post {PostId}", id);
                return StatusCode(500, new { error = "An error occurred while retrieving the post" });
            }
        }

        /// <summary>
        /// Search posts by title, content, or category
        /// </summary>
        [HttpGet("search")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<PostResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<PostResponseDto>>> SearchPosts([FromQuery] string q)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return await GetPosts(null);
                }

                var posts = await _postService.SearchPostsAsync(q);
                return Ok(posts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching posts with term: {SearchTerm}", q);
                return StatusCode(500, new { error = "An error occurred while searching posts" });
            }
        }

        /// <summary>
        /// Create a new post
        /// </summary>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(PostResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<PostResponseDto>> CreatePost(CreatePostDto createPostDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? 
                            "anonymous";
                
                var post = await _postService.CreatePostAsync(createPostDto, userId);

                return CreatedAtAction(nameof(GetPost), new { id = post.Id, version = "1.0" }, post);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating post");
                return StatusCode(500, new { error = "An error occurred while creating the post" });
            }
        }

        /// <summary>
        /// Update an existing post
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(PostResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PostResponseDto>> UpdatePost(int id, UpdatePostDto updatePostDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var post = await _postService.UpdatePostAsync(id, updatePostDto);
                if (post == null)
                {
                    return NotFound(new { error = $"Post with ID {id} not found" });
                }

                return Ok(post);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating post {PostId}", id);
                return StatusCode(500, new { error = "An error occurred while updating the post" });
            }
        }

        /// <summary>
        /// Delete a post
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePost(int id)
        {
            try
            {
                var result = await _postService.DeletePostAsync(id);
                if (!result)
                {
                    return NotFound(new { error = $"Post with ID {id} not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting post {PostId}", id);
                return StatusCode(500, new { error = "An error occurred while deleting the post" });
            }
        }
    }
}