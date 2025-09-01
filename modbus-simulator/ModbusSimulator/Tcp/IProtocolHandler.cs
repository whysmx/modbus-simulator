using System.Net;

namespace ModbusSimulator.Tcp;

public interface IProtocolHandler
{
    Task<byte[]> ProcessRequestAsync(byte[] request, ProtocolContext context);
    string ProtocolType { get; }
}

public sealed class ProtocolContext
{
    public string? ConnectionId { get; init; }
    public int LocalPort { get; init; }
    public EndPoint? RemoteEndPoint { get; init; }
}