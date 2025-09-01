using System.Data;
using Dapper;
using ModbusSimulator.Models;

namespace ModbusSimulator.Repositories;

public class SlaveRepository : ISlaveRepository
{
    private readonly IDbConnection _connection;
    
    public SlaveRepository(IDbConnection connection)
    {
        _connection = connection;
    }
    
    public async Task<Slave> CreateAsync(Slave slave)
    {
        slave.Id = Guid.NewGuid().ToString("N");
        
        const string sql = @"
            INSERT INTO slaves (id, connid, name, slaveid) 
            VALUES (@Id, @Connid, @Name, @Slaveid);";
        
        await _connection.ExecuteAsync(sql, slave);
        return slave;
    }
    
    public async Task<Slave> UpdateAsync(Slave slave)
    {
        var existingCount = await _connection.QuerySingleAsync<int>(
            "SELECT COUNT(1) FROM slaves WHERE id = @Id", new { Id = slave.Id });
        if (existingCount == 0)
            throw new KeyNotFoundException("从机不存在");
        
        const string sql = @"
            UPDATE slaves 
            SET name = @Name, slaveid = @Slaveid
            WHERE id = @Id;";
        
        await _connection.ExecuteAsync(sql, slave);
        return slave;
    }
    
    public async Task DeleteAsync(string id)
    {
        const string sql = "DELETE FROM slaves WHERE id = @Id";
        var affected = await _connection.ExecuteAsync(sql, new { Id = id });
        if (affected == 0)
            throw new KeyNotFoundException("资源不存在");
    }
}