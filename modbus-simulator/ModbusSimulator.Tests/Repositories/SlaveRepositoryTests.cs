using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using ModbusSimulator.Models;
using ModbusSimulator.Repositories;
using Xunit;

namespace ModbusSimulator.Tests.Repositories;

public class SlaveRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SlaveRepository _repository;

    public SlaveRepositoryTests()
    {
        // 创建内存数据库用于测试
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // 初始化数据库表结构
        InitializeDatabase();

        _repository = new SlaveRepository(_connection);
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
    public async Task CreateAsync_ValidSlave_ShouldCreateAndReturnSlave()
    {
        // Arrange
        var connectionId = Guid.NewGuid().ToString("N");
        await CreateTestConnection(connectionId, "Test Connection", 502);

        var slave = new Slave
        {
            Connid = connectionId,
            Name = "Test Slave",
            Slaveid = 1
        };

        // Act
        var result = await _repository.CreateAsync(slave);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(string.Empty, result.Id);
        Assert.Equal(connectionId, result.Connid);
        Assert.Equal("Test Slave", result.Name);
        Assert.Equal(1, result.Slaveid);

        // 验证数据库中确实创建了记录
        var count = await _connection.QuerySingleAsync<int>(
            "SELECT COUNT(1) FROM slaves WHERE name = @Name", new { Name = "Test Slave" });
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CreateAsync_DuplicateSlaveIdInSameConnection_ShouldThrowException()
    {
        // Arrange
        var connectionId = Guid.NewGuid().ToString("N");
        await CreateTestConnection(connectionId, "Test Connection", 502);

        var slave1 = new Slave
        {
            Connid = connectionId,
            Name = "Slave 1",
            Slaveid = 1
        };
        await _repository.CreateAsync(slave1);

        var slave2 = new Slave
        {
            Connid = connectionId,
            Name = "Slave 2",
            Slaveid = 1  // 重复的从机地址
        };

        // Act & Assert
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(
            () => _repository.CreateAsync(slave2));
    }

    [Fact]
    public async Task CreateAsync_DuplicateNameInSameConnection_ShouldThrowException()
    {
        // Arrange
        var connectionId = Guid.NewGuid().ToString("N");
        await CreateTestConnection(connectionId, "Test Connection", 502);

        var slave1 = new Slave
        {
            Connid = connectionId,
            Name = "Test Slave",
            Slaveid = 1
        };
        await _repository.CreateAsync(slave1);

        var slave2 = new Slave
        {
            Connid = connectionId,
            Name = "Test Slave",  // 重复的从机名称
            Slaveid = 2
        };

        // Act & Assert
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(
            () => _repository.CreateAsync(slave2));
    }

    [Fact]
    public async Task CreateAsync_SameSlaveIdInDifferentConnections_ShouldSucceed()
    {
        // Arrange
        var connectionId1 = Guid.NewGuid().ToString("N");
        var connectionId2 = Guid.NewGuid().ToString("N");
        await CreateTestConnection(connectionId1, "Connection 1", 502);
        await CreateTestConnection(connectionId2, "Connection 2", 503);

        var slave1 = new Slave
        {
            Connid = connectionId1,
            Name = "Slave 1",
            Slaveid = 1
        };
        var slave2 = new Slave
        {
            Connid = connectionId2,
            Name = "Slave 1",  // 相同名称但不同连接
            Slaveid = 1        // 相同地址但不同连接
        };

        // Act
        await _repository.CreateAsync(slave1);
        await _repository.CreateAsync(slave2);

        // Assert
        var count = await _connection.QuerySingleAsync<int>(
            "SELECT COUNT(1) FROM slaves WHERE slaveid = 1");
        Assert.Equal(2, count);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingSlave_ShouldUpdateSuccessfully()
    {
        // Arrange
        var connectionId = Guid.NewGuid().ToString("N");
        await CreateTestConnection(connectionId, "Test Connection", 502);

        var created = await _repository.CreateAsync(new Slave
        {
            Connid = connectionId,
            Name = "Original Slave",
            Slaveid = 1
        });

        var updatedSlave = new Slave
        {
            Id = created.Id,
            Connid = connectionId,
            Name = "Updated Slave",
            Slaveid = 2
        };

        // Act
        var result = await _repository.UpdateAsync(updatedSlave);

        // Assert
        Assert.Equal(created.Id, result.Id);
        Assert.Equal(connectionId, result.Connid);
        Assert.Equal("Updated Slave", result.Name);
        Assert.Equal(2, result.Slaveid);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentSlave_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var slave = new Slave
        {
            Id = "non-existent-id",
            Connid = Guid.NewGuid().ToString("N"),
            Name = "Test",
            Slaveid = 1
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _repository.UpdateAsync(slave));
        Assert.Contains("从机不存在", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_ChangeToDuplicateSlaveId_ShouldThrowException()
    {
        // Arrange
        var connectionId = Guid.NewGuid().ToString("N");
        await CreateTestConnection(connectionId, "Test Connection", 502);

        // 创建两个从机
        var slave1 = await _repository.CreateAsync(new Slave
        {
            Connid = connectionId,
            Name = "Slave 1",
            Slaveid = 1
        });
        await _repository.CreateAsync(new Slave
        {
            Connid = connectionId,
            Name = "Slave 2",
            Slaveid = 2
        });

        // 尝试将slave1的地址改为与slave2相同的地址
        var updatedSlave = new Slave
        {
            Id = slave1.Id,
            Connid = connectionId,
            Name = "Slave 1 Updated",
            Slaveid = 2  // 冲突地址
        };

        // Act & Assert
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(
            () => _repository.UpdateAsync(updatedSlave));
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingSlave_ShouldDeleteSuccessfully()
    {
        // Arrange
        var connectionId = Guid.NewGuid().ToString("N");
        await CreateTestConnection(connectionId, "Test Connection", 502);

        var created = await _repository.CreateAsync(new Slave
        {
            Connid = connectionId,
            Name = "To Delete",
            Slaveid = 1
        });

        // Act
        await _repository.DeleteAsync(created.Id);

        // Assert
        var count = await _connection.QuerySingleAsync<int>(
            "SELECT COUNT(1) FROM slaves WHERE id = @Id", new { Id = created.Id });
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentSlave_ShouldThrowKeyNotFoundException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _repository.DeleteAsync("non-existent-id"));
        Assert.Contains("资源不存在", exception.Message);
    }

    #endregion

    #region Helper Methods

    private async Task CreateTestConnection(string id, string name, int port)
    {
        const string sql = @"
            INSERT INTO connections (id, name, port)
            VALUES (@Id, @Name, @Port)";

        await _connection.ExecuteAsync(sql, new { Id = id, Name = name, Port = port });
    }

    #endregion
}
