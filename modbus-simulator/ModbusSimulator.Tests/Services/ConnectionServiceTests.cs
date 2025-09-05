using System.Data;
using Microsoft.Data.Sqlite;
using ModbusSimulator.Models;
using ModbusSimulator.Repositories;
using ModbusSimulator.Services;
using Moq;
using Xunit;

namespace ModbusSimulator.Tests.Services;

public class ConnectionServiceTests
{
    private readonly Mock<IConnectionRepository> _mockRepository;
    private readonly ConnectionService _service;

    public ConnectionServiceTests()
    {
        _mockRepository = new Mock<IConnectionRepository>();
        _service = new ConnectionService(_mockRepository.Object);
    }

    #region GetConnectionsTreeAsync Tests

    [Fact]
    public async Task GetConnectionsTreeAsync_ShouldReturnRepositoryResult()
    {
        // Arrange
        var expectedTrees = new List<ConnectionTree>
        {
            new ConnectionTree { Id = "test-id", Name = "Test Connection", Port = 502 }
        };
        _mockRepository.Setup(r => r.GetConnectionsTreeAsync())
            .ReturnsAsync(expectedTrees);

        // Act
        var result = await _service.GetConnectionsTreeAsync();

        // Assert
        Assert.Equal(expectedTrees, result);
        _mockRepository.Verify(r => r.GetConnectionsTreeAsync(), Times.Once);
    }

    #endregion

    #region CreateConnectionAsync Tests

