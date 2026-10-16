using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MovieStreaming.Controllers;
using MovieStreaming.Data;
using Xunit;

namespace MovieStreaming.Tests;

public class HealthControllerTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CheckHealth_WhenDatabaseIsHealthy_Returns200WithHealthyStatus()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var loggerMock = new Mock<ILogger<HealthController>>();
        var controller = new HealthController(context, loggerMock.Object);

        // Act
        var result = await controller.CheckHealth(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

        var value = okResult.Value!;
        var status = value.GetType().GetProperty("status")?.GetValue(value)?.ToString();
        var database = value.GetType().GetProperty("database")?.GetValue(value)?.ToString();

        Assert.Equal("Healthy", status);
        Assert.Equal("Connected", database);
    }
}
