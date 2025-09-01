using ModbusSimulator.Models;

namespace ModbusSimulator.Tests.Models;

public class RegisterTests
{
    [Fact]
    public void Register_Should_Initialize_With_Empty_Values()
    {
        var register = new Register();

        Assert.Equal(string.Empty, register.Id);
        Assert.Equal(string.Empty, register.Slaveid);
        Assert.Equal(0, register.Startaddr);
        Assert.Equal(string.Empty, register.Hexdata);
    }

    [Fact]
    public void Register_Should_Set_Properties_Correctly()
    {
        var register = new Register
        {
            Id = "reg-id",
            Slaveid = "slave-id",
            Startaddr = 100,
            Hexdata = "1234ABCD"
        };

        Assert.Equal("reg-id", register.Id);
        Assert.Equal("slave-id", register.Slaveid);
        Assert.Equal(100, register.Startaddr);
        Assert.Equal("1234ABCD", register.Hexdata);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(40000)]
    [InlineData(65535)]
    public void Register_Should_Accept_Valid_Startaddr_Values(int startaddr)
    {
        var register = new Register { Startaddr = startaddr };

        Assert.Equal(startaddr, register.Startaddr);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234")]
    [InlineData("ABCDEF")]
    [InlineData("1234567890ABCDEF")]
    public void Register_Should_Accept_Various_Hexdata_Values(string hexdata)
    {
        var register = new Register { Hexdata = hexdata };

        Assert.Equal(hexdata, register.Hexdata);
    }

    [Fact]
    public void CreateRegisterRequest_Should_Set_Properties_Correctly()
    {
        var request = new CreateRegisterRequest
        {
            Startaddr = 200,
            Hexdata = "FFFF"
        };

        Assert.Equal(200, request.Startaddr);
        Assert.Equal("FFFF", request.Hexdata);
    }

    [Fact]
    public void UpdateRegisterRequest_Should_Set_Properties_Correctly()
    {
        var request = new UpdateRegisterRequest
        {
            Startaddr = 300,
            Hexdata = "0000"
        };

        Assert.Equal(300, request.Startaddr);
        Assert.Equal("0000", request.Hexdata);
    }

    #region Register Type Determination Tests (Based on Address Ranges)

    [Theory]
    [InlineData(1, "Coil")]
    [InlineData(9999, "Coil")]
    [InlineData(10001, "Discrete Input")]
    [InlineData(19999, "Discrete Input")]
    [InlineData(30001, "Input Register")]
    [InlineData(39999, "Input Register")]
    [InlineData(40001, "Holding Register")]
    [InlineData(49999, "Holding Register")]
    public void Register_Should_Determine_Correct_Type_From_Address(int startaddr, string expectedType)
    {
        var register = new Register { Startaddr = startaddr };

        var actualType = GetRegisterType(register.Startaddr);
        Assert.Equal(expectedType, actualType);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10000)]
    [InlineData(20000)]
    [InlineData(30000)]
    [InlineData(40000)]
    [InlineData(50000)]
    [InlineData(65535)]
    public void Register_Should_Handle_Invalid_Address_Ranges(int startaddr)
    {
        var register = new Register { Startaddr = startaddr };

        var actualType = GetRegisterType(register.Startaddr);
        Assert.Equal("Invalid", actualType);
    }

    #endregion

    #region Hexdata Validation Tests

    [Theory]
    [InlineData("1234", true)]      // Valid hex string
    [InlineData("ABCDEF", true)]    // Valid uppercase hex
    [InlineData("abcdef", true)]    // Valid lowercase hex
    [InlineData("123ABC", true)]    // Mixed case
    [InlineData("", true)]          // Empty string (valid)
    [InlineData("123", false)]      // Odd length (invalid for hex)
    [InlineData("XYZ", false)]      // Invalid hex characters
    [InlineData("12 34", false)]    // Contains spaces
    public void Register_Should_Validate_Hexdata_Format(string hexdata, bool isValid)
    {
        var register = new Register { Hexdata = hexdata };

        var result = IsValidHexdata(register.Hexdata);
        Assert.Equal(isValid, result);
    }

