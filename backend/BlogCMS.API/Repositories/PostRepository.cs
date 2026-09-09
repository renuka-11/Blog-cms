using BlogCMS.API.Data;
using BlogCMS.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BlogCMS.API.Repositories
{
    public class PostRepository : IPostRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<PostRepository> _logger;

        public PostRepository(
            ApplicationDbContext context, 
            IDistributedCache cache,
            ILogger<PostRepository> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<IEnumerable<Post>> GetAllPostsAsync()
        {
            const string cacheKey = "all_posts";
            
            try
            {
                // Try to get from cache
                var cachedPosts = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedPosts))
                {
                    _logger.LogInformation("Retrieved all posts from cache");
                    return JsonSerializer.Deserialize<IEnumerable<Post>>(cachedPosts) ?? new List<Post>();
                }

                // If not in cache, get from database
                var posts = await _context.Posts
                    .Where(p => p.IsPublished)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                // Store in cache
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                };
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(posts), cacheOptions);

                _logger.LogInformation("Retrieved {Count} posts from database and cached them", posts.Count);
                return posts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all posts");
                throw;
            }
        }

        public async Task<IEnumerable<Post>> GetPostsByCategoryAsync(string category)
        {
            string cacheKey = $"posts_category_{category}";
            
            try
            {
                var cachedPosts = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedPosts))
                {
                    return JsonSerializer.Deserialize<IEnumerable<Post>>(cachedPosts) ?? new List<Post>();
                }

                var posts = await _context.Posts
                    .Where(p => p.Category == category && p.IsPublished)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                };
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(posts), cacheOptions);

                return posts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving posts by category {Category}", category);
                throw;
            }
        }

        public async Task<Post?> GetPostByIdAsync(int id)
        {
            string cacheKey = $"post_{id}";

            try
            {
                var cachedPost = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedPost))
                {
                    _logger.LogInformation("Retrieved post {PostId} from cache", id);
                    return JsonSerializer.Deserialize<Post>(cachedPost);
                }

                var post = await _context.Posts.FindAsync(id);
                if (post != null)
                {
                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    };
                    await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(post), cacheOptions);
                    _logger.LogInformation("Retrieved post {PostId} from database and cached it", id);
                }

                return post;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving post {PostId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Post>> SearchPostsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllPostsAsync();

            searchTerm = searchTerm.ToLower();
            
            return await _context.Posts
                .Where(p => p.IsPublished && 
                           (p.Title.ToLower().Contains(searchTerm) || 
                            p.Content.ToLower().Contains(searchTerm) ||
                            p.Category.ToLower().Contains(searchTerm)))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Post> CreatePostAsync(Post post)
        {
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            // Invalidate cache
            await InvalidatePostCache();
            
            _logger.LogInformation("Created new post with ID {PostId}", post.Id);
            return post;
        }

        public async Task<Post> UpdatePostAsync(Post post)
        {
            post.UpdatedAt = DateTime.UtcNow;
            _context.Entry(post).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            await InvalidatePostCache(post.Id);
            
            _logger.LogInformation("Updated post {PostId}", post.Id);
            return post;
        }

        public async Task<bool> DeletePostAsync(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null)
                return false;

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            await InvalidatePostCache(id);
            
            _logger.LogInformation("Deleted post {PostId}", id);
            return true;
        }

        public async Task<bool> PostExistsAsync(int id)
        {
            return await _context.Posts.AnyAsync(p => p.Id == id);
        }

        private async Task InvalidatePostCache(int? specificPostId = null)
        {
            await _cache.RemoveAsync("all_posts");
            
            if (specificPostId.HasValue)
            {
                await _cache.RemoveAsync($"post_{specificPostId}");
            }
        }
    }
}