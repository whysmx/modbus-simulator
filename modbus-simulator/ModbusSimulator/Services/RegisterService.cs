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
    private readonly ICacheService _cacheService;

    public RegisterService(
        IRegisterRepository registerRepository,
        IConnectionRepository connectionRepository,
        ISlaveRepository slaveRepository,
        IMemoryCache cache,
        ICacheService cacheService)
    {
        _registerRepository = registerRepository;
        _connectionRepository = connectionRepository;
        _slaveRepository = slaveRepository;
        _cache = cache;
        _cacheService = cacheService;
    }

    public async Task<IEnumerable<Register>> GetRegistersBySlaveIdAsync(int port, string slaveAddress)
    {
        // 验证从机地址
        if (string.IsNullOrWhiteSpace(slaveAddress))
        {
            throw new ArgumentException("从机地址不能为空", nameof(slaveAddress));
        }

        // 添加调试信息
        Console.WriteLine($"[DEBUG] GetRegistersBySlaveIdAsync - Port: {port}, SlaveAddress: '{slaveAddress}' (Length: {slaveAddress.Length})");

        // 首先根据端口查找连接，然后根据从机地址查找从机ID
        var connections = await _connectionRepository.GetConnectionsTreeAsync();
        var connection = connections.FirstOrDefault(c => c.Port == port);
        if (connection == null)
        {
            Console.WriteLine($"[DEBUG] No connection found for port {port}");
            return new List<Register>();
        }

        // 将字符串从机地址转换为整数进行匹配，添加更详细的错误信息
        if (!int.TryParse(slaveAddress.Trim(), out int slaveAddressInt))
        {
            Console.WriteLine($"[DEBUG] Failed to parse slaveAddress: '{slaveAddress}' (after trim: '{slaveAddress.Trim()}')");
            // 尝试解析为十六进制
            if (slaveAddress.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(slaveAddress[2..], System.Globalization.NumberStyles.HexNumber, null, out slaveAddressInt))
                {
                    throw new ArgumentException($"从机地址格式无效: '{slaveAddress}' - 无法解析为十进制或十六进制数字", nameof(slaveAddress));
                }
            }
            else
            {
                throw new ArgumentException($"从机地址格式无效: '{slaveAddress}' - 必须是有效的数字", nameof(slaveAddress));
            }
        }

        Console.WriteLine($"[DEBUG] Parsed slaveAddress {slaveAddress} to int {slaveAddressInt}");

        // 使用规范化（十进制）从机地址作为缓存键的一部分，确保清理时能命中
        string normalizedSlaveAddress = slaveAddressInt.ToString();
        string cacheKey = GetPortSlaveRegistersCacheKey(port, normalizedSlaveAddress);
        if (_cache.TryGetValue<IEnumerable<Register>>(cacheKey, out var cachedRegisters))
        {
            return cachedRegisters;
        }

        var slave = connection.Slaves.FirstOrDefault(s => s.Slaveid == slaveAddressInt);
        if (slave == null)
        {
            Console.WriteLine($"[DEBUG] No slave found with Slaveid {slaveAddressInt}. Available slaves:");
            foreach (var s in connection.Slaves)
            {
                Console.WriteLine($"[DEBUG] - Slave ID: {s.Id}, Slaveid: {s.Slaveid}");
            }
            return new List<Register>();
        }

        // 从数据库获取数据，使用从机的UUID
        var registers = await _registerRepository.GetBySlaveIdAsync(slave.Id);
        
        // 调试信息：记录查找到的寄存器数量和数据
        Console.WriteLine($"[DEBUG] Found slave {slave.Id} with {registers.Count()} registers");
        foreach (var reg in registers)
        {
            Console.WriteLine($"[DEBUG] Register - Addr: {reg.Startaddr}, Data: '{reg.Hexdata}'");
        }
        
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
            Hexdata = request.Hexdata, // 保存原始数据，不做任何处理
            Names = request.Names ?? string.Empty,
            Coefficients = request.Coefficients ?? string.Empty
        };

        try
        {
            var result = await _registerRepository.CreateAsync(register);
            
            // 清除相关缓存，使用从机的slaveid
            var slave = connection.Slaves.FirstOrDefault(s => s.Id == slaveId);
            if (slave != null)
            {
                _cacheService.ClearSlaveCache(connection.Port, slave.Slaveid);
            }
            
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
            Hexdata = request.Hexdata, // 保存原始数据，不做任何处理
            Names = request.Names ?? string.Empty,
            Coefficients = request.Coefficients ?? "1"
        };

        try
        {
            var result = await _registerRepository.UpdateAsync(register);
            
            // 清除相关缓存，使用从机的slaveid
            var slave = connection.Slaves.FirstOrDefault(s => s.Id == slaveId);
            if (slave != null)
            {
                _cacheService.ClearSlaveCache(connection.Port, slave.Slaveid);
            }
            
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
        
        // 清除相关缓存，使用从机的slaveid
        var slave = connection.Slaves.FirstOrDefault(s => s.Id == slaveId);
        if (slave != null)
        {
            _cacheService.ClearSlaveCache(connection.Port, slave.Slaveid);
        }
    }

    private async Task<ConnectionTree> ValidateConnectionAndSlaveAsync(string connectionId, string slaveId)
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
    private string GetPortSlaveRegistersCacheKey(int port, string slaveAddress)
    {
        return $"port_slave_registers_{port}_{slaveAddress}";
    }
}
