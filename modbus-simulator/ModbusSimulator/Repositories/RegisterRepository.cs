using System.Data;
using Dapper;
using ModbusSimulator.Models;

namespace ModbusSimulator.Repositories;

public class RegisterRepository : IRegisterRepository
{
    private readonly IDbConnection _connection;
    
    public RegisterRepository(IDbConnection connection)
    {
        _connection = connection;
    }
    
    public async Task<IEnumerable<Register>> GetBySlaveIdAsync(string slaveId)
    {
        const string sql = @"
            SELECT id, slaveid, startaddr, hexdata 
            FROM registers 
            WHERE slaveid = @SlaveId
            ORDER BY startaddr";
        
        return await _connection.QueryAsync<Register>(sql, new { SlaveId = slaveId });
    }
    
    public async Task<Register> CreateAsync(Register register)
    {
        register.Id = Guid.NewGuid().ToString("N");
        
        const string sql = @"
            INSERT INTO registers (id, slaveid, startaddr, hexdata) 
            VALUES (@Id, @Slaveid, @Startaddr, @Hexdata);";
        
        await _connection.ExecuteAsync(sql, register);
        return register;
    }
    
    public async Task<Register> UpdateAsync(Register register)
    {
        var existingCount = await _connection.QuerySingleAsync<int>(
            "SELECT COUNT(1) FROM registers WHERE id = @Id", new { Id = register.Id });
        if (existingCount == 0)
            throw new KeyNotFoundException("寄存器不存在");
        
        const string sql = @"
            UPDATE registers 
            SET startaddr = @Startaddr, hexdata = @Hexdata
            WHERE id = @Id;";
        
        await _connection.ExecuteAsync(sql, register);
        return register;
    }
    
    public async Task<bool> DeleteAsync(string id)
    {
        const string sql = "DELETE FROM registers WHERE id = @Id";
        var affected = await _connection.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }
}