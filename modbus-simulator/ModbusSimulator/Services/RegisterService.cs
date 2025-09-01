using ModbusSimulator.Models;
using ModbusSimulator.Repositories;

namespace ModbusSimulator.Services;

public class RegisterService : IRegisterService
{
    private readonly IRegisterRepository _registerRepository;
    private readonly IConnectionRepository _connectionRepository;
    private readonly ISlaveRepository _slaveRepository;

    public RegisterService(
        IRegisterRepository registerRepository,
        IConnectionRepository connectionRepository,
        ISlaveRepository slaveRepository)
    {
        _registerRepository = registerRepository;
        _connectionRepository = connectionRepository;
        _slaveRepository = slaveRepository;
    }

    public async Task<IEnumerable<Register>> GetRegistersBySlaveIdAsync(string slaveId)
    {
        // 验证从机ID
        if (string.IsNullOrWhiteSpace(slaveId))
        {
            throw new ArgumentException("从机ID不能为空", nameof(slaveId));
        }

        // 验证从机是否存在
        var connections = await _connectionRepository.GetConnectionsTreeAsync();
        var slaveExists = connections.Any(c => c.Slaves.Any(s => s.Id == slaveId));
        if (!slaveExists)
        {
            throw new KeyNotFoundException("从机不存在");
        }

        return await _registerRepository.GetBySlaveIdAsync(slaveId);
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

        // 验证连接和从机是否存在
        await ValidateConnectionAndSlaveAsync(connectionId, slaveId);

        // 业务验证
        if (request.Startaddr < 0)
        {
            throw new ArgumentException("起始地址不能为负数", nameof(request.Startaddr));
        }

        if (string.IsNullOrWhiteSpace(request.Hexdata))
        {
            throw new ArgumentException("十六进制数据不能为空", nameof(request.Hexdata));
        }

        // 验证十六进制格式
        if (!IsValidHexString(request.Hexdata))
        {
            throw new ArgumentException("十六进制数据格式无效，只能包含0-9和A-F字符", nameof(request.Hexdata));
        }

        // 根据地址范围验证数据长度
        ValidateHexDataLength(request.Startaddr, request.Hexdata);

        var register = new Register
        {
            Slaveid = slaveId,
            Startaddr = request.Startaddr,
            Hexdata = request.Hexdata.ToUpper()
        };

        try
        {
            return await _registerRepository.CreateAsync(register);
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

        // 验证连接和从机是否存在
        await ValidateConnectionAndSlaveAsync(connectionId, slaveId);

        // 业务验证
        if (request.Startaddr < 0)
        {
            throw new ArgumentException("起始地址不能为负数", nameof(request.Startaddr));
        }

        if (string.IsNullOrWhiteSpace(request.Hexdata))
        {
            throw new ArgumentException("十六进制数据不能为空", nameof(request.Hexdata));
        }

        // 验证十六进制格式
        if (!IsValidHexString(request.Hexdata))
        {
            throw new ArgumentException("十六进制数据格式无效，只能包含0-9和A-F字符", nameof(request.Hexdata));
        }

        // 根据地址范围验证数据长度
        ValidateHexDataLength(request.Startaddr, request.Hexdata);

        var register = new Register
        {
            Id = registerId,
            Slaveid = slaveId,
            Startaddr = request.Startaddr,
            Hexdata = request.Hexdata.ToUpper()
        };

        try
        {
            return await _registerRepository.UpdateAsync(register);
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

        // 验证连接和从机是否存在
        await ValidateConnectionAndSlaveAsync(connectionId, slaveId);

        await _registerRepository.DeleteAsync(registerId);
    }

    private async Task ValidateConnectionAndSlaveAsync(string connectionId, string slaveId)
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
    }

    internal static bool IsValidHexString(string hexString)
    {
        if (string.IsNullOrEmpty(hexString))
        {
            return false;
        }
        return hexString.All(c => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'));
    }

    internal static void ValidateHexDataLength(int startAddr, string hexData)
    {
        // 根据地址范围确定寄存器类型和长度要求
        if (startAddr >= 30001 && startAddr <= 39999) // 输入寄存器
        {
            if (hexData.Length % 4 != 0)
            {
                throw new ArgumentException("输入寄存器数据长度必须是4的倍数（每4个十六进制字符代表1个16位寄存器）", nameof(hexData));
            }
        }
        else if (startAddr >= 40001 && startAddr <= 49999) // 保持寄存器
        {
            if (hexData.Length % 4 != 0)
            {
                throw new ArgumentException("保持寄存器数据长度必须是4的倍数（每4个十六进制字符代表1个16位寄存器）", nameof(hexData));
            }
        }
        else if (startAddr >= 1 && startAddr <= 9999) // 线圈
        {
            if (hexData.Length % 2 != 0)
            {
                throw new ArgumentException("线圈数据长度必须是2的倍数（每2个十六进制字符代表1字节=8个线圈）", nameof(hexData));
            }
        }
        else if (startAddr >= 10001 && startAddr <= 19999) // 离散输入
        {
            if (hexData.Length % 2 != 0)
            {
                throw new ArgumentException("离散输入数据长度必须是2的倍数（每2个十六进制字符代表1字节=8个离散输入）", nameof(hexData));
            }
        }
        else if (startAddr >= 30001 && startAddr <= 39999) // 输入寄存器
        {
            if (hexData.Length % 4 != 0)
            {
                throw new ArgumentException("输入寄存器数据长度必须是4的倍数（每4个十六进制字符代表1个16位寄存器）", nameof(hexData));
            }
        }
        else if (startAddr >= 40001 && startAddr <= 49999) // 保持寄存器
        {
            if (hexData.Length % 4 != 0)
            {
                throw new ArgumentException("保持寄存器数据长度必须是4的倍数（每4个十六进制字符代表1个16位寄存器）", nameof(hexData));
            }
        }
        else
        {
            throw new ArgumentException("起始地址不在有效范围内（1-9999线圈，10001-19999离散输入，30001-39999输入寄存器，40001-49999保持寄存器）", nameof(startAddr));
        }
    }
}
