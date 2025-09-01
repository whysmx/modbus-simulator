using ModbusSimulator.Services;
using Xunit;

namespace ModbusSimulator.Tests.Validation;

public class DataValidationTests
{
    #region Hex String Validation Tests

    [Theory]
    [InlineData("1234ABCD", true)]
    [InlineData("abcdef12", true)]
    [InlineData("ABCDEF12", true)]
    [InlineData("12ab34cd", true)]
    [InlineData("aBcD1234", true)]
    [InlineData("", false)]
    [InlineData("XYZ", false)]
    [InlineData("123", false)]      // Odd length
    [InlineData("12 34", false)]    // Contains space
    [InlineData("12@34", false)]    // Invalid character
    [InlineData("12G34", false)]    // Invalid character 'G'
    [InlineData("12g34", false)]    // Lowercase 'g' is valid, wait this should be true
    public void IsValidHexString_ValidatesCorrectly(string hexString, bool expected)
    {
        // Act
        var result = RegisterService.IsValidHexString(hexString);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsValidHexString_AllValidHexCharacters_AreAccepted()
    {
        // Arrange - All valid hexadecimal characters
        var validChars = "0123456789ABCDEFabcdef";

        foreach (var c in validChars)
        {
            var hexString = $"{c}{c}"; // Two identical characters

            // Act
            var result = RegisterService.IsValidHexString(hexString);

            // Assert
            Assert.True(result, $"Character '{c}' should be valid in hex string");
        }
    }

    [Fact]
    public void IsValidHexString_InvalidCharacters_AreRejected()
    {
        // Arrange - Invalid characters for hex
        var invalidChars = "GHIJKLMNOPQRSTUVWXYZghijklmnopqrstuvwxyz!@#$%^&*()";

        foreach (var c in invalidChars)
        {
            var hexString = $"12{c}34";

            // Act
            var result = RegisterService.IsValidHexString(hexString);

            // Assert
            Assert.False(result, $"Character '{c}' should be invalid in hex string");
        }
    }

    #endregion

    #region Hex Data Length Validation Tests

    [Theory]
    [InlineData(40001, "ABCD", true)]         // Holding register: 4 chars valid
    [InlineData(40001, "ABCD1234", true)]     // Holding register: 8 chars valid
    [InlineData(40001, "ABCD12345678", true)] // Holding register: 12 chars valid
    [InlineData(40001, "ABC", false)]         // Holding register: 3 chars invalid
    [InlineData(40001, "ABCDE", false)]       // Holding register: 5 chars invalid
    public void ValidateHexDataLength_HoldingRegisters_ValidatesCorrectly(int startAddr, string hexData, bool shouldPass)
    {
        // Act & Assert
        if (shouldPass)
        {
            // Should not throw
            RegisterService.ValidateHexDataLength(startAddr, hexData);
        }
        else
        {
            // Should throw
            Assert.Throws<ArgumentException>(() =>
                RegisterService.ValidateHexDataLength(startAddr, hexData));
        }
    }

    [Theory]
    [InlineData(30001, "ABCD", true)]         // Input register: 4 chars valid
    [InlineData(30001, "ABCD1234", true)]     // Input register: 8 chars valid
    [InlineData(30001, "ABC", false)]         // Input register: 3 chars invalid
    [InlineData(30001, "ABCDE", false)]       // Input register: 5 chars invalid
    public void ValidateHexDataLength_InputRegisters_ValidatesCorrectly(int startAddr, string hexData, bool shouldPass)
    {
        // Act & Assert
        if (shouldPass)
        {
            // Should not throw
            RegisterService.ValidateHexDataLength(startAddr, hexData);
        }
        else
        {
            // Should throw
            Assert.Throws<ArgumentException>(() =>
                RegisterService.ValidateHexDataLength(startAddr, hexData));
        }
    }

    [Theory]
    [InlineData(1, "AB", true)]               // Coil: 2 chars valid
    [InlineData(1, "ABCD", true)]             // Coil: 4 chars valid
    [InlineData(1, "ABCDEF", true)]           // Coil: 6 chars valid
    [InlineData(1, "A", false)]               // Coil: 1 char invalid
    [InlineData(1, "ABC", false)]             // Coil: 3 chars invalid
    [InlineData(1, "ABCDE", false)]           // Coil: 5 chars invalid
    public void ValidateHexDataLength_Coils_ValidatesCorrectly(int startAddr, string hexData, bool shouldPass)
    {
        // Act & Assert
        if (shouldPass)
        {
            // Should not throw
            RegisterService.ValidateHexDataLength(startAddr, hexData);
        }
        else
        {
            // Should throw
            Assert.Throws<ArgumentException>(() =>
                RegisterService.ValidateHexDataLength(startAddr, hexData));
        }
    }

    [Theory]
    [InlineData(10001, "AB", true)]           // Discrete input: 2 chars valid
    [InlineData(10001, "ABCD", true)]         // Discrete input: 4 chars valid
    [InlineData(10001, "A", false)]           // Discrete input: 1 char invalid
    [InlineData(10001, "ABC", false)]         // Discrete input: 3 chars invalid
    public void ValidateHexDataLength_DiscreteInputs_ValidatesCorrectly(int startAddr, string hexData, bool shouldPass)
    {
        // Act & Assert
        if (shouldPass)
        {
            // Should not throw
            RegisterService.ValidateHexDataLength(startAddr, hexData);
        }
        else
        {
            // Should throw
            Assert.Throws<ArgumentException>(() =>
                RegisterService.ValidateHexDataLength(startAddr, hexData));
        }
    }

    [Theory]
    [InlineData(0, "ABCD")]       // Invalid: below range
    [InlineData(10000, "ABCD")]   // Invalid: between coil and discrete input
    [InlineData(20000, "ABCD")]   // Invalid: between discrete input and input register
    [InlineData(30000, "ABCD")]   // Invalid: between input register and holding register
    [InlineData(50000, "ABCD")]   // Invalid: above holding register range
    [InlineData(65535, "ABCD")]   // Invalid: max value
    public void ValidateHexDataLength_InvalidAddressRanges_ThrowException(int startAddr, string hexData)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            RegisterService.ValidateHexDataLength(startAddr, hexData));

