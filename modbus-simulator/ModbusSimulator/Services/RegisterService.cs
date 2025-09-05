using Microsoft.Extensions.Caching.Memory;
using ModbusSimulator.Models;
using ModbusSimulator.Repositories;

namespace ModbusSimulator.Services;

public class RegisterService : IRegisterService
{
    private readonly IRegisterRepository _registerRepository;
    private readonly IConnectionRepository _connectionRepository;
    private readonly ISlaveRepository _slaveRepository;
    private readonly IMemoryCache _cache;

    public RegisterService(
        IRegisterRepository registerRepository,
        IConnectionRepository connectionRepository,
        ISlaveRepository slaveRepository,
        IMemoryCache cache)
    {
        _registerRepository = registerRepository;
        _connectionRepository = connectionRepository;
        _slaveRepository = slaveRepository;
        _cache = cache;
    }

    public async Task<IEnumerable<Register>> GetRegistersBySlaveIdAsync(int port, string slaveId)
    {
        // 验证从机ID
        if (string.IsNullOrWhiteSpace(slaveId))
        {
            throw new ArgumentException("从机ID不能为空", nameof(slaveId));
        }

        // 使用基于端口的缓存
        string cacheKey = GetPortSlaveRegistersCacheKey(port, slaveId);
        if (_cache.TryGetValue(cacheKey, out IEnumerable<Register> cachedRegisters))
        {
            return cachedRegisters;
        }

        // 从数据库获取数据
        var registers = await _registerRepository.GetBySlaveIdAsync(slaveId);
        
        // 缓存结果，过期时间设为24小时（主要依靠手动清除）
        _cache.Set(cacheKey, registers, TimeSpan.FromHours(24));
        
        return registers;
    }

    public async Task<Register> CreateRegisterAsync(string connectionId, string slaveId, CreateRegisterRequest request)
    {
        // 验证参数
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            throw new ArgumentException("连接ID不能为空", nameof(connectionId));
        }

        if (string.IsNullOrWhiteSpace(slaveId))
        {
            throw new ArgumentException("从机ID不能为空", nameof(slaveId));
        }

        // 验证连接和从机是否存在，并获取连接信息
        var connection = await ValidateConnectionAndSlaveAsync(connectionId, slaveId);

        // 业务验证 - 只保留地址范围校验
        if (request.Startaddr < 0)
        {
            throw new ArgumentException("起始地址不能为负数", "request.Startaddr");
        }

        // 验证地址范围
        if (!IsValidAddressRange(request.Startaddr))
        {
            throw new ArgumentException("起始地址不在有效范围内（1-9999线圈，10001-19999离散输入，30001-39999输入寄存器，40001-49999保持寄存器）", "request.Startaddr");
        }

        var register = new Register
        {
            Slaveid = slaveId,
            Startaddr = request.Startaddr,
            Hexdata = request.Hexdata // 保存原始数据，不做任何处理
        };

        try
        {
            var result = await _registerRepository.CreateAsync(register);
            
            // 清除相关缓存，使用端口和从站ID
            ClearPortSlaveCache(connection.Port, slaveId);
            
            return result;
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("UNIQUE constraint failed"))
        {
            throw new InvalidOperationException("该从机下已存在相同起始地址的寄存器", ex);
        }
    }

    public async Task<Register> UpdateRegisterAsync(string connectionId, string slaveId, string registerId, UpdateRegisterRequest request)
    {
        // 验证参数
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            throw new ArgumentException("连接ID不能为空", nameof(connectionId));
        }

        if (string.IsNullOrWhiteSpace(slaveId))
        {
            throw new ArgumentException("从机ID不能为空", nameof(slaveId));
        }

        if (string.IsNullOrWhiteSpace(registerId))
        {
            throw new ArgumentException("寄存器ID不能为空", nameof(registerId));
        }

        // 验证连接和从机是否存在，并获取连接信息
        var connection = await ValidateConnectionAndSlaveAsync(connectionId, slaveId);

        // 业务验证 - 只保留地址范围校验
        if (request.Startaddr < 0)
        {
            throw new ArgumentException("起始地址不能为负数", "request.Startaddr");
        }

        // 验证地址范围
        if (!IsValidAddressRange(request.Startaddr))
        {
            throw new ArgumentException("起始地址不在有效范围内（1-9999线圈，10001-19999离散输入，30001-39999输入寄存器，40001-49999保持寄存器）", "request.Startaddr");
        }

        var register = new Register
        {
            Id = registerId,
            Slaveid = slaveId,
            Startaddr = request.Startaddr,
            Hexdata = request.Hexdata // 保存原始数据，不做任何处理
        };

        try
        {
            var result = await _registerRepository.UpdateAsync(register);
            
            // 清除相关缓存，使用端口和从站ID
            ClearPortSlaveCache(connection.Port, slaveId);
            
            return result;
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("UNIQUE constraint failed"))
        {
            throw new InvalidOperationException("该从机下已存在相同起始地址的寄存器", ex);
        }
    }

    public async Task DeleteRegisterAsync(string connectionId, string slaveId, string registerId)
    {
        // 验证参数
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            throw new ArgumentException("连接ID不能为空", nameof(connectionId));
        }

        if (string.IsNullOrWhiteSpace(slaveId))
        {
            throw new ArgumentException("从机ID不能为空", nameof(slaveId));
        }

        if (string.IsNullOrWhiteSpace(registerId))
        {
            throw new ArgumentException("寄存器ID不能为空", nameof(registerId));
        }

        // 验证连接和从机是否存在，并获取连接信息
        var connection = await ValidateConnectionAndSlaveAsync(connectionId, slaveId);

        await _registerRepository.DeleteAsync(registerId);
        
        // 清除相关缓存，使用端口和从站ID
        ClearPortSlaveCache(connection.Port, slaveId);
    }

    private async Task<Connection> ValidateConnectionAndSlaveAsync(string connectionId, string slaveId)
    {
        var connections = await _connectionRepository.GetConnectionsTreeAsync();
        var connection = connections.FirstOrDefault(c => c.Id == connectionId);
        if (connection == null)
        {
            throw new KeyNotFoundException("连接不存在");
        }

        var slave = connection.Slaves.FirstOrDefault(s => s.Id == slaveId);
        if (slave == null)
        {
            throw new KeyNotFoundException("从机不存在");
        }

        return connection;
    }

    private static bool IsValidAddressRange(int address)
    {
        return (address >= 1 && address <= 9999) ||          // 线圈
               (address >= 10001 && address <= 19999) ||     // 离散输入
               (address >= 30001 && address <= 39999) ||     // 输入寄存器
               (address >= 40001 && address <= 49999);       // 保持寄存器
    }

    // 缓存相关方法
    private string GetPortSlaveRegistersCacheKey(int port, string slaveId)
    {
        return $"port_slave_registers_{port}_{slaveId}";
    }

    private void ClearPortSlaveCache(int port, string slaveId)
    {
        string cacheKey = GetPortSlaveRegistersCacheKey(port, slaveId);
        _cache.Remove(cacheKey);
    }
}
