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
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL UNIQUE,
                port INTEGER NOT NULL UNIQUE
            );";

        var createSlavesTable = @"
            CREATE TABLE IF NOT EXISTS slaves (
                id TEXT PRIMARY KEY,
                connid TEXT NOT NULL,
                name TEXT NOT NULL,
                slaveid INTEGER NOT NULL,
                
                FOREIGN KEY (connid) REFERENCES connections(id) ON DELETE CASCADE,
                UNIQUE(connid, slaveid),
                UNIQUE(connid, name)
            );";

        var createRegistersTable = @"
            CREATE TABLE IF NOT EXISTS registers (
                id TEXT PRIMARY KEY,
                slaveid TEXT NOT NULL,
                startaddr INTEGER NOT NULL,
                hexdata TEXT NOT NULL,
                
                FOREIGN KEY (slaveid) REFERENCES slaves(id) ON DELETE CASCADE,
                UNIQUE(slaveid, startaddr)
            );";

        var createIndexes = @"
            CREATE INDEX IF NOT EXISTS idx_slaves_conn ON slaves(connid);
            CREATE INDEX IF NOT EXISTS idx_registers_slave ON registers(slaveid);
            CREATE INDEX IF NOT EXISTS idx_registers_addr ON registers(slaveid, startaddr);";

        await connection.ExecuteAsync(createConnectionsTable);
        await connection.ExecuteAsync(createSlavesTable);
        await connection.ExecuteAsync(createRegistersTable);
        await connection.ExecuteAsync(createIndexes);
    }
}