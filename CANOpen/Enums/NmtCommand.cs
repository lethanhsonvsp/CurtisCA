namespace CANOpen.Enums;

/// <summary>
/// NMT (Network Management) commands
/// </summary>
public enum NmtCommand : byte
{
    Start = 0x01,
    Stop = 0x02,
    PreOperational = 0x80,
    ResetNode = 0x81,
    ResetCommunication = 0x82
}

/// <summary>
/// NMT States
/// </summary>
public enum NmtState : byte
{
    Unknown = 0xFF,
    Initializing = 0x00,
    Stopped = 0x04,
    Operational = 0x05,
    PreOperational = 0x7F,
    BootUp = 0x00
}
