using BlogCMS.API.DTOs;
using BlogCMS.API.Models;
using BlogCMS.API.Repositories;
using BlogCMS.API.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace BlogCMS.Tests.Services
{
    public class PostServiceTests
    {
        private readonly Mock<IPostRepository> _mockRepository;
        private readonly Mock<ILogger<PostService>> _mockLogger;
        private readonly PostService _service;

        public PostServiceTests()
        {
            _mockRepository = new Mock<IPostRepository>();
            _mockLogger = new Mock<ILogger<PostService>>();
            _service = new PostService(_mockRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetAllPostsAsync_ReturnsAllPosts()
        {
            // Arrange
            var expectedPosts = new List<Post>
            {
                new Post { Id = 1, Title = "Post 1", Category = "Tech", Content = "Content 1" },
                new Post { Id = 2, Title = "Post 2", Category = "College", Content = "Content 2" }
            };

            _mockRepository.Setup(r => r.GetAllPostsAsync())
                .ReturnsAsync(expectedPosts);

            // Act
            var result = await _service.GetAllPostsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Count().Should().Be(2);
            result.First().Id.Should().Be(1);
        }

        [Fact]
        public async Task GetPostByIdAsync_WithValidId_ReturnsPost()
        {
            // Arrange
            int postId = 1;
            var expectedPost = new Post 
            { 
                Id = postId, 
                Title = "Test Post", 
                Category = "Tech", 
                Content = "Test Content" 
            };

            _mockRepository.Setup(r => r.GetPostByIdAsync(postId))
                .ReturnsAsync(expectedPost);

            // Act
            var result = await _service.GetPostByIdAsync(postId);

            // Assert
            result.Should().NotBeNull();
            result?.Id.Should().Be(postId);
            result?.Title.Should().Be("Test Post");
        }

        [Fact]
        public async Task GetPostByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            int postId = 999;
            _mockRepository.Setup(r => r.GetPostByIdAsync(postId))
                .ReturnsAsync((Post?)null);

            // Act
            var result = await _service.GetPostByIdAsync(postId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreatePostAsync_ValidData_CreatesPost()
        {
            // Arrange
            var createDto = new CreatePostDto
            {
                Title = "New Post",
                Category = "Tech",
                Content = "Test Content"
            };

            var createdPost = new Post
            {
                Id = 1,
                Title = createDto.Title,
                Category = createDto.Category,
                Content = createDto.Content,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test-user"
            };

            _mockRepository.Setup(r => r.CreatePostAsync(It.IsAny<Post>()))
                .ReturnsAsync(createdPost);

            // Act
            var result = await _service.CreatePostAsync(createDto, "test-user");

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.Title.Should().Be(createDto.Title);
            result.CreatedBy.Should().Be("test-user");
            
            _mockRepository.Verify(r => r.CreatePostAsync(It.Is<Post>(p => 
                p.Title == createDto.Title && 
                p.Category == createDto.Category)), Times.Once);
        }
    }
}