        Assert.Contains("起始地址不在有效范围内", exception.Message);
    }

    #endregion

    #region Modbus Address Range Validation Tests

    [Theory]
    [InlineData(1, true)]          // Coil: minimum valid
    [InlineData(9999, true)]       // Coil: maximum valid
    [InlineData(0, false)]         // Invalid: below minimum
    [InlineData(10000, false)]     // Invalid: gap between ranges
    public void ValidateHexDataLength_CoilAddressRange_IsCorrect(int startAddr, bool shouldPass)
    {
        // Arrange
        var hexData = "ABCD"; // Valid length for registers

        // Act & Assert
        if (shouldPass)
        {
            // Should not throw for coils with even length
            RegisterService.ValidateHexDataLength(startAddr, "AB");
        }
        else
        {
            // Should throw for invalid address
            var exception = Assert.Throws<ArgumentException>(() =>
                RegisterService.ValidateHexDataLength(startAddr, hexData));
            Assert.Contains("起始地址不在有效范围内", exception.Message);
        }
    }

    [Theory]
    [InlineData(10001, true)]      // Discrete input: minimum valid
    [InlineData(19999, true)]      // Discrete input: maximum valid
    [InlineData(10000, false)]     // Invalid: below minimum
    [InlineData(20000, false)]     // Invalid: above maximum
    public void ValidateHexDataLength_DiscreteInputAddressRange_IsCorrect(int startAddr, bool shouldPass)
    {
        // Arrange
        var hexData = "AB"; // Valid length for discrete inputs

        // Act & Assert
        if (shouldPass)
        {
            // Should not throw for discrete inputs with even length
            RegisterService.ValidateHexDataLength(startAddr, hexData);
        }
        else
        {
            // Should throw for invalid address
            var exception = Assert.Throws<ArgumentException>(() =>
                RegisterService.ValidateHexDataLength(startAddr, hexData));
            Assert.Contains("起始地址不在有效范围内", exception.Message);
        }
    }

    [Theory]
    [InlineData(30001, true)]      // Input register: minimum valid
    [InlineData(39999, true)]      // Input register: maximum valid
    [InlineData(30000, false)]     // Invalid: below minimum
    [InlineData(40000, false)]     // Invalid: above maximum (but valid for holding)
    public void ValidateHexDataLength_InputRegisterAddressRange_IsCorrect(int startAddr, bool shouldPass)
    {
        // Arrange
        var hexData = "ABCD"; // Valid length for input registers

        // Act & Assert
        if (shouldPass)
        {
            // Should not throw for input registers with multiple of 4 length
            RegisterService.ValidateHexDataLength(startAddr, hexData);
        }
        else
        {
            // Should throw for invalid address
            var exception = Assert.Throws<ArgumentException>(() =>
                RegisterService.ValidateHexDataLength(startAddr, hexData));
            Assert.Contains("起始地址不在有效范围内", exception.Message);
        }
    }

    [Theory]
    [InlineData(40001, true)]      // Holding register: minimum valid
    [InlineData(49999, true)]      // Holding register: maximum valid
    [InlineData(40000, false)]     // Invalid: below minimum
    [InlineData(50000, false)]     // Invalid: above maximum
    public void ValidateHexDataLength_HoldingRegisterAddressRange_IsCorrect(int startAddr, bool shouldPass)
    {
        // Arrange
        var hexData = "ABCD"; // Valid length for holding registers

        // Act & Assert
        if (shouldPass)
        {
            // Should not throw for holding registers with multiple of 4 length
            RegisterService.ValidateHexDataLength(startAddr, hexData);
        }
        else
        {
            // Should throw for invalid address
            var exception = Assert.Throws<ArgumentException>(() =>
                RegisterService.ValidateHexDataLength(startAddr, hexData));
            Assert.Contains("起始地址不在有效范围内", exception.Message);
        }
    }

    #endregion

    #region Error Message Validation Tests

    [Fact]
    public void ValidateHexDataLength_InvalidCoilLength_ProvidesCorrectErrorMessage()
    {
        // Arrange
        var startAddr = 1;
        var hexData = "ABC"; // Odd length - invalid for coils

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            RegisterService.ValidateHexDataLength(startAddr, hexData));

        Assert.Contains("线圈数据长度必须是2的倍数", exception.Message);
        Assert.Equal("hexData", exception.ParamName);
    }

    [Fact]
    public void ValidateHexDataLength_InvalidDiscreteInputLength_ProvidesCorrectErrorMessage()
    {
        // Arrange
        var startAddr = 10001;
        var hexData = "ABC"; // Odd length - invalid for discrete inputs

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            RegisterService.ValidateHexDataLength(startAddr, hexData));

        Assert.Contains("离散输入数据长度必须是2的倍数", exception.Message);
        Assert.Equal("hexData", exception.ParamName);
    }

    [Fact]
    public void ValidateHexDataLength_InvalidRegisterLength_ProvidesCorrectErrorMessage()
    {
        // Arrange
        var startAddr = 40001; // Holding register
        var hexData = "ABC"; // Length not multiple of 4 - invalid for registers

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            RegisterService.ValidateHexDataLength(startAddr, hexData));

        Assert.Contains("保持寄存器数据长度必须是4的倍数", exception.Message);
        Assert.Equal("hexData", exception.ParamName);
    }

    [Fact]
    public void ValidateHexDataLength_InvalidAddressRange_ProvidesCorrectErrorMessage()
    {
        // Arrange
        var startAddr = 50000; // Invalid address range
        var hexData = "ABCD";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            RegisterService.ValidateHexDataLength(startAddr, hexData));

        Assert.Contains("起始地址不在有效范围内", exception.Message);
        Assert.Contains("1-9999线圈", exception.Message);
        Assert.Contains("10001-19999离散输入", exception.Message);
        Assert.Contains("30001-39999输入寄存器", exception.Message);
        Assert.Contains("40001-49999保持寄存器", exception.Message);
        Assert.Equal("startAddr", exception.ParamName);
    }

    #endregion

    #region Boundary Condition Tests

    [Fact]
    public void ValidateHexDataLength_MaximumValidLengths_AreAccepted()
    {
        // Test maximum reasonable lengths for each type

        // Coils: Very large even number
        RegisterService.ValidateHexDataLength(1, new string('A', 1000));

        // Discrete inputs: Very large even number
        RegisterService.ValidateHexDataLength(10001, new string('B', 1000));

        // Input registers: Very large multiple of 4
        RegisterService.ValidateHexDataLength(30001, new string('C', 1000));

        // Holding registers: Very large multiple of 4
        RegisterService.ValidateHexDataLength(40001, new string('D', 1000));
    }

    [Fact]
    public void ValidateHexDataLength_MinimumValidLengths_AreAccepted()
    {
        // Test minimum valid lengths for each type

        // Coils: 2 characters minimum
        RegisterService.ValidateHexDataLength(1, "AB");

        // Discrete inputs: 2 characters minimum
        RegisterService.ValidateHexDataLength(10001, "CD");

        // Input registers: 4 characters minimum
        RegisterService.ValidateHexDataLength(30001, "EF12");

        // Holding registers: 4 characters minimum
        RegisterService.ValidateHexDataLength(40001, "3456");
    }

    [Theory]
    [InlineData(1, "")]        // Empty string for coils
    [InlineData(10001, "")]    // Empty string for discrete inputs
    [InlineData(30001, "")]    // Empty string for input registers
    [InlineData(40001, "")]    // Empty string for holding registers
    public void ValidateHexDataLength_EmptyString_ThrowsException(int startAddr, string hexData)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            RegisterService.ValidateHexDataLength(startAddr, hexData));
    }

    #endregion

    #region Hex String Case Sensitivity Tests

    [Fact]
    public void IsValidHexString_CaseInsensitive_AcceptsBothCases()
    {
        // Arrange - Mix of uppercase and lowercase
        var mixedCase = "AbCdEf12";

        // Act
        var result = RegisterService.IsValidHexString(mixedCase);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidHexString_UppercaseOnly_IsValid()
    {
        // Arrange
        var uppercase = "ABCDEF1234567890";

        // Act
        var result = RegisterService.IsValidHexString(uppercase);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidHexString_LowercaseOnly_IsValid()
    {
        // Arrange
        var lowercase = "abcdef1234567890";

        // Act
        var result = RegisterService.IsValidHexString(lowercase);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region Comprehensive Validation Tests

    [Fact]
    public void ValidateHexDataLength_ComprehensiveValidation_AllValidCases()
    {
        // Test all valid combinations of address ranges and lengths

        // Coils (1-9999): even lengths
        for (int addr = 1; addr <= 9999; addr += 1000)
        {
            RegisterService.ValidateHexDataLength(addr, "AB");
            RegisterService.ValidateHexDataLength(addr, "ABCD");
            RegisterService.ValidateHexDataLength(addr, "ABCDEF");
        }

        // Discrete inputs (10001-19999): even lengths
        for (int addr = 10001; addr <= 19999; addr += 1000)
        {
            RegisterService.ValidateHexDataLength(addr, "AB");
            RegisterService.ValidateHexDataLength(addr, "ABCD");
            RegisterService.ValidateHexDataLength(addr, "ABCDEF");
        }

        // Input registers (30001-39999): multiples of 4
        for (int addr = 30001; addr <= 39999; addr += 1000)
        {
            RegisterService.ValidateHexDataLength(addr, "ABCD");
            RegisterService.ValidateHexDataLength(addr, "ABCDEFGH");
            RegisterService.ValidateHexDataLength(addr, "ABCDEFGHIJKL");
        }

        // Holding registers (40001-49999): multiples of 4
        for (int addr = 40001; addr <= 49999; addr += 1000)
        {
            RegisterService.ValidateHexDataLength(addr, "ABCD");
            RegisterService.ValidateHexDataLength(addr, "ABCDEFGH");
            RegisterService.ValidateHexDataLength(addr, "ABCDEFGHIJKL");
        }
    }

    #endregion
}