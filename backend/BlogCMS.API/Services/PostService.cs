using BlogCMS.API.DTOs;
using BlogCMS.API.Models;
using BlogCMS.API.Repositories;

namespace BlogCMS.API.Services
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepository;
        private readonly ILogger<PostService> _logger;

        public PostService(IPostRepository postRepository, ILogger<PostService> logger)
        {
            _postRepository = postRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<PostResponseDto>> GetAllPostsAsync()
        {
            var posts = await _postRepository.GetAllPostsAsync();
            return MapToDtoList(posts);
        }

        public async Task<IEnumerable<PostResponseDto>> GetPostsByCategoryAsync(string category)
        {
            var posts = await _postRepository.GetPostsByCategoryAsync(category);
            return MapToDtoList(posts);
        }

        public async Task<PostResponseDto?> GetPostByIdAsync(int id)
        {
            var post = await _postRepository.GetPostByIdAsync(id);
            return post == null ? null : MapToDto(post);
        }

        public async Task<IEnumerable<PostResponseDto>> SearchPostsAsync(string searchTerm)
        {
            var posts = await _postRepository.SearchPostsAsync(searchTerm);
            return MapToDtoList(posts);
        }

        public async Task<PostResponseDto> CreatePostAsync(CreatePostDto createPostDto, string createdBy)
        {
            var post = new Post
            {
                Title = createPostDto.Title,
                Category = createPostDto.Category,
                Content = createPostDto.Content,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            var createdPost = await _postRepository.CreatePostAsync(post);
            _logger.LogInformation("Post created successfully with ID: {PostId}", createdPost.Id);
            
            return MapToDto(createdPost);
        }

        public async Task<PostResponseDto?> UpdatePostAsync(int id, UpdatePostDto updatePostDto)
        {
            var existingPost = await _postRepository.GetPostByIdAsync(id);
            if (existingPost == null)
            {
                _logger.LogWarning("Attempted to update non-existent post with ID: {PostId}", id);
                return null;
            }

            existingPost.Title = updatePostDto.Title;
            existingPost.Category = updatePostDto.Category;
            existingPost.Content = updatePostDto.Content;
            existingPost.UpdatedAt = DateTime.UtcNow;
            existingPost.IsPublished = updatePostDto.IsPublished;

            var updatedPost = await _postRepository.UpdatePostAsync(existingPost);
            _logger.LogInformation("Post updated successfully with ID: {PostId}", id);
            
            return MapToDto(updatedPost);
        }

        public async Task<bool> DeletePostAsync(int id)
        {
            var result = await _postRepository.DeletePostAsync(id);
            if (result)
            {
                _logger.LogInformation("Post deleted successfully with ID: {PostId}", id);
            }
            else
            {
                _logger.LogWarning("Attempted to delete non-existent post with ID: {PostId}", id);
            }
            return result;
        }

        private PostResponseDto MapToDto(Post post)
        {
            return new PostResponseDto
            {
                Id = post.Id,
                Title = post.Title,
                Category = post.Category,
                Content = post.Content,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                CreatedBy = post.CreatedBy
            };
        }

        private IEnumerable<PostResponseDto> MapToDtoList(IEnumerable<Post> posts)
        {
            return posts.Select(MapToDto);
        }
    }
}