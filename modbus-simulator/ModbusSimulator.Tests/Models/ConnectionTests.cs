using ModbusSimulator.Models;

namespace ModbusSimulator.Tests.Models;

public class ConnectionTests
{
    [Fact]
    public void Connection_Should_Initialize_With_Empty_Values()
    {
        var connection = new Connection();
        
        Assert.Equal(string.Empty, connection.Id);
        Assert.Equal(string.Empty, connection.Name);
        Assert.Equal(0, connection.Port);
    }

    [Fact]
    public void Connection_Should_Set_Properties_Correctly()
    {
        var connection = new Connection
        {
            Id = "test-id",
            Name = "Test Connection",
            Port = 502
        };
        
        Assert.Equal("test-id", connection.Id);
        Assert.Equal("Test Connection", connection.Name);
        Assert.Equal(502, connection.Port);
    }

    [Fact]
    public void ConnectionTree_Should_Initialize_With_Empty_Slaves_List()
    {
        var connectionTree = new ConnectionTree();
        
        Assert.NotNull(connectionTree.Slaves);
        Assert.Empty(connectionTree.Slaves);
    }

    [Fact]
    public void CreateConnectionRequest_Should_Initialize_With_Empty_Name()
    {
        var request = new CreateConnectionRequest();
        
        Assert.Equal(string.Empty, request.Name);
    }

    [Fact]
    public void UpdateConnectionRequest_Should_Set_Properties_Correctly()
    {
        var request = new UpdateConnectionRequest
        {
            Name = "Updated Connection",
            Port = 1502
        };
        
        Assert.Equal("Updated Connection", request.Name);
        Assert.Equal(1502, request.Port);
    }
}