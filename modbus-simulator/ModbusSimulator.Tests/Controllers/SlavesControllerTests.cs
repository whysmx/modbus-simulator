using Microsoft.AspNetCore.Mvc;
using ModbusSimulator.Controllers;
using ModbusSimulator.Models;
using ModbusSimulator.Repositories;
using Moq;
using Xunit;

namespace ModbusSimulator.Tests.Controllers;

public class SlavesControllerTests
{
    private readonly Mock<ISlaveRepository> _mockSlaveRepository;
    private readonly SlavesController _controller;

    public SlavesControllerTests()
    {
        _mockSlaveRepository = new Mock<ISlaveRepository>();
        _controller = new SlavesController(_mockSlaveRepository.Object);
    }

    #region CreateSlave Tests

    [Fact]
    public async Task CreateSlave_ValidRequest_ShouldReturnCreatedResult()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var request = new CreateSlaveRequest { Name = "Test Slave", Slaveid = 1 };
        var expectedSlave = new Slave
        {
            Id = "generated-slave-id",
            Connid = connectionId,
            Name = "Test Slave",
            Slaveid = 1
        };

        _mockSlaveRepository.Setup(r => r.CreateAsync(It.IsAny<Slave>())).ReturnsAsync(expectedSlave);

        // Act
        var result = await _controller.CreateSlave(connectionId, request);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal(expectedSlave, createdResult.Value);

