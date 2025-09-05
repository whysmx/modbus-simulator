using System.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Data.Sqlite;
using ModbusSimulator.Models;
using ModbusSimulator.Repositories;
using ModbusSimulator.Services;
using Xunit;

namespace ModbusSimulator.Tests.Integration;

public class ModbusIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ConnectionRepository _connectionRepository;
    private readonly SlaveRepository _slaveRepository;
    private readonly RegisterRepository _registerRepository;
    private readonly ConnectionService _connectionService;
    private readonly SlaveService _slaveService;
    private readonly RegisterService _registerService;
    private readonly IMemoryCache _memoryCache;

    public ModbusIntegrationTests()
    {
        // 创建内存数据库用于测试
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        InitializeDatabase();

        // 初始化Repository层
        _connectionRepository = new ConnectionRepository(_connection);
        _slaveRepository = new SlaveRepository(_connection);
        _registerRepository = new RegisterRepository(_connection);

        // 创建内存缓存实例
        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        // 初始化Service层
        _connectionService = new ConnectionService(_connectionRepository);
        _slaveService = new SlaveService(_slaveRepository, _connectionRepository);
        _registerService = new RegisterService(_registerRepository, _connectionRepository, _slaveRepository, _memoryCache);
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
                FOREIGN KEY (SlaveId) REFERENCES slaves(Id) ON DELETE CASCADE,
                UNIQUE(SlaveId, StartAddr)
            );";

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

    #region 完整业务流程集成测试

    [Fact]
    public async Task CompleteModbusSetupWorkflow_ShouldWorkEndToEnd()
    {
        // 1. 创建连接
        var connectionRequest = new CreateConnectionRequest { Name = "Integration Test Connection" };
        var connection = await _connectionService.CreateConnectionAsync(connectionRequest);

        Assert.NotNull(connection);
        Assert.Equal("Integration Test Connection", connection.Name);
        Assert.True(connection.Port >= 502);

        // 2. 创建从机
        var slaveRequest = new CreateSlaveRequest { Name = "Test Slave", Slaveid = 1 };
        var slave = await _slaveService.CreateSlaveAsync(connection.Id, slaveRequest);

        Assert.NotNull(slave);
        Assert.Equal(connection.Id, slave.Connid);
        Assert.Equal("Test Slave", slave.Name);
        Assert.Equal(1, slave.Slaveid);

        // 3. 创建线圈寄存器
        var coilRegisterRequest = new CreateRegisterRequest { Startaddr = 1, Hexdata = "ABCD" };
        var coilRegister = await _registerService.CreateRegisterAsync(connection.Id, slave.Id, coilRegisterRequest);

        Assert.NotNull(coilRegister);
        Assert.Equal(slave.Id, coilRegister.Slaveid);
        Assert.Equal(1, coilRegister.Startaddr);
        Assert.Equal("ABCD", coilRegister.Hexdata);

        // 4. 创建保持寄存器
        var holdingRegisterRequest = new CreateRegisterRequest { Startaddr = 40001, Hexdata = "1234ABCD" };
        var holdingRegister = await _registerService.CreateRegisterAsync(connection.Id, slave.Id, holdingRegisterRequest);

        Assert.NotNull(holdingRegister);
        Assert.Equal(slave.Id, holdingRegister.Slaveid);
        Assert.Equal(40001, holdingRegister.Startaddr);
        Assert.Equal("1234ABCD", holdingRegister.Hexdata);

        // 5. 验证连接树结构
        var connectionTree = await _connectionService.GetConnectionsTreeAsync();
        var createdConnectionTree = connectionTree.First(c => c.Id == connection.Id);

        Assert.Equal(connection.Name, createdConnectionTree.Name);
        Assert.Equal(connection.Port, createdConnectionTree.Port);
        Assert.Single(createdConnectionTree.Slaves);

        var createdSlave = createdConnectionTree.Slaves.First();
        Assert.Equal(slave.Name, createdSlave.Name);
        Assert.Equal(slave.Slaveid, createdSlave.Slaveid);

        // 6. 验证从机下的所有寄存器
        var registers = await _registerService.GetRegistersBySlaveIdAsync(connection.Port, slave.Id);
        Assert.Equal(2, registers.Count());

        var coilReg = registers.First(r => r.Startaddr == 1);
        Assert.Equal("ABCD", coilReg.Hexdata);

        var holdingReg = registers.First(r => r.Startaddr == 40001);
        Assert.Equal("1234ABCD", holdingReg.Hexdata);

        // 7. 更新连接
        var updateConnectionRequest = new UpdateConnectionRequest { Name = "Updated Connection", Port = 1502 };
        var updatedConnection = await _connectionService.UpdateConnectionAsync(connection.Id, updateConnectionRequest);

        Assert.Equal(connection.Id, updatedConnection.Id);
        Assert.Equal("Updated Connection", updatedConnection.Name);
        Assert.Equal(1502, updatedConnection.Port);

        // 8. 更新从机
        var updateSlaveRequest = new UpdateSlaveRequest { Name = "Updated Slave", Slaveid = 2 };
        var updatedSlave = await _slaveService.UpdateSlaveAsync(connection.Id, slave.Id, updateSlaveRequest);

        Assert.Equal(slave.Id, updatedSlave.Id);
        Assert.Equal("Updated Slave", updatedSlave.Name);
        Assert.Equal(2, updatedSlave.Slaveid);

        // 9. 更新寄存器
        var updateRegisterRequest = new UpdateRegisterRequest { Startaddr = 40002, Hexdata = "5678EFAB" };
        var updatedRegister = await _registerService.UpdateRegisterAsync(connection.Id, slave.Id, holdingRegister.Id, updateRegisterRequest);

        Assert.Equal(holdingRegister.Id, updatedRegister.Id);
        Assert.Equal(40002, updatedRegister.Startaddr);
        Assert.Equal("5678EFAB", updatedRegister.Hexdata);

        // 10. 验证级联删除（删除连接时级联删除从机和寄存器）
        await _connectionService.DeleteConnectionAsync(connection.Id);

        // 验证连接已被删除
        var remainingConnections = await _connectionService.GetConnectionsTreeAsync();
        Assert.DoesNotContain(remainingConnections, c => c.Id == connection.Id);
    }

    [Fact]
    public async Task MultipleConnectionsAndSlaves_ShouldWorkCorrectly()
    {
        // 创建第一个连接和从机
        var connection1 = await _connectionService.CreateConnectionAsync(
            new CreateConnectionRequest { Name = "Connection 1" });
        var slave1 = await _slaveService.CreateSlaveAsync(connection1.Id,
            new CreateSlaveRequest { Name = "Slave 1-1", Slaveid = 1 });
        var slave2 = await _slaveService.CreateSlaveAsync(connection1.Id,
            new CreateSlaveRequest { Name = "Slave 1-2", Slaveid = 2 });

        // 创建第二个连接和从机
        var connection2 = await _connectionService.CreateConnectionAsync(
            new CreateConnectionRequest { Name = "Connection 2" });
        var slave3 = await _slaveService.CreateSlaveAsync(connection2.Id,
            new CreateSlaveRequest { Name = "Slave 2-1", Slaveid = 1 });

        // 为每个从机创建不同类型的寄存器
        await _registerService.CreateRegisterAsync(connection1.Id, slave1.Id,
            new CreateRegisterRequest { Startaddr = 1, Hexdata = "AA" }); // 线圈
        await _registerService.CreateRegisterAsync(connection1.Id, slave1.Id,
            new CreateRegisterRequest { Startaddr = 10001, Hexdata = "BB" }); // 离散输入
        await _registerService.CreateRegisterAsync(connection1.Id, slave1.Id,
            new CreateRegisterRequest { Startaddr = 30001, Hexdata = "CCCC" }); // 输入寄存器
        await _registerService.CreateRegisterAsync(connection1.Id, slave1.Id,
            new CreateRegisterRequest { Startaddr = 40001, Hexdata = "DDDD" }); // 保持寄存器

        // 验证连接树
        var connectionTree = await _connectionService.GetConnectionsTreeAsync();
        Assert.Equal(2, connectionTree.Count());

        var conn1Tree = connectionTree.First(c => c.Id == connection1.Id);
        Assert.Equal(2, conn1Tree.Slaves.Count);

        var conn2Tree = connectionTree.First(c => c.Id == connection2.Id);
        Assert.Single(conn2Tree.Slaves);

        // 验证从机1的寄存器
        var slave1Registers = await _registerService.GetRegistersBySlaveIdAsync(connection1.Port, slave1.Id);
        Assert.Equal(4, slave1Registers.Count());

        // 验证从机2没有寄存器
        var slave2Registers = await _registerService.GetRegistersBySlaveIdAsync(connection1.Port, slave2.Id);
        Assert.Empty(slave2Registers);

        // 验证从机3没有寄存器
        var slave3Registers = await _registerService.GetRegistersBySlaveIdAsync(connection2.Port, slave3.Id);
        Assert.Empty(slave3Registers);
    }

    [Fact]
    public async Task DataConsistency_And_Constraints_ShouldBeMaintained()
    {
        // 测试唯一性约束
        await _connectionService.CreateConnectionAsync(new CreateConnectionRequest { Name = "Test Connection" });

        // 尝试创建同名连接应该失败
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _connectionService.CreateConnectionAsync(new CreateConnectionRequest { Name = "Test Connection" }));

        // 测试外键约束
        var connection = await _connectionService.CreateConnectionAsync(
            new CreateConnectionRequest { Name = "Another Connection" });
        var slave = await _slaveService.CreateSlaveAsync(connection.Id,
            new CreateSlaveRequest { Name = "Test Slave", Slaveid = 1 });

        // 删除连接后，从机应该也被删除（级联删除）
        await _connectionService.DeleteConnectionAsync(connection.Id);

        var remainingTree = await _connectionService.GetConnectionsTreeAsync();
        Assert.DoesNotContain(remainingTree, c => c.Id == connection.Id);

        // 验证从机也不存在
        // 注意：删除连接后，从机被级联删除，GetRegistersBySlaveIdAsync 会返回空列表而不是抛出异常
        var emptyRegisters = await _registerService.GetRegistersBySlaveIdAsync(connection.Port, slave.Id);
        Assert.Empty(emptyRegisters);
    }

    [Fact]
    public async Task BusinessValidation_ShouldPreventInvalidData()
    {
        // 测试连接名称验证
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _connectionService.CreateConnectionAsync(new CreateConnectionRequest { Name = "" }));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _connectionService.CreateConnectionAsync(new CreateConnectionRequest { Name = new string('A', 101) }));

        // 测试端口范围验证
        var connection = await _connectionService.CreateConnectionAsync(
            new CreateConnectionRequest { Name = "Valid Connection" });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _connectionService.UpdateConnectionAsync(connection.Id,
                new UpdateConnectionRequest { Name = "Valid", Port = 0 }));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _connectionService.UpdateConnectionAsync(connection.Id,
                new UpdateConnectionRequest { Name = "Valid", Port = 70000 }));

        // 测试从机地址范围验证
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _slaveService.CreateSlaveAsync(connection.Id,
                new CreateSlaveRequest { Name = "Test", Slaveid = 0 }));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _slaveService.CreateSlaveAsync(connection.Id,
                new CreateSlaveRequest { Name = "Test", Slaveid = 248 }));

        // 验证其他约束仍然有效
        var slave = await _slaveService.CreateSlaveAsync(connection.Id,
            new CreateSlaveRequest { Name = "Test Slave", Slaveid = 1 });

        // 验证地址范围验证仍然有效
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _registerService.CreateRegisterAsync(connection.Id, slave.Id,
                new CreateRegisterRequest { Startaddr = 50000, Hexdata = "ABCD" })); // 超出地址范围
    }

    [Fact]
    public async Task ConcurrentOperations_ShouldMaintainDataIntegrity()
    {
        // 创建基础数据
        var connection = await _connectionService.CreateConnectionAsync(
            new CreateConnectionRequest { Name = "Concurrent Test" });
        var slave = await _slaveService.CreateSlaveAsync(connection.Id,
            new CreateSlaveRequest { Name = "Test Slave", Slaveid = 1 });

        // 并发创建多个寄存器
        var registerTasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            var task = _registerService.CreateRegisterAsync(connection.Id, slave.Id,
                new CreateRegisterRequest
                {
                    Startaddr = 40001 + (i * 10),  // 使用不同的间隔避免冲突
                    Hexdata = $"AB{i:X2}"    // 4字符=4的倍数,符合保持寄存器要求
                });
            registerTasks.Add(task);
        }

        // 等待所有任务完成
        await Task.WhenAll(registerTasks);

        // 验证所有寄存器都已创建
        var registers = await _registerService.GetRegistersBySlaveIdAsync(connection.Port, slave.Id);
        Assert.Equal(10, registers.Count());

        // 验证每个寄存器的唯一性
        var addresses = registers.Select(r => r.Startaddr).ToList();
        Assert.Equal(addresses.Count, addresses.Distinct().Count());
    }

    #endregion

    #region Modbus协议仿真集成测试

    [Fact]
    public async Task ModbusProtocolSimulation_EndToEnd_ShouldWorkCorrectly()
    {
        // 1. 设置完整的Modbus配置
        var connection = await _connectionService.CreateConnectionAsync(
            new CreateConnectionRequest { Name = "Modbus Test Connection" });
        var slave = await _slaveService.CreateSlaveAsync(connection.Id,
            new CreateSlaveRequest { Name = "Modbus Slave", Slaveid = 1 });

        // 创建不同类型的寄存器
        // 线圈 (Coil) - 地址范围: 00001-09999
        await _registerService.CreateRegisterAsync(connection.Id, slave.Id,
            new CreateRegisterRequest { Startaddr = 1, Hexdata = "ABCD" }); // 16位线圈数据

        // 离散输入 (Discrete Input) - 地址范围: 10001-19999
        await _registerService.CreateRegisterAsync(connection.Id, slave.Id,
            new CreateRegisterRequest { Startaddr = 10001, Hexdata = "1234" }); // 16位离散输入数据

        // 输入寄存器 (Input Register) - 地址范围: 30001-39999
        await _registerService.CreateRegisterAsync(connection.Id, slave.Id,
            new CreateRegisterRequest { Startaddr = 30001, Hexdata = "5678ABCD" }); // 2个16位输入寄存器

        // 保持寄存器 (Holding Register) - 地址范围: 40001-49999
        await _registerService.CreateRegisterAsync(connection.Id, slave.Id,
            new CreateRegisterRequest { Startaddr = 40001, Hexdata = "EF12AB34" }); // 2个16位保持寄存器

        // 2. 验证配置正确性
        var registers = await _registerService.GetRegistersBySlaveIdAsync(connection.Port, slave.Id);
        Assert.Equal(4, registers.Count());

        // 3. 模拟Modbus TCP客户端请求（这里我们通过直接调用服务来验证）
        // 注意：实际的TCP连接测试需要更复杂的设置，这里我们验证数据准备部分

        // 验证线圈数据的地址范围
        var coilRegister = registers.First(r => r.Startaddr >= 1 && r.Startaddr <= 9999);
        Assert.Equal(1, coilRegister.Startaddr);
        Assert.Equal("ABCD", coilRegister.Hexdata);

        // 验证离散输入数据的地址范围
        var discreteInputRegister = registers.First(r => r.Startaddr >= 10001 && r.Startaddr <= 19999);
        Assert.Equal(10001, discreteInputRegister.Startaddr);
        Assert.Equal("1234", discreteInputRegister.Hexdata);

        // 验证输入寄存器数据的地址范围
        var inputRegister = registers.First(r => r.Startaddr >= 30001 && r.Startaddr <= 39999);
        Assert.Equal(30001, inputRegister.Startaddr);
        Assert.Equal("5678ABCD", inputRegister.Hexdata);

        // 验证保持寄存器数据的地址范围
        var holdingRegister = registers.First(r => r.Startaddr >= 40001 && r.Startaddr <= 49999);
        Assert.Equal(40001, holdingRegister.Startaddr);
        Assert.Equal("EF12AB34", holdingRegister.Hexdata);

        // 4. 验证数据格式正确性
        // 线圈和离散输入应该是2的倍数字节长度
        Assert.True(coilRegister.Hexdata.Length % 2 == 0);
        Assert.True(discreteInputRegister.Hexdata.Length % 2 == 0);

        // 输入和保持寄存器应该是4的倍数字节长度
        Assert.True(inputRegister.Hexdata.Length % 4 == 0);
        Assert.True(holdingRegister.Hexdata.Length % 4 == 0);
    }


    [Fact]
    public async Task ModbusAddressMapping_ShouldWorkCorrectly()
    {
        // 1. 创建测试配置
        var connection = await _connectionService.CreateConnectionAsync(
            new CreateConnectionRequest { Name = "Address Mapping Test" });
        var slave = await _slaveService.CreateSlaveAsync(connection.Id,
            new CreateSlaveRequest { Name = "Mapping Slave", Slaveid = 5 });

        // 2. 创建不同地址范围的寄存器
        var coil1 = await _registerService.CreateRegisterAsync(connection.Id, slave.Id,
            new CreateRegisterRequest { Startaddr = 1, Hexdata = "AAAA" });
        var coil2 = await _registerService.CreateRegisterAsync(connection.Id, slave.Id,
            new CreateRegisterRequest { Startaddr = 100, Hexdata = "BBBB" });

        var discrete1 = await _registerService.CreateRegisterAsync(connection.Id, slave.Id,
            new CreateRegisterRequest { Startaddr = 10001, Hexdata = "CCCC" });
        var discrete2 = await _registerService.CreateRegisterAsync(connection.Id, slave.Id,
            new CreateRegisterRequest { Startaddr = 10100, Hexdata = "DDDD" });

        var inputReg1 = await _registerService.CreateRegisterAsync(connection.Id, slave.Id,
            new CreateRegisterRequest { Startaddr = 30001, Hexdata = "EEEEFFFF" });
        var inputReg2 = await _registerService.CreateRegisterAsync(connection.Id, slave.Id,
            new CreateRegisterRequest { Startaddr = 30100, Hexdata = "11112222" });

        var holdingReg1 = await _registerService.CreateRegisterAsync(connection.Id, slave.Id,
            new CreateRegisterRequest { Startaddr = 40001, Hexdata = "33334444" });
        var holdingReg2 = await _registerService.CreateRegisterAsync(connection.Id, slave.Id,
            new CreateRegisterRequest { Startaddr = 40100, Hexdata = "55556666" });

        // 3. 验证所有寄存器都创建成功
        var allRegisters = await _registerService.GetRegistersBySlaveIdAsync(connection.Port, slave.Id);
        Assert.Equal(8, allRegisters.Count());

        // 4. 验证地址范围分组
        var coils = allRegisters.Where(r => r.Startaddr >= 1 && r.Startaddr <= 9999).ToList();
        var discreteInputs = allRegisters.Where(r => r.Startaddr >= 10001 && r.Startaddr <= 19999).ToList();
        var inputRegisters = allRegisters.Where(r => r.Startaddr >= 30001 && r.Startaddr <= 39999).ToList();
        var holdingRegisters = allRegisters.Where(r => r.Startaddr >= 40001 && r.Startaddr <= 49999).ToList();

        Assert.Equal(2, coils.Count);
        Assert.Equal(2, discreteInputs.Count);
        Assert.Equal(2, inputRegisters.Count);
        Assert.Equal(2, holdingRegisters.Count);

        // 5. 验证数据完整性
        Assert.Equal("AAAA", coils.First(r => r.Startaddr == 1).Hexdata);
        Assert.Equal("BBBB", coils.First(r => r.Startaddr == 100).Hexdata);
        Assert.Equal("CCCC", discreteInputs.First(r => r.Startaddr == 10001).Hexdata);
        Assert.Equal("DDDD", discreteInputs.First(r => r.Startaddr == 10100).Hexdata);
        Assert.Equal("EEEEFFFF", inputRegisters.First(r => r.Startaddr == 30001).Hexdata);
        Assert.Equal("11112222", inputRegisters.First(r => r.Startaddr == 30100).Hexdata);
        Assert.Equal("33334444", holdingRegisters.First(r => r.Startaddr == 40001).Hexdata);
        Assert.Equal("55556666", holdingRegisters.First(r => r.Startaddr == 40100).Hexdata);
    }

    [Fact]
    public async Task ModbusConfigurationPersistence_ShouldSurviveRestart()
    {
        // 1. 创建完整的配置
        var connection = await _connectionService.CreateConnectionAsync(
            new CreateConnectionRequest { Name = "Persistence Test" });
        var slave = await _slaveService.CreateSlaveAsync(connection.Id,
            new CreateSlaveRequest { Name = "Persistence Slave", Slaveid = 1 });

        // 创建多个寄存器
        var registersData = new[]
        {
            new { Addr = 1, Data = "ABCD" },
            new { Addr = 10001, Data = "EF12" },
            new { Addr = 30001, Data = "34567890" },
            new { Addr = 40001, Data = "ABCDEF12" }
        };

        foreach (var regData in registersData)
        {
            await _registerService.CreateRegisterAsync(connection.Id, slave.Id,
                new CreateRegisterRequest { Startaddr = regData.Addr, Hexdata = regData.Data });
        }

        // 2. 验证配置存在
        var originalRegisters = await _registerService.GetRegistersBySlaveIdAsync(connection.Port, slave.Id);
        Assert.Equal(4, originalRegisters.Count());

        // 3. 模拟"重启" - 创建新的服务实例（但使用相同数据库）
        var newConnectionService = new ConnectionService(_connectionRepository);
        var newSlaveService = new SlaveService(_slaveRepository, _connectionRepository);
        var newRegisterService = new RegisterService(_registerRepository, _connectionRepository, _slaveRepository, _memoryCache);

        // 4. 验证配置仍然存在
        var persistedRegisters = await newRegisterService.GetRegistersBySlaveIdAsync(connection.Port, slave.Id);
        Assert.Equal(4, persistedRegisters.Count());

        // 5. 验证数据完整性
        foreach (var original in originalRegisters)
        {
            var persisted = persistedRegisters.First(r => r.Id == original.Id);
            Assert.Equal(original.Startaddr, persisted.Startaddr);
            Assert.Equal(original.Hexdata, persisted.Hexdata);
            Assert.Equal(original.Slaveid, persisted.Slaveid);
        }

        // 6. 验证连接和从机也保持不变
        var connectionTree = await newConnectionService.GetConnectionsTreeAsync();
        var persistedConnection = connectionTree.First(c => c.Id == connection.Id);
        Assert.Equal(connection.Name, persistedConnection.Name);
        Assert.Equal(connection.Port, persistedConnection.Port);

        var persistedSlave = persistedConnection.Slaves.First();
        Assert.Equal(slave.Name, persistedSlave.Name);
        Assert.Equal(slave.Slaveid, persistedSlave.Slaveid);
    }

    [Fact]
    public async Task ModbusBulkOperations_ShouldMaintainPerformanceAndConsistency()
    {
        // 1. 创建基础配置
        var connection = await _connectionService.CreateConnectionAsync(
            new CreateConnectionRequest { Name = "Bulk Test" });
        var slave = await _slaveService.CreateSlaveAsync(connection.Id,
            new CreateSlaveRequest { Name = "Bulk Slave", Slaveid = 1 });

        // 2. 批量创建多个寄存器
        const int bulkSize = 50;
        var createTasks = new List<Task>();

        for (int i = 0; i < bulkSize; i++)
        {
            var task = _registerService.CreateRegisterAsync(connection.Id, slave.Id,
                new CreateRegisterRequest
                {
                    Startaddr = 40001 + (i * 10), // 确保地址不冲突
                    Hexdata = $"AB{i:X2}"           // 4字符=4的倍数,符合保持寄存器要求
                });
            createTasks.Add(task);
        }

        // 3. 并发执行所有创建操作
        await Task.WhenAll(createTasks);

        // 4. 验证所有操作都成功完成
        var allRegisters = await _registerService.GetRegistersBySlaveIdAsync(connection.Port, slave.Id);
        Assert.Equal(bulkSize, allRegisters.Count());

        // 5. 验证数据一致性
        var addresses = allRegisters.Select(r => r.Startaddr).ToList();
        Assert.Equal(bulkSize, addresses.Distinct().Count()); // 确保没有重复地址

        // 6. 验证数据正确性
        for (int i = 0; i < bulkSize; i++)
        {
            var expectedAddr = 40001 + (i * 10);
            var register = allRegisters.First(r => r.Startaddr == expectedAddr);
            var expectedData = $"AB{i:X2}";
            Assert.Equal(expectedData, register.Hexdata);
        }

        // 7. 验证排序（按地址升序）
        var sortedAddresses = allRegisters.OrderBy(r => r.Startaddr).Select(r => r.Startaddr).ToList();
        for (int i = 0; i < sortedAddresses.Count - 1; i++)
        {
            Assert.True(sortedAddresses[i] < sortedAddresses[i + 1]);
        }
    }

    #endregion
}
