using ModbusSimulator.Models;

namespace ModbusSimulator.Repositories;

public interface IConnectionRepository
{
    Task<Connection> CreateAsync(Connection connection);
    Task<Connection> UpdateAsync(Connection connection);
    Task DeleteAsync(string id);
    Task<IEnumerable<ConnectionTree>> GetConnectionsTreeAsync();
}