        _mockSlaveRepository.Verify(r => r.CreateAsync(It.Is<Slave>(s =>
            s.Connid == connectionId && s.Name == "Test Slave" && s.Slaveid == 1)), Times.Once);
    }

    [Fact]
    public async Task CreateSlave_KeyNotFoundException_ShouldReturnNotFound()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var request = new CreateSlaveRequest { Name = "Test Slave", Slaveid = 1 };

        _mockSlaveRepository.Setup(r => r.CreateAsync(It.IsAny<Slave>()))
            .ThrowsAsync(new KeyNotFoundException("Connection not found"));

        // Act
        var result = await _controller.CreateSlave(connectionId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(404, notFoundResult.StatusCode);
        var error = notFoundResult.Value as dynamic;
        Assert.Equal("Connection not found", error.error);
        Assert.Equal(404, error.code);
    }

    [Fact]
    public async Task CreateSlave_InvalidOperationException_ShouldReturnBadRequest()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var request = new CreateSlaveRequest { Name = "Test Slave", Slaveid = 1 };

        _mockSlaveRepository.Setup(r => r.CreateAsync(It.IsAny<Slave>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate slave ID"));

        // Act
        var result = await _controller.CreateSlave(connectionId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
        var error = badRequestResult.Value as dynamic;
        Assert.Equal("Duplicate slave ID", error.error);
        Assert.Equal(400, error.code);
    }

    [Fact]
    public async Task CreateSlave_GenericException_ShouldReturnBadRequest()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var request = new CreateSlaveRequest { Name = "Test Slave", Slaveid = 1 };

        _mockSlaveRepository.Setup(r => r.CreateAsync(It.IsAny<Slave>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.CreateSlave(connectionId, request);

        // Assert - Generic exceptions should also be handled
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
        var error = badRequestResult.Value as dynamic;
        Assert.Equal("Unexpected error", error.error);
        Assert.Equal(400, error.code);
    }

    #endregion

    #region UpdateSlave Tests

    [Fact]
    public async Task UpdateSlave_ValidRequest_ShouldReturnOkResult()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";
        var request = new UpdateSlaveRequest { Name = "Updated Slave", Slaveid = 2 };
        var expectedSlave = new Slave
        {
            Id = slaveId,
            Connid = connectionId,
            Name = "Updated Slave",
            Slaveid = 2
        };

        _mockSlaveRepository.Setup(r => r.UpdateAsync(It.IsAny<Slave>())).ReturnsAsync(expectedSlave);

        // Act
        var result = await _controller.UpdateSlave(connectionId, slaveId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(expectedSlave, okResult.Value);

        _mockSlaveRepository.Verify(r => r.UpdateAsync(It.Is<Slave>(s =>
            s.Id == slaveId && s.Connid == connectionId && s.Name == "Updated Slave" && s.Slaveid == 2)), Times.Once);
    }

    [Fact]
    public async Task UpdateSlave_KeyNotFoundException_ShouldReturnNotFound()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";
        var request = new UpdateSlaveRequest { Name = "Updated Slave", Slaveid = 2 };

        _mockSlaveRepository.Setup(r => r.UpdateAsync(It.IsAny<Slave>()))
            .ThrowsAsync(new KeyNotFoundException("Slave not found"));

        // Act
        var result = await _controller.UpdateSlave(connectionId, slaveId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(404, notFoundResult.StatusCode);
        var error = notFoundResult.Value as dynamic;
        Assert.Equal("Slave not found", error.error);
        Assert.Equal(404, error.code);
    }

    [Fact]
    public async Task UpdateSlave_InvalidOperationException_ShouldReturnBadRequest()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";
        var request = new UpdateSlaveRequest { Name = "Updated Slave", Slaveid = 2 };

        _mockSlaveRepository.Setup(r => r.UpdateAsync(It.IsAny<Slave>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate slave ID"));

        // Act
        var result = await _controller.UpdateSlave(connectionId, slaveId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
        var error = badRequestResult.Value as dynamic;
        Assert.Equal("Duplicate slave ID", error.error);
        Assert.Equal(400, error.code);
    }

    #endregion

    #region DeleteSlave Tests

    [Fact]
    public async Task DeleteSlave_ValidIds_ShouldReturnNoContent()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";

        _mockSlaveRepository.Setup(r => r.DeleteAsync(slaveId)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteSlave(connectionId, slaveId);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockSlaveRepository.Verify(r => r.DeleteAsync(slaveId), Times.Once);
    }

    [Fact]
    public async Task DeleteSlave_KeyNotFoundException_ShouldReturnNotFound()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";

        _mockSlaveRepository.Setup(r => r.DeleteAsync(slaveId))
            .ThrowsAsync(new KeyNotFoundException("Slave not found"));

        // Act
        var result = await _controller.DeleteSlave(connectionId, slaveId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
        var error = notFoundResult.Value as dynamic;
        Assert.Equal("Slave not found", error.error);
        Assert.Equal(404, error.code);
    }

    #endregion

    #region Model Binding Tests

    [Fact]
    public async Task CreateSlave_NullRequest_ShouldHandleGracefully()
    {
        // Arrange
        var connectionId = "test-connection-id";

        // Act & Assert - This would typically be handled by ASP.NET Core model validation
        // but we test the repository call behavior
        _mockSlaveRepository.Setup(r => r.CreateAsync(It.IsAny<Slave>()))
            .ReturnsAsync(new Slave { Id = "test-id", Connid = connectionId, Name = null, Slaveid = 0 });

        var result = await _controller.CreateSlave(connectionId, null);

        // Assert - The controller doesn't validate null requests, it just passes to repository
        var createdResult = Assert.IsType<CreatedResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);
    }

    [Fact]
    public async Task UpdateSlave_NullRequest_ShouldHandleGracefully()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";

        _mockSlaveRepository.Setup(r => r.UpdateAsync(It.IsAny<Slave>()))
            .ReturnsAsync(new Slave { Id = slaveId, Connid = connectionId, Name = null, Slaveid = 0 });

        // Act
        var result = await _controller.UpdateSlave(connectionId, slaveId, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);
    }

    #endregion

    #region Route Parameter Tests

    [Theory]
    [InlineData("valid-connection-id", "valid-slave-id")]
    [InlineData("123", "456")]
    [InlineData("connection_with_underscores", "slave_with_underscores")]
    public async Task CreateSlave_VariousRouteParameters_ShouldPassThrough(string connectionId, string slaveId)
    {
        // Arrange
        var request = new CreateSlaveRequest { Name = "Test Slave", Slaveid = 1 };
        var expectedSlave = new Slave
        {
            Id = "generated-id",
            Connid = connectionId,
            Name = "Test Slave",
            Slaveid = 1
        };

        _mockSlaveRepository.Setup(r => r.CreateAsync(It.Is<Slave>(s => s.Connid == connectionId)))
            .ReturnsAsync(expectedSlave);

        // Act
        var result = await _controller.CreateSlave(connectionId, request);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result.Result);
        var returnedSlave = createdResult.Value as Slave;
        Assert.Equal(connectionId, returnedSlave.Connid);
    }

    #endregion

    #region HTTP Status Code Tests

    [Fact]
    public async Task CreateSlave_ShouldReturnCorrectHttpStatusCodes()
    {
        // Test Created (201)
        var request = new CreateSlaveRequest { Name = "Test Slave", Slaveid = 1 };
        var slave = new Slave { Id = "id", Connid = "conn", Name = "Test", Slaveid = 1 };
        _mockSlaveRepository.Setup(r => r.CreateAsync(It.IsAny<Slave>())).ReturnsAsync(slave);

        var result = await _controller.CreateSlave("conn", request);
        var createdResult = Assert.IsType<CreatedResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);

        // Test NotFound (404)
        _mockSlaveRepository.Setup(r => r.CreateAsync(It.IsAny<Slave>()))
            .ThrowsAsync(new KeyNotFoundException("Not found"));
        var notFoundResult = await _controller.CreateSlave("conn", request);
        Assert.Equal(404, ((NotFoundObjectResult)notFoundResult.Result).StatusCode);

        // Test BadRequest (400)
        _mockSlaveRepository.Setup(r => r.CreateAsync(It.IsAny<Slave>()))
            .ThrowsAsync(new InvalidOperationException("Bad request"));
        var badRequestResult = await _controller.CreateSlave("conn", request);
        Assert.Equal(400, ((BadRequestObjectResult)badRequestResult.Result).StatusCode);
    }

    [Fact]
    public async Task UpdateSlave_ShouldReturnCorrectHttpStatusCodes()
    {
        // Test OK (200)
        var request = new UpdateSlaveRequest { Name = "Updated", Slaveid = 2 };
        var slave = new Slave { Id = "id", Connid = "conn", Name = "Updated", Slaveid = 2 };
        _mockSlaveRepository.Setup(r => r.UpdateAsync(It.IsAny<Slave>())).ReturnsAsync(slave);

        var result = await _controller.UpdateSlave("conn", "slave", request);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);

        // Test NotFound (404)
        _mockSlaveRepository.Setup(r => r.UpdateAsync(It.IsAny<Slave>()))
            .ThrowsAsync(new KeyNotFoundException("Not found"));
        var notFoundResult = await _controller.UpdateSlave("conn", "slave", request);
        Assert.Equal(404, ((NotFoundObjectResult)notFoundResult.Result).StatusCode);
    }

    [Fact]
    public async Task DeleteSlave_ShouldReturnCorrectHttpStatusCodes()
    {
        // Test NoContent (204)
        _mockSlaveRepository.Setup(r => r.DeleteAsync("slave")).Returns(Task.CompletedTask);
        var result = await _controller.DeleteSlave("conn", "slave");
        Assert.IsType<NoContentResult>(result);

        // Test NotFound (404)
        _mockSlaveRepository.Setup(r => r.DeleteAsync("slave"))
            .ThrowsAsync(new KeyNotFoundException("Not found"));
        var notFoundResult = await _controller.DeleteSlave("conn", "slave");
        Assert.Equal(404, ((NotFoundObjectResult)notFoundResult).StatusCode);
    }

    #endregion
}
