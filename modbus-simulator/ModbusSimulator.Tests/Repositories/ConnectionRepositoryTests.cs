using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using ModbusSimulator.Models;
using ModbusSimulator.Repositories;
using Moq;
using Xunit;

namespace ModbusSimulator.Tests.Repositories;

public class ConnectionRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ConnectionRepository _repository;

    public ConnectionRepositoryTests()
    {
        // 创建内存数据库用于测试
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // 初始化数据库表结构
        InitializeDatabase();

        _repository = new ConnectionRepository(_connection);
    }

    private void InitializeDatabase()
    {
        const string createConnectionsTable = @"
            CREATE TABLE connections (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL UNIQUE,
                port INTEGER NOT NULL UNIQUE
            );";

        const string createSlavesTable = @"
            CREATE TABLE slaves (
                id TEXT PRIMARY KEY,
                connid TEXT NOT NULL,
                name TEXT NOT NULL,
                slaveid INTEGER NOT NULL,
                FOREIGN KEY (connid) REFERENCES connections(id) ON DELETE CASCADE,
                UNIQUE(connid, slaveid),
                UNIQUE(connid, name)
            );";

        const string createRegistersTable = @"
            CREATE TABLE registers (
                id TEXT PRIMARY KEY,
                slaveid TEXT NOT NULL,
                startaddr INTEGER NOT NULL,
                hexdata TEXT NOT NULL,
                FOREIGN KEY (slaveid) REFERENCES slaves(id) ON DELETE CASCADE,
                UNIQUE(slaveid, startaddr)
            );";

        // 创建索引
        const string createIndices = @"
            CREATE INDEX idx_slaves_conn ON slaves(connid);
            CREATE INDEX idx_registers_slave ON registers(slaveid);
            CREATE INDEX idx_registers_addr ON registers(slaveid, startaddr);";

        using var command = _connection.CreateCommand();
        command.CommandText = createConnectionsTable + createSlavesTable + createRegistersTable + createIndices;
        command.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidConnection_ShouldCreateAndReturnConnection()
    {
        // Arrange
        var connection = new Connection
        {
            Name = "Test Connection",
            Port = 502
        };

        // Act
        var result = await _repository.CreateAsync(connection);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(string.Empty, result.Id);
        Assert.Equal("Test Connection", result.Name);
        Assert.Equal(502, result.Port);

        // 验证数据库中确实创建了记录
        var count = await _connection.QuerySingleAsync<int>(
            "SELECT COUNT(1) FROM connections WHERE name = @Name", new { Name = "Test Connection" });
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CreateAsync_FirstConnection_ShouldAssignPort502()
    {
        // Arrange
        var connection = new Connection
        {
            Name = "First Connection"
        };

        // Act
        var result = await _repository.CreateAsync(connection);

        // Assert
        Assert.Equal(502, result.Port);
    }

    [Fact]
    public async Task CreateAsync_SecondConnection_ShouldIncrementPort()
    {
        // Arrange
        await _repository.CreateAsync(new Connection { Name = "First", Port = 502 });
        var secondConnection = new Connection { Name = "Second" };

        // Act
        var result = await _repository.CreateAsync(secondConnection);

        // Assert
        Assert.Equal(503, result.Port);
    }

    [Fact]
    public async Task CreateAsync_ConnectionWithExplicitPort_ShouldUseProvidedPort()
    {
        // Arrange
        var connection = new Connection
        {
            Name = "Custom Port Connection",
            Port = 1502
        };

        // Act
        var result = await _repository.CreateAsync(connection);

        // Assert
        Assert.Equal(1502, result.Port);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingConnection_ShouldUpdateSuccessfully()
    {
        // Arrange
        var created = await _repository.CreateAsync(new Connection { Name = "Original", Port = 502 });
        var updatedConnection = new Connection
        {
            Id = created.Id,
            Name = "Updated Name",
            Port = 1502
        };

        // Act
        var result = await _repository.UpdateAsync(updatedConnection);

        // Assert
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal(1502, result.Port);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentConnection_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var connection = new Connection
        {
            Id = "non-existent-id",
            Name = "Test",
            Port = 502
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _repository.UpdateAsync(connection));
        Assert.Contains("连接不存在", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_PortConflict_ShouldThrowInvalidOperationException()
    {
        // Arrange
        await _repository.CreateAsync(new Connection { Name = "Connection 1", Port = 502 });
        var connection2 = await _repository.CreateAsync(new Connection { Name = "Connection 2", Port = 503 });

        var updatedConnection = new Connection
        {
            Id = connection2.Id,
            Name = "Connection 2 Updated",
            Port = 502  // 冲突端口
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _repository.UpdateAsync(updatedConnection));
        Assert.Contains("端口已被使用", exception.Message);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingConnection_ShouldDeleteSuccessfully()
    {
        // Arrange
        var created = await _repository.CreateAsync(new Connection { Name = "To Delete", Port = 502 });

        // Act
        await _repository.DeleteAsync(created.Id);

        // Assert
        var count = await _connection.QuerySingleAsync<int>(
            "SELECT COUNT(1) FROM connections WHERE id = @Id", new { Id = created.Id });
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentConnection_ShouldThrowKeyNotFoundException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _repository.DeleteAsync("non-existent-id"));
        Assert.Contains("资源不存在", exception.Message);
    }

    #endregion

    #region GetConnectionsTreeAsync Tests

    [Fact]
    public async Task GetConnectionsTreeAsync_NoConnections_ShouldReturnEmpty()
    {
        // Act
        var result = await _repository.GetConnectionsTreeAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetConnectionsTreeAsync_SingleConnectionNoSlaves_ShouldReturnConnectionWithEmptySlaves()
    {
        // Arrange
        await _repository.CreateAsync(new Connection { Name = "Test Connection", Port = 502 });

        // Act
        var result = await _repository.GetConnectionsTreeAsync();

        // Assert
        var connectionTree = Assert.Single(result);
        Assert.Equal("Test Connection", connectionTree.Name);
        Assert.Equal(502, connectionTree.Port);
        Assert.Empty(connectionTree.Slaves);
    }

    [Fact]
    public async Task GetConnectionsTreeAsync_ConnectionWithSlaves_ShouldReturnCompleteTree()
    {
        // Arrange - 创建连接
        var connection = await _repository.CreateAsync(new Connection { Name = "Test Connection", Port = 502 });

        // 手动添加从机到数据库（因为还没有SlaveRepository）
        const string insertSlaveSql = @"
            INSERT INTO slaves (id, connid, name, slaveid)
            VALUES (@Id, @ConnId, @Name, @SlaveId)";

        await _connection.ExecuteAsync(insertSlaveSql, new
        {
            Id = Guid.NewGuid().ToString("N"),
            ConnId = connection.Id,
            Name = "Slave 1",
            SlaveId = 1
        });

        await _connection.ExecuteAsync(insertSlaveSql, new
        {
            Id = Guid.NewGuid().ToString("N"),
            ConnId = connection.Id,
            Name = "Slave 2",
            SlaveId = 2
        });

        // Act
        var result = await _repository.GetConnectionsTreeAsync();

        // Assert
        var connectionTree = Assert.Single(result);
        Assert.Equal("Test Connection", connectionTree.Name);
        Assert.Equal(2, connectionTree.Slaves.Count);

        var slave1 = connectionTree.Slaves.First(s => s.Slaveid == 1);
        Assert.Equal("Slave 1", slave1.Name);
        Assert.Equal(connection.Id, slave1.Connid);

        var slave2 = connectionTree.Slaves.First(s => s.Slaveid == 2);
        Assert.Equal("Slave 2", slave2.Name);
        Assert.Equal(connection.Id, slave2.Connid);
    }

    [Fact]
    public async Task GetConnectionsTreeAsync_MultipleConnections_ShouldReturnAllOrderedByName()
    {
        // Arrange
        await _repository.CreateAsync(new Connection { Name = "Z Connection", Port = 502 });
        await _repository.CreateAsync(new Connection { Name = "A Connection", Port = 503 });

        // Act
        var result = await _repository.GetConnectionsTreeAsync();

        // Assert
        Assert.Equal(2, result.Count());
        var orderedResult = result.OrderBy(c => c.Name).ToList();
        Assert.Equal("A Connection", orderedResult[0].Name);
        Assert.Equal("Z Connection", orderedResult[1].Name);
    }

    #endregion
}
