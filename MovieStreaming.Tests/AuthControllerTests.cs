using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieStreaming.Controllers;
using MovieStreaming.DTOs.Auth;
using MovieStreaming.Services.Authentication;

namespace MovieStreaming.Tests;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _controller = new AuthController(_mockAuthService.Object);
    }

    [Fact]
    public async Task Register_ValidRequest_Returns201CreatedWithUserResponse()
    {
        // Arrange
        var request = new RegisterRequest { Username = "newuser", Email = "new@example.com", Password = "Password123" };
        var expectedResponse = new UserResponse { Id = 1, Username = "newuser", Email = "new@example.com", Role = "User" };

        _mockAuthService.Setup(s => s.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Register(request, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
        Assert.Equal(expectedResponse, objectResult.Value);
    }

    [Fact]
    public async Task Login_ValidRequest_Returns200OkWithLoginResponse()
    {
        // Arrange
        var request = new LoginRequest { UsernameOrEmail = "user", Password = "Password123" };
        var expectedResponse = new LoginResponse
        {
            AccessToken = "valid-jwt-token",
            RefreshToken = "valid-refresh-token",
            ExpiresIn = 3600,
            User = new UserResponse { Id = 1, Username = "user", Email = "user@example.com", Role = "User" }
        };

        _mockAuthService.Setup(s => s.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Equal(expectedResponse, okResult.Value);
    }

    [Fact]
    public async Task RefreshToken_ValidRequest_Returns200OkWithRefreshTokenResponse()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "old-token" };
        var expectedResponse = new RefreshTokenResponse
        {
            AccessToken = "new-jwt-token",
            RefreshToken = "new-refresh-token",
            ExpiresIn = 3600,
            User = new UserResponse { Id = 1, Username = "user", Email = "user@example.com", Role = "User" }
        };

        _mockAuthService.Setup(s => s.RefreshTokenAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.RefreshToken(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Equal(expectedResponse, okResult.Value);
    }

    [Fact]
    public async Task Logout_ValidRequest_CallsServiceAndReturns200Ok()
    {
        // Arrange
        var request = new LogoutRequest { RefreshToken = "token-to-revoke" };

        // Act
        var result = await _controller.Logout(request, CancellationToken.None);

        // Assert
        _mockAuthService.Verify(s => s.LogoutAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public void TestAuth_AuthenticatedUser_ReturnsOkWithUsername()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Role, "User")
        }, "TestAuthType"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Act
        var result = _controller.TestAuth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public void TestAdminAuth_AdminUser_ReturnsOk()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "adminuser"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "TestAuthType"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Act
        var result = _controller.TestAdminAuth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }
}
