using ModbusSimulator.Models;
using ModbusSimulator.Repositories;

namespace ModbusSimulator.Services;

public class ConnectionService : IConnectionService
{
    private readonly IConnectionRepository _connectionRepository;
    private readonly ICacheService _cacheService;

    public ConnectionService(IConnectionRepository connectionRepository, ICacheService cacheService)
    {
        _connectionRepository = connectionRepository;
        _cacheService = cacheService;
    }

    public async Task<IEnumerable<ConnectionTree>> GetConnectionsTreeAsync()
    {
        return await _connectionRepository.GetConnectionsTreeAsync();
    }

    public async Task<Connection> GetConnectionByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("连接ID不能为空", nameof(id));
        }

        var connections = await _connectionRepository.GetConnectionsTreeAsync();
        var connection = connections.FirstOrDefault(c => c.Id == id);
        
        if (connection == null)
        {
            throw new KeyNotFoundException("连接不存在");
        }

        return new Connection
        {
            Id = connection.Id,
            Name = connection.Name,
            Port = connection.Port,
            ProtocolType = connection.ProtocolType
        };
    }

    public async Task<Connection> CreateConnectionAsync(CreateConnectionRequest request)
    {
        // 业务验证
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("连接名称不能为空", "request.Name");
        }

        if (request.Name.Length > 100)
        {
            throw new ArgumentException("连接名称长度不能超过100个字符", "request.Name");
        }

        var connection = new Connection
        {
            Name = request.Name.Trim(),
            ProtocolType = request.ProtocolType
        };

        try
        {
            return await _connectionRepository.CreateAsync(connection);
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("UNIQUE constraint failed"))
        {
            throw new InvalidOperationException("连接名称已存在", ex);
        }
    }

    public async Task<Connection> UpdateConnectionAsync(string id, UpdateConnectionRequest request)
    {
        // 验证ID格式
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("连接ID不能为空", nameof(id));
        }

        // 业务验证
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("连接名称不能为空", "request.Name");
        }

        if (request.Name.Length > 100)
        {
            throw new ArgumentException("连接名称长度不能超过100个字符", "request.Name");
        }

        if (request.Port < 1 || request.Port > 65535)
        {
            throw new ArgumentException("端口号必须在1-65535之间", nameof(request.Port));
        }

        var connection = new Connection
        {
            Id = id,
            Name = request.Name.Trim(),
            Port = request.Port,
            ProtocolType = request.ProtocolType
        };

        try
        {
            return await _connectionRepository.UpdateAsync(connection);
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("UNIQUE constraint failed"))
        {
            if (ex.Message.Contains("name"))
            {
                throw new InvalidOperationException("连接名称已存在", ex);
            }
            else if (ex.Message.Contains("port"))
            {
                throw new InvalidOperationException("端口已被使用", ex);
            }
            throw;
        }
    }

    public async Task DeleteConnectionAsync(string id)
    {
        // 验证ID格式
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("连接ID不能为空", nameof(id));
        }

        // 清除相关的寄存器缓存
        _cacheService.ClearConnectionCache(id);
        
        await _connectionRepository.DeleteAsync(id);
    }
}
