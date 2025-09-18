using ModbusSimulator.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace ModbusSimulator.Services;

public class CacheService : ICacheService
{
    private readonly IConnectionRepository _connectionRepository;
    private readonly IMemoryCache _cache;

    public CacheService(IConnectionRepository connectionRepository, IMemoryCache cache)
    {
        _connectionRepository = connectionRepository;
        _cache = cache;
    }

    public void ClearSlaveCache(int port, int slaveid)
    {
        // 直接使用端口和从机地址清除缓存，不依赖数据库查询
        try
        {
            string cacheKey = GetPortSlaveRegistersCacheKey(port, slaveid.ToString());
            _cache.Remove(cacheKey);
        }
        catch
        {
            // 如果清除缓存失败，忽略错误（缓存会自动过期）
        }
    }

    public void ClearConnectionCache(string connectionId)
    {
        // 清除连接下所有从机的缓存
        try
        {
            var connections = _connectionRepository.GetConnectionsTreeAsync().Result;
            var connection = connections.FirstOrDefault(c => c.Id == connectionId);
            if (connection != null)
            {
                // 清除该连接下所有从机的缓存
                foreach (var slave in connection.Slaves)
                {
                    string cacheKey = GetPortSlaveRegistersCacheKey(connection.Port, slave.Slaveid.ToString());
                    _cache.Remove(cacheKey);
                }
            }
        }
        catch
        {
            // 如果清除缓存失败，忽略错误（缓存会自动过期）
        }
    }

    private string GetPortSlaveRegistersCacheKey(int port, string slaveAddress)
    {
        return $"port_slave_registers_{port}_{slaveAddress}";
    }
}