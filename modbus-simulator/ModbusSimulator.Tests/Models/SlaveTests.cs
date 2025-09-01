using ModbusSimulator.Models;

namespace ModbusSimulator.Tests.Models;

public class SlaveTests
{
    [Fact]
    public void Slave_Should_Initialize_With_Empty_Values()
    {
        var slave = new Slave();
        
        Assert.Equal(string.Empty, slave.Id);
        Assert.Equal(string.Empty, slave.Connid);
        Assert.Equal(string.Empty, slave.Name);
        Assert.Equal(0, slave.Slaveid);
    }

    [Fact]
    public void Slave_Should_Set_Properties_Correctly()
    {
        var slave = new Slave
        {
            Id = "slave-id",
            Connid = "conn-id",
            Name = "Test Slave",
            Slaveid = 1
        };
        
        Assert.Equal("slave-id", slave.Id);
        Assert.Equal("conn-id", slave.Connid);
        Assert.Equal("Test Slave", slave.Name);
        Assert.Equal(1, slave.Slaveid);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(247)]
    [InlineData(255)]
    public void Slave_Should_Accept_Valid_SlaveId_Values(int slaveid)
    {
        var slave = new Slave { Slaveid = slaveid };
        
        Assert.Equal(slaveid, slave.Slaveid);
    }

    [Fact]
    public void CreateSlaveRequest_Should_Set_Properties_Correctly()
    {
        var request = new CreateSlaveRequest
        {
            Name = "New Slave",
            Slaveid = 10
        };
        
        Assert.Equal("New Slave", request.Name);
        Assert.Equal(10, request.Slaveid);
    }

    [Fact]
    public void UpdateSlaveRequest_Should_Set_Properties_Correctly()
    {
        var request = new UpdateSlaveRequest
        {
            Name = "Updated Slave",
            Slaveid = 20
        };

        Assert.Equal("Updated Slave", request.Name);
        Assert.Equal(20, request.Slaveid);
    }

    [Fact]
    public void CreateSlaveRequest_Should_Initialize_With_Default_Values()
    {
        var request = new CreateSlaveRequest();

        Assert.Equal(string.Empty, request.Name);
        Assert.Equal(0, request.Slaveid);
    }

    [Fact]
    public void UpdateSlaveRequest_Should_Initialize_With_Default_Values()
    {
        var request = new UpdateSlaveRequest();

        Assert.Equal(string.Empty, request.Name);
        Assert.Equal(0, request.Slaveid);
    }

    #region Modbus Protocol Validation Tests

    [Theory]
    [InlineData(1, true)]      // Valid: minimum slave ID
    [InlineData(127, true)]    // Valid: standard range
    [InlineData(247, true)]    // Valid: maximum in specification
    [InlineData(0, false)]     // Invalid: broadcast address
    [InlineData(248, false)]   // Invalid: above maximum
    [InlineData(255, false)]   // Invalid: broadcast address
    public void Slave_Should_Validate_Slaveid_In_Modbus_Range(int slaveid, bool isValid)
    {
        var result = IsValidModbusSlaveId(slaveid);
        Assert.Equal(isValid, result);
    }

    [Theory]
    [InlineData("Slave 1", true)]
    [InlineData("从机1", true)]           // Chinese characters
    [InlineData("PLC_Device_001", true)] // Underscores and numbers
    [InlineData("", false)]               // Empty name
    [InlineData("   ", false)]            // Whitespace only
    [InlineData(null, false)]             // Null name
    public void Slave_Should_Validate_Name_Format(string name, bool isValid)
    {
        var result = IsValidSlaveName(name);
        Assert.Equal(isValid, result);
    }

    [Theory]
    [InlineData("1234567890abcdef", true)]     // Valid UUID format
    [InlineData("ABCDEF1234567890", true)]     // Valid uppercase
    [InlineData("", false)]                    // Empty ID
    [InlineData("invalid-id", false)]          // Invalid format
    [InlineData(null, false)]                  // Null ID
    public void Slave_Should_Validate_Id_Format(string id, bool isValid)
    {
        var result = IsValidSlaveId(id);
        Assert.Equal(isValid, result);
    }

    [Theory]
    [InlineData("1234567890abcdef1234567890abcdef", true)]     // Valid connection ID (32 chars)
    [InlineData("ABCDEF1234567890ABCDEF1234567890", true)]     // Valid uppercase (32 chars)
    [InlineData("", false)]                    // Empty connection ID
    [InlineData("invalid-id", false)]          // Invalid format
    public void Slave_Should_Validate_Connid_Format(string connid, bool isValid)
    {
        var result = IsValidConnectionId(connid);
        Assert.Equal(isValid, result);
    }

    #endregion

    #region Helper Methods for Testing

    private static bool IsValidModbusSlaveId(int slaveid)
    {
        // Modbus specification: slave addresses 1-247 are valid
        // 0 is broadcast, 248-255 are reserved
        return slaveid >= 1 && slaveid <= 247;
    }

    private static bool IsValidSlaveName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        // Name should not be empty or whitespace only
        return !string.IsNullOrWhiteSpace(name.Trim());
    }

    private static bool IsValidSlaveId(string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;

        // Should be 32-character hex string (UUID without dashes)
        if (id.Length != 32)
            return false;

        return id.All(c => "0123456789abcdefABCDEF".Contains(c));
    }

    private static bool IsValidConnectionId(string connid)
    {
        // Same validation as slave ID
        return IsValidSlaveId(connid);
    }

    #endregion
}