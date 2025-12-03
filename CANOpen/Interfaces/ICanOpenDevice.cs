using CANOpen.Enums;
using CANOpen.Models;

namespace CANOpen.Interfaces;

public interface ICanOpenDevice
{
    byte NodeId { get; }
    NmtState State { get; }
    
    Task<byte[]> ReadObjectAsync(ushort index, byte subIndex, CancellationToken cancellationToken = default);
    Task WriteObjectAsync(ushort index, byte subIndex, byte[] data, CancellationToken cancellationToken = default);
    Task SendNmtCommandAsync(NmtCommand command, CancellationToken cancellationToken = default);
}

public interface ICanBus : IDisposable
{
    string InterfaceName { get; }
    bool IsConnected { get; }
    
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task SendFrameAsync(uint canId, byte[] data, CancellationToken cancellationToken = default);
    
    event EventHandler<CanFrameReceivedEventArgs>? FrameReceived;
}

public class CanFrameReceivedEventArgs(uint canId, byte[] data, DateTime timestamp) : EventArgs
{
    public uint CanId { get; init; } = canId;
    public byte[] Data { get; init; } = data;
    public DateTime Timestamp { get; init; } = timestamp;
}