    [Fact]
    public async Task CreateConnectionAsync_ValidRequest_ShouldCreateSuccessfully()
    {
        // Arrange
        var request = new CreateConnectionRequest { Name = "Test Connection" };
        var expectedConnection = new Connection
        {
            Id = "generated-id",
            Name = "Test Connection",
            Port = 502
        };

        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<Connection>()))
            .ReturnsAsync(expectedConnection);

        // Act
        var result = await _service.CreateConnectionAsync(request);

        // Assert
        Assert.Equal(expectedConnection, result);
        _mockRepository.Verify(r => r.CreateAsync(It.Is<Connection>(c =>
            c.Name == "Test Connection" && c.Port == 0)), Times.Once);
    }

    [Fact]
    public async Task CreateConnectionAsync_EmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new CreateConnectionRequest { Name = "" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateConnectionAsync(request));
        Assert.Contains("连接名称不能为空", exception.Message);
        Assert.Equal("request.Name", exception.ParamName);
    }

    [Fact]
    public async Task CreateConnectionAsync_WhitespaceName_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new CreateConnectionRequest { Name = "   " };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateConnectionAsync(request));
        Assert.Contains("连接名称不能为空", exception.Message);
    }

    [Fact]
    public async Task CreateConnectionAsync_NameTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new CreateConnectionRequest { Name = new string('A', 101) };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateConnectionAsync(request));
        Assert.Contains("连接名称长度不能超过100个字符", exception.Message);
    }

    [Fact]
    public async Task CreateConnectionAsync_NameWithLeadingTrailingSpaces_ShouldTrimName()
    {
        // Arrange
        var request = new CreateConnectionRequest { Name = "  Test Connection  " };
        var expectedConnection = new Connection
        {
            Id = "generated-id",
            Name = "Test Connection",
            Port = 502
        };

        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<Connection>()))
            .ReturnsAsync(expectedConnection);

        // Act
        await _service.CreateConnectionAsync(request);

        // Assert
        _mockRepository.Verify(r => r.CreateAsync(It.Is<Connection>(c =>
            c.Name == "Test Connection")), Times.Once);
    }

    [Fact]
    public async Task CreateConnectionAsync_DuplicateName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = new CreateConnectionRequest { Name = "Duplicate Name" };

        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<Connection>()))
            .ThrowsAsync(new Microsoft.Data.Sqlite.SqliteException("UNIQUE constraint failed: connections.name", 0));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateConnectionAsync(request));
        Assert.Contains("连接名称已存在", exception.Message);
    }

    #endregion

    #region UpdateConnectionAsync Tests

    [Fact]
    public async Task UpdateConnectionAsync_ValidRequest_ShouldUpdateSuccessfully()
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

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Connection>()))
            .ReturnsAsync(expectedConnection);

        // Act
        var result = await _service.UpdateConnectionAsync(id, request);

        // Assert
        Assert.Equal(expectedConnection, result);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<Connection>(c =>
            c.Id == id && c.Name == "Updated Connection" && c.Port == 1502)), Times.Once);
    }

    [Fact]
    public async Task UpdateConnectionAsync_EmptyId_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new UpdateConnectionRequest { Name = "Test", Port = 502 };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateConnectionAsync("", request));
        Assert.Contains("连接ID不能为空", exception.Message);
        Assert.Equal("id", exception.ParamName);
    }

    [Fact]
    public async Task UpdateConnectionAsync_EmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new UpdateConnectionRequest { Name = "", Port = 502 };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateConnectionAsync("test-id", request));
        Assert.Contains("连接名称不能为空", exception.Message);
    }

    [Fact]
    public async Task UpdateConnectionAsync_InvalidPort_ShouldThrowArgumentException()
    {
        // Test cases for invalid ports
        var invalidPorts = new[] { 0, -1, 65536 };

        foreach (var port in invalidPorts)
        {
            var request = new UpdateConnectionRequest { Name = "Test", Port = port };

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateConnectionAsync("test-id", request));
            Assert.Contains("端口号必须在1-65535之间", exception.Message);
        }
    }

    [Fact]
    public async Task UpdateConnectionAsync_NameTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new UpdateConnectionRequest { Name = new string('A', 101), Port = 502 };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateConnectionAsync("test-id", request));
        Assert.Contains("连接名称长度不能超过100个字符", exception.Message);
    }

    [Fact]
    public async Task UpdateConnectionAsync_NameTrimming_ShouldTrimWhitespace()
    {
        // Arrange
        var request = new UpdateConnectionRequest { Name = "  Updated Name  ", Port = 502 };

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Connection>()))
            .ReturnsAsync((Connection c) => c);

        // Act
        await _service.UpdateConnectionAsync("test-id", request);

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<Connection>(c =>
            c.Name == "Updated Name")), Times.Once);
    }

    [Fact]
    public async Task UpdateConnectionAsync_DuplicateName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = new UpdateConnectionRequest { Name = "Duplicate Name", Port = 502 };

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Connection>()))
            .ThrowsAsync(new Microsoft.Data.Sqlite.SqliteException("UNIQUE constraint failed: connections.name", 0));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateConnectionAsync("test-id", request));
        Assert.Contains("连接名称已存在", exception.Message);
    }

    [Fact]
    public async Task UpdateConnectionAsync_PortConflict_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = new UpdateConnectionRequest { Name = "Test", Port = 502 };

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Connection>()))
            .ThrowsAsync(new Microsoft.Data.Sqlite.SqliteException("UNIQUE constraint failed: connections.port", 0));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateConnectionAsync("test-id", request));
        Assert.Contains("端口已被使用", exception.Message);
    }

    #endregion

    #region DeleteConnectionAsync Tests

    [Fact]
    public async Task DeleteConnectionAsync_ValidId_ShouldDeleteSuccessfully()
    {
        // Arrange
        var id = "test-id";
        _mockRepository.Setup(r => r.DeleteAsync(id)).Returns(Task.CompletedTask);

        // Act
        await _service.DeleteConnectionAsync(id);

        // Assert
        _mockRepository.Verify(r => r.DeleteAsync(id), Times.Once);
    }

    [Fact]
    public async Task DeleteConnectionAsync_EmptyId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.DeleteConnectionAsync(""));
        Assert.Contains("连接ID不能为空", exception.Message);
        Assert.Equal("id", exception.ParamName);
    }

    [Fact]
    public async Task DeleteConnectionAsync_NonExistentId_ShouldPropagateRepositoryException()
    {
        // Arrange
        _mockRepository.Setup(r => r.DeleteAsync("missing"))
            .ThrowsAsync(new KeyNotFoundException("资源不存在"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.DeleteConnectionAsync("missing"));
    }

    #endregion
}
