using ModbusSimulator.Models;
using ModbusSimulator.Repositories;

namespace ModbusSimulator.Services;

public class SlaveService : ISlaveService
{
    private readonly ISlaveRepository _slaveRepository;
    private readonly IConnectionRepository _connectionRepository;

    public SlaveService(ISlaveRepository slaveRepository, IConnectionRepository connectionRepository)
    {
        _slaveRepository = slaveRepository;
        _connectionRepository = connectionRepository;
    }

    public async Task<Slave> CreateSlaveAsync(string connectionId, CreateSlaveRequest request)
    {
        // 验证连接ID
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            throw new ArgumentException("连接ID不能为空", nameof(connectionId));
        }

        // 验证连接是否存在
        var connections = await _connectionRepository.GetConnectionsTreeAsync();
        var connection = connections.FirstOrDefault(c => c.Id == connectionId);
        if (connection == null)
        {
            throw new KeyNotFoundException("连接不存在");
        }

        // 业务验证
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("从机名称不能为空", "request.Name");
        }

        if (request.Name.Length > 100)
        {
            throw new ArgumentException("从机名称长度不能超过100个字符", "request.Name");
        }

        if (request.Slaveid < 1 || request.Slaveid > 247)
        {
            throw new ArgumentException("从机地址必须在1-247之间", "request.Slaveid");
        }

        var slave = new Slave
        {
            Connid = connectionId,
            Name = request.Name.Trim(),
            Slaveid = request.Slaveid
        };

        try
        {
            return await _slaveRepository.CreateAsync(slave);
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("UNIQUE constraint failed"))
        {
            if (ex.Message.Contains("slaveid"))
            {
                throw new InvalidOperationException("该连接下已存在相同地址的从机", ex);
            }
            else if (ex.Message.Contains("name"))
            {
                throw new InvalidOperationException("该连接下已存在相同名称的从机", ex);
            }
            throw;
        }
    }

    public async Task<Slave> UpdateSlaveAsync(string connectionId, string slaveId, UpdateSlaveRequest request)
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

        // 验证连接是否存在
        var connections = await _connectionRepository.GetConnectionsTreeAsync();
        var connection = connections.FirstOrDefault(c => c.Id == connectionId);
        if (connection == null)
        {
            throw new KeyNotFoundException("连接不存在");
        }

        // 业务验证
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("从机名称不能为空", "request.Name");
        }

        if (request.Name.Length > 100)
        {
            throw new ArgumentException("从机名称长度不能超过100个字符", "request.Name");
        }

        if (request.Slaveid < 1 || request.Slaveid > 247)
        {
            throw new ArgumentException("从机地址必须在1-247之间", "request.Slaveid");
        }

        var slave = new Slave
        {
            Id = slaveId,
            Connid = connectionId,
            Name = request.Name.Trim(),
            Slaveid = request.Slaveid
        };

        try
        {
            return await _slaveRepository.UpdateAsync(slave);
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("UNIQUE constraint failed"))
        {
            if (ex.Message.Contains("slaveid"))
            {
                throw new InvalidOperationException("该连接下已存在相同地址的从机", ex);
            }
            else if (ex.Message.Contains("name"))
            {
                throw new InvalidOperationException("该连接下已存在相同名称的从机", ex);
            }
            throw;
        }
    }

    public async Task DeleteSlaveAsync(string connectionId, string slaveId)
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

        // 验证连接是否存在
        var connections = await _connectionRepository.GetConnectionsTreeAsync();
        var connection = connections.FirstOrDefault(c => c.Id == connectionId);
        if (connection == null)
        {
            throw new KeyNotFoundException("连接不存在");
        }

        await _slaveRepository.DeleteAsync(slaveId);
    }
}
