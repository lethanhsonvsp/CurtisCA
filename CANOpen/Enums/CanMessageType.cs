namespace CANOpen.Enums;

/// <summary>
/// Loại tin nhắn CANOpen dựa trên COB-ID
/// </summary>
public enum CanMessageType : uint
{
    Nmt = 0x000,
    Sync = 0x080,
    Emergency = 0x080,
    Time = 0x100,
    Tpdo1 = 0x180,
    Rpdo1 = 0x200,
    Tpdo2 = 0x280,
    Rpdo2 = 0x300,
    Tpdo3 = 0x380,
    Rpdo3 = 0x400,
    Tpdo4 = 0x480,
    Rpdo4 = 0x500,
    Tsdo = 0x580,
    Rsdo = 0x600,
    Heartbeat = 0x700
}