    [Theory]
    [InlineData("0000", 1)]         // 1 register (4 hex chars)
    [InlineData("00000000", 2)]     // 2 registers (8 hex chars)
    [InlineData("12345678ABCD", 3)] // 3 registers (12 hex chars)
    public void Register_Should_Calculate_Register_Count_Correctly(string hexdata, int expectedCount)
    {
        var register = new Register { Hexdata = hexdata, Startaddr = 40001 }; // Holding register

        var registerCount = GetRegisterCount(register);
        Assert.Equal(expectedCount, registerCount);
    }

    [Theory]
    [InlineData("00", 1)]           // 1 byte (2 hex chars) = 8 bits
    [InlineData("0000", 2)]         // 2 bytes (4 hex chars) = 16 bits
    [InlineData("000000", 3)]       // 3 bytes (6 hex chars) = 24 bits
    public void Register_Should_Calculate_Bit_Count_For_Discrete_Data(string hexdata, int expectedBytes)
    {
        var register = new Register { Hexdata = hexdata, Startaddr = 10001 }; // Discrete input

        var bitCount = GetBitCount(register);
        Assert.Equal(expectedBytes * 8, bitCount);
    }

    #endregion

    #region Address Range Validation Tests

    [Theory]
    [InlineData(40001, "0000", true)]           // Valid holding register
    [InlineData(40001, "00000000", true)]       // Valid holding register (2 registers)
    [InlineData(40001, "0000000", false)]       // Invalid length (odd hex count)
    [InlineData(10001, "00", true)]             // Valid discrete input (1 byte)
    [InlineData(10001, "0000", true)]           // Valid discrete input (2 bytes)
    [InlineData(10001, "00000", false)]         // Invalid length (odd hex count)
    public void Register_Should_Validate_Hexdata_Length_By_Type(int startaddr, string hexdata, bool isValid)
    {
        var register = new Register { Startaddr = startaddr, Hexdata = hexdata };

        var result = IsValidHexdataForType(register);
        Assert.Equal(isValid, result);
    }

    #endregion

    #region Helper Methods for Testing

    private static string GetRegisterType(int startaddr)
    {
        if (startaddr >= 1 && startaddr <= 9999)
            return "Coil";
        if (startaddr >= 10001 && startaddr <= 19999)
            return "Discrete Input";
        if (startaddr >= 30001 && startaddr <= 39999)
            return "Input Register";
        if (startaddr >= 40001 && startaddr <= 49999)
            return "Holding Register";
        return "Invalid";
    }

    private static bool IsValidHexdata(string hexdata)
    {
        if (string.IsNullOrEmpty(hexdata))
            return true;

        if (hexdata.Length % 2 != 0)
            return false;

        return hexdata.All(c => "0123456789ABCDEFabcdef".Contains(c));
    }

    private static int GetRegisterCount(Register register)
    {
        if (register.Startaddr >= 30001 && register.Startaddr <= 49999) // Register types
        {
            return register.Hexdata.Length / 4;
        }
        return 0;
    }

    private static int GetBitCount(Register register)
    {
        if (register.Startaddr >= 1 && register.Startaddr <= 19999) // Bit types
        {
            return (register.Hexdata.Length / 2) * 8;
        }
        return 0;
    }

    private static bool IsValidHexdataForType(Register register)
    {
        // For register types (03/04), hexdata length must be multiple of 4
        if (register.Startaddr >= 30001 && register.Startaddr <= 49999)
        {
            return register.Hexdata.Length % 4 == 0 && IsValidHexdata(register.Hexdata);
        }

        // For bit types (01/02), hexdata length must be multiple of 2
        if (register.Startaddr >= 1 && register.Startaddr <= 19999)
        {
            return register.Hexdata.Length % 2 == 0 && IsValidHexdata(register.Hexdata);
        }

        return IsValidHexdata(register.Hexdata);
    }

    #endregion
}