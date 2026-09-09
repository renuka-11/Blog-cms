using BlogCMS.API.Models;

namespace BlogCMS.API.Repositories
{
    public interface IPostRepository
    {
        Task<IEnumerable<Post>> GetAllPostsAsync();
        Task<IEnumerable<Post>> GetPostsByCategoryAsync(string category);
        Task<Post?> GetPostByIdAsync(int id);
        Task<IEnumerable<Post>> SearchPostsAsync(string searchTerm);
        Task<Post> CreatePostAsync(Post post);
        Task<Post> UpdatePostAsync(Post post);
        Task<bool> DeletePostAsync(int id);
        Task<bool> PostExistsAsync(int id);
    }
}