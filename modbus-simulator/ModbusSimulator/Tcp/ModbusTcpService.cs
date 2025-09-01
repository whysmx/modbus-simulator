namespace ModbusSimulator.Tcp;

public class ModbusTcpService : IProtocolHandler
{
    public string ProtocolType => "ModbusTCP";

    public async Task<byte[]> ProcessRequestAsync(byte[] request, ProtocolContext context)
    {
        try
        {
            var frame = ParseModbusTcpFrame(request);
            
            if (frame.FunctionCode < 1 || frame.FunctionCode > 4)
            {
                return BuildErrorResponse(frame.TransactionId, frame.Slaveid, (byte)(frame.FunctionCode + 0x80), 0x01);
            }

            var responsePdu = await HandleReadFunctionAsync(frame, context);
            return BuildResponseFrame(frame.TransactionId, frame.Slaveid, frame.FunctionCode, responsePdu);
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }

    private ModbusTcpFrame ParseModbusTcpFrame(byte[] request)
    {
        ushort ReadUInt16BE(ReadOnlySpan<byte> buf, int offset) => (ushort)((buf[offset] << 8) | buf[offset + 1]);
        
        var transactionId = ReadUInt16BE(request, 0);
        var protocolId = ReadUInt16BE(request, 2);
        var length = ReadUInt16BE(request, 4);
        var slaveid = request[6];
        var functionCode = request[7];
        var data = request.Skip(8).ToArray();
        
        return new ModbusTcpFrame
        {
            TransactionId = transactionId,
            Slaveid = slaveid,
            FunctionCode = functionCode,
            Data = data
        };
    }

    private async Task<byte[]> HandleReadFunctionAsync(ModbusTcpFrame frame, ProtocolContext context)
    {
        return Array.Empty<byte>();
    }

    private byte[] BuildResponseFrame(ushort transactionId, byte slaveid, byte functionCode, byte[] responseData)
    {
        void WriteUInt16BE(List<byte> dst, ushort value)
        {
            dst.Add((byte)(value >> 8));
            dst.Add((byte)(value & 0xFF));
        }

        var response = new List<byte>(7 + 1 + responseData.Length);
        WriteUInt16BE(response, transactionId);
        WriteUInt16BE(response, 0);
        WriteUInt16BE(response, (ushort)(responseData.Length + 2));
        response.Add(slaveid);
        response.Add(functionCode);
        response.AddRange(responseData);
        return response.ToArray();
    }

    private byte[] BuildErrorResponse(ushort transactionId, byte slaveid, byte errorFunctionCode, byte errorCode)
    {
        void WriteUInt16BE(List<byte> dst, ushort value)
        {
            dst.Add((byte)(value >> 8));
            dst.Add((byte)(value & 0xFF));
        }

        var pdu = new byte[] { errorFunctionCode, errorCode };
        var response = new List<byte>(7 + pdu.Length);
        WriteUInt16BE(response, transactionId);
        WriteUInt16BE(response, 0);
        WriteUInt16BE(response, (ushort)(pdu.Length + 1));
        response.Add(slaveid);
        response.AddRange(pdu);
        return response.ToArray();
    }
}

public class ModbusTcpFrame
{
    public ushort TransactionId { get; set; }
    public byte Slaveid { get; set; }
    public byte FunctionCode { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
}