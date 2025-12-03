using CANOpen.Enums;

namespace CANOpen.Models;

public readonly struct NmtMessage(NmtCommand command, byte nodeId)
{
    public NmtCommand Command { get; init; } = command;
    public byte NodeId { get; init; } = nodeId;

    public byte[] ToBytes()
    {
        return [(byte)Command, NodeId];
    }
    
    public static NmtMessage FromBytes(byte[] data)
    {
        if (data.Length < 2)
            throw new ArgumentException("NMT message must be at least 2 bytes");
        
        return new NmtMessage
        {
            Command = (NmtCommand)data[0],
            NodeId = data[1]
        };
    }
}

public readonly struct HeartbeatMessage(byte nodeId, NmtState state)
{
    public byte NodeId { get; init; } = nodeId;
    public NmtState State { get; init; } = state;

    public byte[] ToBytes()
    {
        return [(byte)State];
    }
    
    public static HeartbeatMessage FromBytes(uint canId, byte[] data)
    {
        if (data.Length < 1)
            throw new ArgumentException("Heartbeat message must be at least 1 byte");
        
        byte nodeId = (byte)(canId & 0x7F);
        
        return new HeartbeatMessage
        {
            NodeId = nodeId,
            State = (NmtState)data[0]
        };
    }
}
