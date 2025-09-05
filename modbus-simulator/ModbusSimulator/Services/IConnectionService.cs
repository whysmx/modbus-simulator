using ModbusSimulator.Models;

namespace ModbusSimulator.Services;

public interface IConnectionService
{
    Task<IEnumerable<ConnectionTree>> GetConnectionsTreeAsync();
    Task<Connection> GetConnectionByIdAsync(string id);
    Task<Connection> CreateConnectionAsync(CreateConnectionRequest request);
    Task<Connection> UpdateConnectionAsync(string id, UpdateConnectionRequest request);
    Task DeleteConnectionAsync(string id);
}
