using BlogCMS.API.Controllers;
using BlogCMS.API.DTOs;
using BlogCMS.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace BlogCMS.Tests.Controllers
{
    public class PostsControllerTests
    {
        private readonly Mock<IPostService> _mockPostService;
        private readonly Mock<ILogger<PostsController>> _mockLogger;
        private readonly PostsController _controller;

        public PostsControllerTests()
        {
            _mockPostService = new Mock<IPostService>();
            _mockLogger = new Mock<ILogger<PostsController>>();
            _controller = new PostsController(_mockPostService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetPosts_WithoutCategory_ReturnsAllPosts()
        {
            // Arrange
            var expectedPosts = new List<PostResponseDto>
            {
                new PostResponseDto { Id = 1, Title = "Post 1", Category = "Tech" },
                new PostResponseDto { Id = 2, Title = "Post 2", Category = "College" }
            };

            _mockPostService.Setup(s => s.GetAllPostsAsync())
                .ReturnsAsync(expectedPosts);

            // Act
            var result = await _controller.GetPosts(null);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult?.StatusCode.Should().Be(200);

            var posts = okResult?.Value as IEnumerable<PostResponseDto>;
            posts.Should().NotBeNull();
            posts?.Count().Should().Be(2);
        }

        [Fact]
        public async Task GetPosts_WithCategory_ReturnsFilteredPosts()
        {
            // Arrange
            var category = "Tech";
            var expectedPosts = new List<PostResponseDto>
            {
                new PostResponseDto { Id = 1, Title = "Post 1", Category = category }
            };

            _mockPostService.Setup(s => s.GetPostsByCategoryAsync(category))
                .ReturnsAsync(expectedPosts);

            // Act
            var result = await _controller.GetPosts(category);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult?.StatusCode.Should().Be(200);

            var posts = okResult?.Value as IEnumerable<PostResponseDto>;
            posts.Should().NotBeNull();
            posts?.Count().Should().Be(1);
            posts?.First().Category.Should().Be(category);
        }

        [Fact]
        public async Task GetPost_WithValidId_ReturnsPost()
        {
            // Arrange
            int postId = 1;
            var expectedPost = new PostResponseDto 
            { 
                Id = postId, 
                Title = "Test Post", 
                Category = "Tech" 
            };

            _mockPostService.Setup(s => s.GetPostByIdAsync(postId))
                .ReturnsAsync(expectedPost);

            // Act
            var result = await _controller.GetPost(postId);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult?.StatusCode.Should().Be(200);

            var post = okResult?.Value as PostResponseDto;
            post.Should().NotBeNull();
            post?.Id.Should().Be(postId);
        }

        [Fact]
        public async Task GetPost_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            int postId = 999;
            _mockPostService.Setup(s => s.GetPostByIdAsync(postId))
                .ReturnsAsync((PostResponseDto?)null);

            // Act
            var result = await _controller.GetPost(postId);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}