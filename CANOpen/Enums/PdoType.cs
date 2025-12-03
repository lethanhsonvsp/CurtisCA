namespace CANOpen.Enums;

/// <summary>
/// Loại PDO (Process Data Object)
/// </summary>
public enum PdoType
{
    /// <summary>
    /// Transmit PDO - Dữ liệu gửi từ device
    /// </summary>
    Transmit,
    
    /// <summary>
    /// Receive PDO - Dữ liệu nhận vào device
    /// </summary>
    Receive
}

/// <summary>
/// PDO Transmission Type
/// </summary>
public enum PdoTransmissionType : byte
{
    /// <summary>
    /// Acyclic synchronous - Gửi theo lệnh
    /// </summary>
    AcyclicSynchronous = 0x00,
    
    /// <summary>
    /// Cyclic synchronous - Gửi sau mỗi N SYNC (N = 1-240)
    /// </summary>
    CyclicSynchronousMin = 0x01,
    CyclicSynchronousMax = 0xF0,
    
    /// <summary>
    /// Synchronous RTR-only
    /// </summary>
    SynchronousRTR = 0xFC,
    
    /// <summary>
    /// Asynchronous RTR-only
    /// </summary>
    AsynchronousRTR = 0xFD,
    
    /// <summary>
    /// Asynchronous - Gửi khi có thay đổi
    /// </summary>
    Asynchronous = 0xFE,
    
    /// <summary>
    /// Device profile specific
    /// </summary>
    DeviceProfile = 0xFF
}

/// <summary>
/// PDO Trigger type
/// </summary>
public enum PdoTrigger
{
    /// <summary>
    /// Sync triggered
    /// </summary>
    Sync,
    
    /// <summary>
    /// Event triggered (change of state)
    /// </summary>
    Event,
    
    /// <summary>
    /// RTR (Remote Transmission Request)
    /// </summary>
    RTR,
    
    /// <summary>
    /// Time triggered
    /// </summary>
    Timer
}
