using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieStreaming.Controllers;
using MovieStreaming.DTOs.Auth;
using MovieStreaming.Exceptions;
using MovieStreaming.Services.Authentication;

namespace MovieStreaming.Tests;

public class UsersControllerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _controller = new UsersController(_mockAuthService.Object);
    }

    [Fact]
    public async Task GetCurrentUser_ValidClaims_ReturnsUserResponse()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "15"),
            new Claim(ClaimTypes.Name, "currentuser"),
            new Claim(ClaimTypes.Role, "User")
        }, "TestAuthType"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var expectedResponse = new UserResponse
        {
            Id = 15,
            Username = "currentuser",
            Email = "current@example.com",
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        _mockAuthService.Setup(s => s.GetCurrentUserAsync(15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetCurrentUser(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Equal(expectedResponse, okResult.Value);
    }

    [Fact]
    public async Task GetCurrentUser_MissingIdentityClaim_ThrowsUnauthorizedException()
    {
        // Arrange: ClaimsPrincipal without NameIdentifier or Sub
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "User")
        }, "TestAuthType"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _controller.GetCurrentUser(CancellationToken.None));
    }

    [Fact]
    public async Task ChangePassword_ValidClaimsAndRequest_Returns200Ok()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "15"),
            new Claim(ClaimTypes.Role, "User")
        }, "TestAuthType"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword456!"
        };

        // Act
        var result = await _controller.ChangePassword(request, CancellationToken.None);

        // Assert
        _mockAuthService.Verify(s => s.ChangePasswordAsync(15, request, It.IsAny<CancellationToken>()), Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }
}
