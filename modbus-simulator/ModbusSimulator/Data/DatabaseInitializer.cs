using System.Data;
using Dapper;

namespace ModbusSimulator.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IDbConnection connection)
    {
        await CreateTablesAsync(connection);
    }

    private static async Task CreateTablesAsync(IDbConnection connection)
    {
        var createConnectionsTable = @"
            CREATE TABLE IF NOT EXISTS connections (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL UNIQUE,
                Port INTEGER NOT NULL UNIQUE,
                ProtocolType INTEGER NOT NULL DEFAULT 0
            );";

        var createSlavesTable = @"
            CREATE TABLE IF NOT EXISTS slaves (
                Id TEXT PRIMARY KEY,
                ConnId TEXT NOT NULL,
                Name TEXT NOT NULL,
                SlaveId INTEGER NOT NULL,
                
                FOREIGN KEY (ConnId) REFERENCES connections(Id) ON DELETE CASCADE,
                UNIQUE(ConnId, SlaveId),
                UNIQUE(ConnId, Name)
            );";

        var createRegistersTable = @"
            CREATE TABLE IF NOT EXISTS registers (
                Id TEXT PRIMARY KEY,
                SlaveId TEXT NOT NULL,
                StartAddr INTEGER NOT NULL,
                HexData TEXT NOT NULL,
                
                FOREIGN KEY (SlaveId) REFERENCES slaves(Id) ON DELETE CASCADE,
                UNIQUE(SlaveId, StartAddr)
            );";

        var createIndexes = @"
            CREATE INDEX IF NOT EXISTS idx_slaves_conn ON slaves(ConnId);
            CREATE INDEX IF NOT EXISTS idx_registers_slave ON registers(SlaveId);
            CREATE INDEX IF NOT EXISTS idx_registers_addr ON registers(SlaveId, StartAddr);";

        await connection.ExecuteAsync(createConnectionsTable);
        await connection.ExecuteAsync(createSlavesTable);
        await connection.ExecuteAsync(createRegistersTable);
        await connection.ExecuteAsync(createIndexes);
    }
}