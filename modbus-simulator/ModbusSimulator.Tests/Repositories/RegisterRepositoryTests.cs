using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using ModbusSimulator.Models;
using ModbusSimulator.Repositories;
using Xunit;

namespace ModbusSimulator.Tests.Repositories;

public class RegisterRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly RegisterRepository _repository;

    public RegisterRepositoryTests()
    {
        // 创建内存数据库用于测试
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // 启用外键约束
        using var pragmaCommand = _connection.CreateCommand();
        pragmaCommand.CommandText = "PRAGMA foreign_keys = ON;";
        pragmaCommand.ExecuteNonQuery();

        // 初始化数据库表结构
        InitializeDatabase();

        _repository = new RegisterRepository(_connection);
    }

    private void InitializeDatabase()
    {
        const string createConnectionsTable = @"
            CREATE TABLE connections (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL UNIQUE,
                Port INTEGER NOT NULL UNIQUE,
                ProtocolType INTEGER NOT NULL DEFAULT 0
            );";

        const string createSlavesTable = @"
            CREATE TABLE slaves (
                Id TEXT PRIMARY KEY,
                ConnId TEXT NOT NULL,
                Name TEXT NOT NULL,
                SlaveId INTEGER NOT NULL,
                FOREIGN KEY (ConnId) REFERENCES connections(Id) ON DELETE CASCADE,
                UNIQUE(ConnId, SlaveId),
                UNIQUE(ConnId, Name)
            );";

        const string createRegistersTable = @"
            CREATE TABLE registers (
                Id TEXT PRIMARY KEY,
                SlaveId TEXT NOT NULL,
                StartAddr INTEGER NOT NULL,
                HexData TEXT NOT NULL,
                names TEXT NOT NULL DEFAULT '',
                coefficients TEXT NOT NULL DEFAULT '',
                FOREIGN KEY (SlaveId) REFERENCES slaves(Id) ON DELETE CASCADE,
                UNIQUE(SlaveId, StartAddr)
            );";

        // 创建索引
        const string createIndices = @"
            CREATE INDEX idx_slaves_conn ON slaves(ConnId);
            CREATE INDEX idx_registers_slave ON registers(SlaveId);
            CREATE INDEX idx_registers_addr ON registers(SlaveId, StartAddr);";

        using var command = _connection.CreateCommand();
        command.CommandText = createConnectionsTable + createSlavesTable + createRegistersTable + createIndices;
        command.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }

    #region GetBySlaveIdAsync Tests

    [Fact]
    public async Task GetBySlaveIdAsync_NoRegisters_ShouldReturnEmpty()
    {
        // Arrange
        var slaveId = Guid.NewGuid().ToString("N");

        // Act
        var result = await _repository.GetBySlaveIdAsync(slaveId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBySlaveIdAsync_SingleRegister_ShouldReturnRegister()
    {
        // Arrange
        var slaveId = Guid.NewGuid().ToString("N");
        await CreateTestSlave(slaveId, "Test Slave", 1);

        var register = new Register
        {
            Slaveid = slaveId,
            Startaddr = 40001,
            Hexdata = "ABCD1234",
            Names = "寄存器A",
            Coefficients = "0.1,0.2"
        };
        await _repository.CreateAsync(register);

        // Act
        var result = await _repository.GetBySlaveIdAsync(slaveId);

        // Assert
        var returnedRegister = Assert.Single(result);
        Assert.Equal(slaveId, returnedRegister.Slaveid);
        Assert.Equal(40001, returnedRegister.Startaddr);
        Assert.Equal("ABCD1234", returnedRegister.Hexdata);
        Assert.Equal("寄存器A", returnedRegister.Names);
        Assert.Equal("0.1,0.2", returnedRegister.Coefficients);
    }

    [Fact]
    public async Task GetBySlaveIdAsync_MultipleRegisters_ShouldReturnOrderedByStartAddr()
    {
        // Arrange
        var slaveId = Guid.NewGuid().ToString("N");
        await CreateTestSlave(slaveId, "Test Slave", 1);

        // 创建多个寄存器，地址不按顺序
        var register1 = await _repository.CreateAsync(new Register
        {
            Slaveid = slaveId,
            Startaddr = 40010,
            Hexdata = "1111"
        });
        var register2 = await _repository.CreateAsync(new Register
        {
            Slaveid = slaveId,
            Startaddr = 40001,
            Hexdata = "2222"
        });
        var register3 = await _repository.CreateAsync(new Register
        {
            Slaveid = slaveId,
            Startaddr = 40005,
            Hexdata = "3333"
        });

        // Act
        var result = await _repository.GetBySlaveIdAsync(slaveId);

        // Assert
        Assert.Equal(3, result.Count());
        var orderedResult = result.ToList();
        Assert.Equal(40001, orderedResult[0].Startaddr);
        Assert.Equal(40005, orderedResult[1].Startaddr);
        Assert.Equal(40010, orderedResult[2].Startaddr);
    }

    [Fact]
    public async Task GetBySlaveIdAsync_DifferentSlaves_ShouldReturnOnlySpecifiedSlaveRegisters()
    {
        // Arrange
        var slaveId1 = Guid.NewGuid().ToString("N");
        var slaveId2 = Guid.NewGuid().ToString("N");
        await CreateTestSlave(slaveId1, "Slave 1", 1);
        await CreateTestSlave(slaveId2, "Slave 2", 2);

        await _repository.CreateAsync(new Register { Slaveid = slaveId1, Startaddr = 40001, Hexdata = "AAAA" });
        await _repository.CreateAsync(new Register { Slaveid = slaveId2, Startaddr = 40001, Hexdata = "BBBB" });
        await _repository.CreateAsync(new Register { Slaveid = slaveId1, Startaddr = 40002, Hexdata = "CCCC" });

        // Act
        var result = await _repository.GetBySlaveIdAsync(slaveId1);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, r => Assert.Equal(slaveId1, r.Slaveid));
        Assert.Contains(result, r => r.Hexdata == "AAAA");
        Assert.Contains(result, r => r.Hexdata == "CCCC");
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidRegister_ShouldCreateAndReturnRegister()
    {
        // Arrange
        var slaveId = Guid.NewGuid().ToString("N");
        await CreateTestSlave(slaveId, "Test Slave", 1);

        var register = new Register
        {
            Slaveid = slaveId,
            Startaddr = 40001,
            Hexdata = "ABCD1234",
            Names = "寄存器A",
            Coefficients = "0.1,0.2"
        };

        // Act
        var result = await _repository.CreateAsync(register);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(string.Empty, result.Id);
        Assert.Equal(slaveId, result.Slaveid);
        Assert.Equal(40001, result.Startaddr);
        Assert.Equal("ABCD1234", result.Hexdata);
        Assert.Equal("寄存器A", result.Names);
        Assert.Equal("0.1,0.2", result.Coefficients);

        // 验证数据库中确实创建了记录
        var stored = await _connection.QuerySingleAsync<Register>(
            "SELECT id, slaveid, startaddr, hexdata, names, coefficients FROM registers WHERE id = @Id",
            new { Id = result.Id });
        Assert.Equal("寄存器A", stored.Names);
        Assert.Equal("0.1,0.2", stored.Coefficients);
    }

    [Fact]
    public async Task CreateAsync_DuplicateStartAddrInSameSlave_ShouldThrowException()
    {
        // Arrange
        var slaveId = Guid.NewGuid().ToString("N");
        await CreateTestSlave(slaveId, "Test Slave", 1);

        var register1 = new Register
        {
            Slaveid = slaveId,
            Startaddr = 40001,
            Hexdata = "ABCD"
        };
        await _repository.CreateAsync(register1);

        var register2 = new Register
        {
            Slaveid = slaveId,
            Startaddr = 40001,  // 重复的起始地址
            Hexdata = "1234"
        };

        // Act & Assert
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(
            () => _repository.CreateAsync(register2));
    }

    [Fact]
    public async Task CreateAsync_SameStartAddrInDifferentSlaves_ShouldSucceed()
    {
        // Arrange
        var slaveId1 = Guid.NewGuid().ToString("N");
        var slaveId2 = Guid.NewGuid().ToString("N");
        await CreateTestSlave(slaveId1, "Slave 1", 1);
        await CreateTestSlave(slaveId2, "Slave 2", 2);

        var register1 = new Register
        {
            Slaveid = slaveId1,
            Startaddr = 40001,
            Hexdata = "AAAA"
        };
        var register2 = new Register
        {
            Slaveid = slaveId2,
            Startaddr = 40001,  // 相同地址但不同从机
            Hexdata = "BBBB"
        };

        // Act
        await _repository.CreateAsync(register1);
        await _repository.CreateAsync(register2);

        // Assert
        var count = await _connection.QuerySingleAsync<int>(
            "SELECT COUNT(1) FROM registers WHERE startaddr = 40001");
        Assert.Equal(2, count);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingRegister_ShouldUpdateSuccessfully()
    {
        // Arrange
        var slaveId = Guid.NewGuid().ToString("N");
        await CreateTestSlave(slaveId, "Test Slave", 1);

        var created = await _repository.CreateAsync(new Register
        {
            Slaveid = slaveId,
            Startaddr = 40001,
            Hexdata = "ABCD"
        });

        var updatedRegister = new Register
        {
            Id = created.Id,
            Slaveid = slaveId,
            Startaddr = 40002,
            Hexdata = "1234"
        };

        // Act
        var result = await _repository.UpdateAsync(updatedRegister);

        // Assert
        Assert.Equal(created.Id, result.Id);
        Assert.Equal(slaveId, result.Slaveid);
        Assert.Equal(40002, result.Startaddr);
        Assert.Equal("1234", result.Hexdata);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentRegister_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var register = new Register
        {
            Id = "non-existent-id",
            Slaveid = Guid.NewGuid().ToString("N"),
            Startaddr = 40001,
            Hexdata = "ABCD"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _repository.UpdateAsync(register));
        Assert.Contains("寄存器不存在", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_ChangeToDuplicateStartAddr_ShouldThrowException()
    {
        // Arrange
        var slaveId = Guid.NewGuid().ToString("N");
        await CreateTestSlave(slaveId, "Test Slave", 1);

        // 创建两个寄存器
        var register1 = await _repository.CreateAsync(new Register
        {
            Slaveid = slaveId,
            Startaddr = 40001,
            Hexdata = "ABCD"
        });
        await _repository.CreateAsync(new Register
        {
            Slaveid = slaveId,
            Startaddr = 40002,
            Hexdata = "1234"
        });

        // 尝试将register1的地址改为与第二个寄存器相同的地址
        var updatedRegister = new Register
        {
            Id = register1.Id,
            Slaveid = slaveId,
            Startaddr = 40002,  // 冲突地址
            Hexdata = "5678"
        };

        // Act & Assert
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(
            () => _repository.UpdateAsync(updatedRegister));
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingRegister_ShouldReturnTrue()
    {
        // Arrange
        var slaveId = Guid.NewGuid().ToString("N");
        await CreateTestSlave(slaveId, "Test Slave", 1);

        var created = await _repository.CreateAsync(new Register
        {
            Slaveid = slaveId,
            Startaddr = 40001,
            Hexdata = "ABCD"
        });

        // Act
        var result = await _repository.DeleteAsync(created.Id);

        // Assert
        Assert.True(result);

        // 验证数据库中确实删除了记录
        var count = await _connection.QuerySingleAsync<int>(
            "SELECT COUNT(1) FROM registers WHERE id = @Id", new { Id = created.Id });
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentRegister_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.DeleteAsync("non-existent-id");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Helper Methods

    private async Task CreateTestSlave(string id, string name, int slaveId)
    {
        // 创建连接
        var connectionId = Guid.NewGuid().ToString("N");
        const string connectionSql = @"
            INSERT INTO connections (id, name, port)
            VALUES (@Id, @Name, @Port)";

        // 使用随机端口避免冲突
        var randomPort = 10000 + Random.Shared.Next(50000);

        await _connection.ExecuteAsync(connectionSql, new
        {
            Id = connectionId,
            Name = $"Test Connection {Guid.NewGuid():N}",  // 使用唯一名称避免冲突
            Port = randomPort
        });

        // 创建从机
        const string slaveSql = @"
            INSERT INTO slaves (id, connid, name, slaveid)
            VALUES (@Id, @ConnId, @Name, @SlaveId)";

        await _connection.ExecuteAsync(slaveSql, new
        {
            Id = id,
            ConnId = connectionId,
            Name = name,
            SlaveId = slaveId
        });
    }

    #endregion
}
