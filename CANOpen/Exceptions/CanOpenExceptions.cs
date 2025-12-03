using CANOpen.Enums;

namespace CANOpen.Exceptions;

/// <summary>
/// Base exception cho tất cả CANOpen errors
/// </summary>
public class CanOpenException : Exception
{
    public ushort ErrorCode { get; }
    public byte NodeId { get; }
    
    public CanOpenException(string message) : base(message)
    {
    }
    
    public CanOpenException(string message, Exception innerException) : base(message, innerException)
    {
    }
    
    public CanOpenException(ushort errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
    
    public CanOpenException(byte nodeId, ushort errorCode, string message) : base(message)
    {
        NodeId = nodeId;
        ErrorCode = errorCode;
    }
}

/// <summary>
/// SDO communication errors
/// </summary>
public class SdoException : CanOpenException
{
    public uint AbortCode { get; }
    public ushort Index { get; }
    public byte SubIndex { get; }
    
    public SdoException(uint abortCode, string message) : base(message)
    {
        AbortCode = abortCode;
    }
    
    public SdoException(byte nodeId, ushort index, byte subIndex, uint abortCode, string message) 
        : base(nodeId, 0, message)
    {
        Index = index;
        SubIndex = subIndex;
        AbortCode = abortCode;
    }
    
    public override string Message => 
        $"SDO Error on Node {NodeId}, Object {Index:X4}h.{SubIndex:X2}h: {base.Message} (Abort Code: 0x{AbortCode:X8})";
}

/// <summary>
/// PDO communication errors
/// </summary>
public class PdoException : CanOpenException
{
    public byte PdoNumber { get; }
    public uint CobId { get; }
    
    public PdoException(string message) : base(message)
    {
    }
    
    public PdoException(byte nodeId, byte pdoNumber, uint cobId, string message) : base(nodeId, 0, message)
    {
        PdoNumber = pdoNumber;
        CobId = cobId;
    }
    
    public override string Message => 
        $"PDO{PdoNumber} Error on Node {NodeId} (COB-ID: 0x{CobId:X3}): {base.Message}";
}

/// <summary>
/// NMT communication errors
/// </summary>
public class NmtException : CanOpenException
{
    public NmtCommand? Command { get; }
    
    public NmtException(string message) : base(message)
    {
    }
    
    public NmtException(byte nodeId, NmtCommand command, string message) : base(nodeId, 0, message)
    {
        Command = command;
    }
    
    public override string Message => 
        Command.HasValue 
            ? $"NMT Error on Node {NodeId}, Command {Command}: {base.Message}"
            : base.Message;
}

/// <summary>
/// Emergency message errors
/// </summary>
public class EmergencyException : CanOpenException
{
    public ushort EmergencyErrorCode { get; }
    public byte ErrorRegister { get; }
    public byte[] ManufacturerSpecificData { get; }
    
    public EmergencyException(byte nodeId, ushort emergencyErrorCode, byte errorRegister, 
        byte[] manufacturerData, string message) : base(nodeId, emergencyErrorCode, message)
    {
        EmergencyErrorCode = emergencyErrorCode;
        ErrorRegister = errorRegister;
        ManufacturerSpecificData = manufacturerData;
    }
    
    public override string Message => 
        $"Emergency on Node {NodeId}: {base.Message} (Error Code: 0x{EmergencyErrorCode:X4}, Error Register: 0x{ErrorRegister:X2})";
}

/// <summary>
/// Configuration validation errors
/// </summary>
public class ConfigurationException : CanOpenException
{
    public string Parameter { get; }
    public object? InvalidValue { get; }
    
    public ConfigurationException(string parameter, string message) : base(message)
    {
        Parameter = parameter;
    }
    
    public ConfigurationException(string parameter, object? invalidValue, string message) : base(message)
    {
        Parameter = parameter;
        InvalidValue = invalidValue;
    }
    
    public override string Message => 
        InvalidValue != null 
            ? $"Configuration Error for '{Parameter}' = {InvalidValue}: {base.Message}"
            : $"Configuration Error for '{Parameter}': {base.Message}";
}

/// <summary>
/// Timeout errors
/// </summary>
public class CanOpenTimeoutException : CanOpenException
{
    public TimeSpan Timeout { get; }
    public string Operation { get; }
    
    public CanOpenTimeoutException(string operation, TimeSpan timeout, string message) : base(message)
    {
        Operation = operation;
        Timeout = timeout;
    }
    
    public override string Message => 
        $"Timeout after {Timeout.TotalMilliseconds}ms during {Operation}: {base.Message}";
}

/// <summary>
/// CAN bus communication errors
/// </summary>
public class CanBusException : CanOpenException
{
    public string InterfaceName { get; }
    
    public CanBusException(string interfaceName, string message) : base(message)
    {
        InterfaceName = interfaceName;
    }
    
    public CanBusException(string interfaceName, string message, Exception innerException) 
        : base(message, innerException)
    {
        InterfaceName = interfaceName;
    }
    
    public override string Message => 
        $"CAN Bus Error on interface '{InterfaceName}': {base.Message}";
}