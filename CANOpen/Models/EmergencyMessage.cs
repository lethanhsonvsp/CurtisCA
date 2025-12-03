namespace CANOpen.Models;

/// <summary>
/// Emergency (EMCY) Error Codes theo CANopen standard
/// </summary>
public enum EmergencyErrorCode : ushort
{
    // 0x00xx: Error Reset / No Error
    ErrorReset = 0x0000,
    
    // 0x10xx: Generic Error
    GenericError = 0x1000,
    
    // 0x20xx: Current
    CurrentGeneric = 0x2000,
    CurrentInputSide = 0x2100,
    CurrentInsideDevice = 0x2200,
    CurrentOutputSide = 0x2300,
    
    // 0x21xx: Current, device input side
    OverCurrent = 0x2110,
    
    // 0x22xx: Current inside the device
    ShortCircuit = 0x2220,
    
    // 0x23xx: Current, device output side
    LoadDump = 0x2330,
    
    // 0x30xx: Voltage
    VoltageGeneric = 0x3000,
    MainsVoltage = 0x3100,
    VoltageInsideDevice = 0x3200,
    OutputVoltage = 0x3300,
    
    // 0x31xx: Mains Voltage
    UnderVoltage = 0x3110,
    OverVoltage = 0x3120,
    
    // 0x40xx: Temperature
    TemperatureGeneric = 0x4000,
    AmbientTemperature = 0x4100,
    DeviceTemperature = 0x4200,
    
    // 0x41xx: Ambient Temperature
    TooHigh = 0x4110,
    TooLow = 0x4120,
    
    // 0x50xx: Device Hardware
    DeviceHardware = 0x5000,
    
    // 0x60xx: Device Software
    DeviceSoftware = 0x6000,
    InternalSoftware = 0x6100,
    UserSoftware = 0x6200,
    DataSet = 0x6300,
    
    // 0x70xx: Additional Modules
    AdditionalModules = 0x7000,
    
    // 0x80xx: Monitoring
    Monitoring = 0x8000,
    Communication = 0x8100,
    ProtocolError = 0x8200,
    
    // 0x81xx: Communication
    CanOverrun = 0x8110,
    ErrorPassive = 0x8120,
    HeartbeatError = 0x8130,
    BusOffRecovered = 0x8140,
    
    // 0x82xx: Protocol Error
    PdoNotProcessed = 0x8210,
    PdoLengthExceeded = 0x8220,
    DamMpdo = 0x8230,
    SyncDataLength = 0x8240,
    
    // 0x90xx: External Error
    ExternalError = 0x9000,
    
    // 0xF0xx: Additional Functions
    AdditionalFunctions = 0xF000,
    
    // 0xFFxx: Device specific
    DeviceSpecific = 0xFF00
}

/// <summary>
/// Emergency Message theo CANopen
/// </summary>
public readonly struct EmergencyMessage(byte nodeId, EmergencyErrorCode errorCode, byte errorRegister,
    byte[] manufacturerError, DateTime timestamp)
{
    /// <summary>
    /// Node ID của device gửi emergency
    /// </summary>
    public byte NodeId { get; init; } = nodeId;

    /// <summary>
    /// Emergency Error Code (16-bit)
    /// </summary>
    public EmergencyErrorCode ErrorCode { get; init; } = errorCode;

    /// <summary>
    /// Error Register (8-bit)
    /// </summary>
    public byte ErrorRegister { get; init; } = errorRegister;

    /// <summary>
    /// Manufacturer Specific Error Code (40-bit / 5 bytes)
    /// </summary>
    public byte[] ManufacturerError { get; init; } = manufacturerError;

    /// <summary>
    /// Timestamp khi nhận emergency
    /// </summary>
    public DateTime Timestamp { get; init; } = timestamp;

    /// <summary>
    /// Parse Emergency message từ CAN frame
    /// </summary>
    public static EmergencyMessage FromCanFrame(uint canId, byte[] data, DateTime timestamp)
    {
        if (data.Length < 8)
            throw new ArgumentException("Emergency message must be 8 bytes");
        
        byte nodeId = (byte)(canId & 0x7F);
        ushort errorCode = (ushort)(data[0] | (data[1] << 8));
        byte errorRegister = data[2];
        byte[] manufacturerError = new byte[5];
        Array.Copy(data, 3, manufacturerError, 0, 5);
        
        return new EmergencyMessage(nodeId, (EmergencyErrorCode)errorCode, errorRegister, 
            manufacturerError, timestamp);
    }
    
    /// <summary>
    /// Convert sang CAN frame data
    /// </summary>
    public byte[] ToBytes()
    {
        byte[] data = new byte[8];
        data[0] = (byte)((ushort)ErrorCode & 0xFF);
        data[1] = (byte)(((ushort)ErrorCode >> 8) & 0xFF);
        data[2] = ErrorRegister;
        
        if (ManufacturerError != null && ManufacturerError.Length >= 5)
        {
            Array.Copy(ManufacturerError, 0, data, 3, 5);
        }
        
        return data;
    }
    
    // Error Register bit definitions
    public bool GenericErrorBit => (ErrorRegister & 0x01) != 0;
    public bool CurrentErrorBit => (ErrorRegister & 0x02) != 0;
    public bool VoltageErrorBit => (ErrorRegister & 0x04) != 0;
    public bool TemperatureErrorBit => (ErrorRegister & 0x08) != 0;
    public bool CommunicationErrorBit => (ErrorRegister & 0x10) != 0;
    public bool DeviceProfileErrorBit => (ErrorRegister & 0x20) != 0;
    public bool ManufacturerErrorBit => (ErrorRegister & 0x80) != 0;
    
    public override string ToString()
    {
        return $"EMCY[Node {NodeId}]: Code=0x{(ushort)ErrorCode:X4}, Register=0x{ErrorRegister:X2}";
    }
}
