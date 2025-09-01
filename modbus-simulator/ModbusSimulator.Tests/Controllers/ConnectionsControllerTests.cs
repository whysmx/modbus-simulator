using Microsoft.AspNetCore.Mvc;
using ModbusSimulator.Controllers;
using ModbusSimulator.Models;
using ModbusSimulator.Services;
using Moq;
using Xunit;

namespace ModbusSimulator.Tests.Controllers;

public class ConnectionsControllerTests
{
    private readonly Mock<IConnectionService> _mockService;
    private readonly ConnectionsController _controller;

    public ConnectionsControllerTests()
    {
        _mockService = new Mock<IConnectionService>();
        _controller = new ConnectionsController(_mockService.Object);
    }

    #region CreateConnection Tests

    [Fact]
    public async Task CreateConnection_ValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreateConnectionRequest { Name = "Test Connection" };
        var expectedConnection = new Connection
        {
            Id = "generated-id",
            Name = "Test Connection",
            Port = 502
        };

        _mockService.Setup(s => s.CreateConnectionAsync(request))
            .ReturnsAsync(expectedConnection);

        // Act
        var result = await _controller.CreateConnection(request);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal(expectedConnection, createdResult.Value);
        _mockService.Verify(s => s.CreateConnectionAsync(request), Times.Once);
    }

    [Fact]
    public async Task CreateConnection_ArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateConnectionRequest { Name = "" };

        _mockService.Setup(s => s.CreateConnectionAsync(request))
            .ThrowsAsync(new ArgumentException("连接名称不能为空"));

        // Act
        var result = await _controller.CreateConnection(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);

        var errorResponse = badRequestResult.Value;
        var errorProperty = errorResponse.GetType().GetProperty("error");
        var codeProperty = errorResponse.GetType().GetProperty("code");

        Assert.Equal("连接名称不能为空", errorProperty.GetValue(errorResponse));
        Assert.Equal(400, codeProperty.GetValue(errorResponse));
    }

    [Fact]
    public async Task CreateConnection_InvalidOperationException_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateConnectionRequest { Name = "Duplicate Name" };

        _mockService.Setup(s => s.CreateConnectionAsync(request))
            .ThrowsAsync(new InvalidOperationException("连接名称已存在"));

        // Act
        var result = await _controller.CreateConnection(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);

        var errorResponse = badRequestResult.Value;
        var errorProperty = errorResponse.GetType().GetProperty("error");
        var codeProperty = errorResponse.GetType().GetProperty("code");

        Assert.Equal("连接名称已存在", errorProperty.GetValue(errorResponse));
        Assert.Equal(400, codeProperty.GetValue(errorResponse));
    }

    #endregion

    #region UpdateConnection Tests

    [Fact]
    public async Task UpdateConnection_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var id = "test-id";
        var request = new UpdateConnectionRequest { Name = "Updated Connection", Port = 1502 };
        var expectedConnection = new Connection
        {
            Id = id,
            Name = "Updated Connection",
            Port = 1502
        };

        _mockService.Setup(s => s.UpdateConnectionAsync(id, request))
            .ReturnsAsync(expectedConnection);

        // Act
        var result = await _controller.UpdateConnection(id, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(expectedConnection, okResult.Value);
        _mockService.Verify(s => s.UpdateConnectionAsync(id, request), Times.Once);
    }

    [Fact]
    public async Task UpdateConnection_KeyNotFoundException_ReturnsNotFound()
    {
        // Arrange
        var id = "non-existent-id";
        var request = new UpdateConnectionRequest { Name = "Test", Port = 502 };

        _mockService.Setup(s => s.UpdateConnectionAsync(id, request))
            .ThrowsAsync(new KeyNotFoundException("连接不存在"));

        // Act
        var result = await _controller.UpdateConnection(id, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(404, notFoundResult.StatusCode);

        var errorResponse = notFoundResult.Value;
        var errorProperty = errorResponse.GetType().GetProperty("error");
        var codeProperty = errorResponse.GetType().GetProperty("code");

        Assert.Equal("连接不存在", errorProperty.GetValue(errorResponse));
        Assert.Equal(404, codeProperty.GetValue(errorResponse));
    }

    [Fact]
    public async Task UpdateConnection_InvalidOperationException_ReturnsBadRequest()
    {
        // Arrange
        var id = "test-id";
        var request = new UpdateConnectionRequest { Name = "Test", Port = 502 };

        _mockService.Setup(s => s.UpdateConnectionAsync(id, request))
            .ThrowsAsync(new InvalidOperationException("端口已被使用"));

        // Act
        var result = await _controller.UpdateConnection(id, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);

        var errorResponse = badRequestResult.Value;
        var errorProperty = errorResponse.GetType().GetProperty("error");
        var codeProperty = errorResponse.GetType().GetProperty("code");

        Assert.Equal("端口已被使用", errorProperty.GetValue(errorResponse));
        Assert.Equal(400, codeProperty.GetValue(errorResponse));
    }

    [Fact]
    public async Task UpdateConnection_ArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var id = "test-id";
        var request = new UpdateConnectionRequest { Name = "", Port = 502 };

        _mockService.Setup(s => s.UpdateConnectionAsync(id, request))
            .ThrowsAsync(new ArgumentException("连接名称不能为空"));

        // Act
        var result = await _controller.UpdateConnection(id, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);

        var errorResponse = badRequestResult.Value;
        var errorProperty = errorResponse.GetType().GetProperty("error");
        var codeProperty = errorResponse.GetType().GetProperty("code");

        Assert.Equal("连接名称不能为空", errorProperty.GetValue(errorResponse));
        Assert.Equal(400, codeProperty.GetValue(errorResponse));
    }

    #endregion

    #region DeleteConnection Tests

    [Fact]
    public async Task DeleteConnection_ValidId_ReturnsNoContent()
    {
        // Arrange
        var id = "test-id";
        _mockService.Setup(s => s.DeleteConnectionAsync(id)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteConnection(id);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockService.Verify(s => s.DeleteConnectionAsync(id), Times.Once);
    }

    [Fact]
    public async Task DeleteConnection_KeyNotFoundException_ReturnsNotFound()
    {
        // Arrange
        var id = "non-existent-id";

        _mockService.Setup(s => s.DeleteConnectionAsync(id))
            .ThrowsAsync(new KeyNotFoundException("连接不存在"));

        // Act
        var result = await _controller.DeleteConnection(id);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);

        var errorResponse = notFoundResult.Value;
        var errorProperty = errorResponse.GetType().GetProperty("error");
        var codeProperty = errorResponse.GetType().GetProperty("code");

        Assert.Equal("连接不存在", errorProperty.GetValue(errorResponse));
        Assert.Equal(404, codeProperty.GetValue(errorResponse));
    }

    [Fact]
    public async Task DeleteConnection_ArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var id = "";

        _mockService.Setup(s => s.DeleteConnectionAsync(id))
            .ThrowsAsync(new ArgumentException("连接ID不能为空"));

        // Act
        var result = await _controller.DeleteConnection(id);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);

        var errorResponse = badRequestResult.Value;
        var errorProperty = errorResponse.GetType().GetProperty("error");
        var codeProperty = errorResponse.GetType().GetProperty("code");

        Assert.Equal("连接ID不能为空", errorProperty.GetValue(errorResponse));
        Assert.Equal(400, codeProperty.GetValue(errorResponse));
    }

    #endregion

    #region GetConnectionsTree Tests

    [Fact]
    public async Task GetConnectionsTree_ReturnsOkResultWithConnectionTrees()
    {
        // Arrange
        var expectedTrees = new List<ConnectionTree>
        {
            new ConnectionTree { Id = "test-id", Name = "Test Connection", Port = 502, Slaves = new List<Slave>() }
        };

        _mockService.Setup(s => s.GetConnectionsTreeAsync())
            .ReturnsAsync(expectedTrees);

        // Act
        var result = await _controller.GetConnectionsTree();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(expectedTrees, okResult.Value);
        _mockService.Verify(s => s.GetConnectionsTreeAsync(), Times.Once);
    }

    #endregion
}
