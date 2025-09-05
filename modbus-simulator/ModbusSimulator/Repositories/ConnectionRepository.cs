using System.Data;
using Dapper;
using ModbusSimulator.Models;

namespace ModbusSimulator.Repositories;

public class ConnectionRepository : IConnectionRepository
{
    private readonly IDbConnection _connection;
    
    public ConnectionRepository(IDbConnection connection)
    {
        _connection = connection;
    }
    
    public async Task<Connection> CreateAsync(Connection connection)
    {
        connection.Id = Guid.NewGuid().ToString("N");

        // 如果用户没有提供端口或端口为0，则自动分配端口
        if (connection.Port == 0)
        {
            connection.Port = await GetNextAvailablePortAsync();
        }

        const string sql = @"
            INSERT INTO connections (Id, Name, Port, ProtocolType)
            VALUES (@Id, @Name, @Port, @ProtocolType);";

        await _connection.ExecuteAsync(sql, connection);
        return connection;
    }
    
    private async Task<int> GetNextAvailablePortAsync()
    {
        const string sql = "SELECT MAX(Port) FROM connections";
        var maxPort = await _connection.QuerySingleOrDefaultAsync<int?>(sql);
        return maxPort.HasValue ? maxPort.Value + 1 : 502;
    }
    
    public async Task<Connection> UpdateAsync(Connection connection)
    {
        var existingCount = await _connection.QuerySingleAsync<int>(
            "SELECT COUNT(1) FROM connections WHERE Id = @Id", new { Id = connection.Id });
        if (existingCount == 0)
            throw new KeyNotFoundException("连接不存在");
        
        var portInUse = await _connection.QuerySingleAsync<int>(
            "SELECT COUNT(1) FROM connections WHERE Port = @Port AND Id != @Id", 
            new { Port = connection.Port, Id = connection.Id });
        if (portInUse > 0)
            throw new InvalidOperationException("端口已被使用");
        
        const string sql = @"
            UPDATE connections 
            SET Name = @Name, Port = @Port, ProtocolType = @ProtocolType 
            WHERE Id = @Id;";
        
        await _connection.ExecuteAsync(sql, connection);
        return connection;
    }
    
    public async Task DeleteAsync(string id)
    {
        const string sql = "DELETE FROM connections WHERE Id = @Id";
        var affected = await _connection.ExecuteAsync(sql, new { Id = id });
        if (affected == 0)
            throw new KeyNotFoundException("资源不存在");
    }
    
    public async Task<IEnumerable<ConnectionTree>> GetConnectionsTreeAsync()
    {
        const string sql = @"
            SELECT
                c.Id AS Id, c.Name AS Name, c.Port AS Port, c.ProtocolType AS ProtocolType,
                s.Id AS Id, s.ConnId AS ConnId, s.Name AS Name, s.SlaveId AS SlaveId
            FROM connections c
            LEFT JOIN slaves s ON c.Id = s.ConnId
            ORDER BY c.Name, s.SlaveId";
        
        var connectionDict = new Dictionary<string, ConnectionTree>();
        
        await _connection.QueryAsync<Connection, Slave, ConnectionTree>(
            sql,
            (connection, slave) =>
            {
                if (!connectionDict.TryGetValue(connection.Id, out var connectionTree))
                {
                    connectionTree = new ConnectionTree
                    {
                        Id = connection.Id,
                        Name = connection.Name,
                        Port = connection.Port,
                        ProtocolType = connection.ProtocolType,
                        Slaves = new List<Slave>()
                    };
                    connectionDict[connection.Id] = connectionTree;
                }
                
                if (slave != null && !string.IsNullOrEmpty(slave.Id))
                {
                    connectionTree.Slaves.Add(slave);
                }
                
                return connectionTree;
            },
            splitOn: "Id"
        );
        
        return connectionDict.Values;
    }
}