using BlogCMS.API.DTOs;

namespace BlogCMS.API.Services
{
    public interface IPostService
    {
        Task<IEnumerable<PostResponseDto>> GetAllPostsAsync();
        Task<IEnumerable<PostResponseDto>> GetPostsByCategoryAsync(string category);
        Task<PostResponseDto?> GetPostByIdAsync(int id);
        Task<IEnumerable<PostResponseDto>> SearchPostsAsync(string searchTerm);
        Task<PostResponseDto> CreatePostAsync(CreatePostDto createPostDto, string createdBy);
        Task<PostResponseDto?> UpdatePostAsync(int id, UpdatePostDto updatePostDto);
        Task<bool> DeletePostAsync(int id);
    }
}