using CANOpen.Enums;
using CANOpen.Interfaces;
using CANOpen.Models;

namespace CANOpen.Services;

/// <summary>
/// Emergency Monitor - theo dõi emergency messages từ các nodes
/// </summary>
public class EmergencyMonitor
{
    private readonly ICanBus _canBus;
    private readonly Dictionary<byte, EmergencyMessage> _lastEmergencies;
    
    public event EventHandler<EmergencyReceivedEventArgs>? EmergencyReceived;
    
    public EmergencyMonitor(ICanBus canBus)
    {
        _canBus = canBus;
        _lastEmergencies = new Dictionary<byte, EmergencyMessage>();
        _canBus.FrameReceived += OnFrameReceived;
    }
    
    /// <summary>
    /// Lấy emergency message cuối cùng từ một node
    /// </summary>
    public EmergencyMessage? GetLastEmergency(byte nodeId)
    {
        lock (_lastEmergencies)
        {
            return _lastEmergencies.TryGetValue(nodeId, out var emergency) ? emergency : null;
        }
    }
    
    /// <summary>
    /// Xóa emergency history của một node
    /// </summary>
    public void ClearEmergency(byte nodeId)
    {
        lock (_lastEmergencies)
        {
            _lastEmergencies.Remove(nodeId);
        }
    }
    
    /// <summary>
    /// Xóa tất cả emergency history
    /// </summary>
    public void ClearAll()
    {
        lock (_lastEmergencies)
        {
            _lastEmergencies.Clear();
        }
    }
    
    private void OnFrameReceived(object? sender, CanFrameReceivedEventArgs e)
    {
        // EMERGENCY COB-ID: 0x80 + NodeID
        uint baseEmergencyCobId = (uint)CanMessageType.Emergency;
        
        if (e.CanId >= baseEmergencyCobId && e.CanId < baseEmergencyCobId + 0x7F)
        {
            var emergency = EmergencyMessage.FromCanFrame(e.CanId, e.Data, e.Timestamp);
            
            lock (_lastEmergencies)
            {
                _lastEmergencies[emergency.NodeId] = emergency;
            }
            
            EmergencyReceived?.Invoke(this, new EmergencyReceivedEventArgs(emergency));
        }
    }
}

/// <summary>
/// Event args cho Emergency received
/// </summary>
public class EmergencyReceivedEventArgs : EventArgs
{
    public EmergencyMessage Emergency { get; }
    
    public EmergencyReceivedEventArgs(EmergencyMessage emergency)
    {
        Emergency = emergency;
    }
    
    /// <summary>
    /// Helper để format emergency message cho logging
    /// </summary>
    public string GetDescription()
    {
        var errorClass = ((ushort)Emergency.ErrorCode) >> 8;
        var errorType = GetErrorType(errorClass);
        
        // Error Register bits (DS301):
        // Bit 0: Generic Error
        // Bit 1: Current
        // Bit 2: Voltage
        // Bit 3: Temperature
        // Bit 4: Communication Error
        // Bit 5: Device Profile Specific
        // Bit 6: Reserved
        // Bit 7: Manufacturer Specific
        
        bool isGeneric = (Emergency.ErrorRegister & 0x01) != 0;
        bool isCurrent = (Emergency.ErrorRegister & 0x02) != 0;
        bool isVoltage = (Emergency.ErrorRegister & 0x04) != 0;
        bool isTemp = (Emergency.ErrorRegister & 0x08) != 0;
        
        return $"Node {Emergency.NodeId}: {errorType} - Error 0x{(ushort)Emergency.ErrorCode:X4}" +
               $" (Generic: {isGeneric}, Current: {isCurrent}, " +
               $"Voltage: {isVoltage}, Temp: {isTemp})";
    }
    
    private string GetErrorType(int errorClass)
    {
        return errorClass switch
        {
            0x00 => "Error Reset or No Error",
            0x10 => "Generic Error",
            0x20 => "Current Error",
            0x21 => "Current Device Input Side",
            0x22 => "Current Inside Device",
            0x23 => "Current Device Output Side",
            0x30 => "Voltage Error",
            0x31 => "Mains Voltage",
            0x32 => "Voltage Inside Device",
            0x33 => "Output Voltage",
            0x40 => "Temperature Error",
            0x41 => "Ambient Temperature",
            0x42 => "Device Temperature",
            0x50 => "Device Hardware Error",
            0x60 => "Device Software Error",
            0x61 => "Internal Software Error",
            0x62 => "User Software Error",
            0x63 => "Data Set Error",
            0x70 => "Additional Modules Error",
            0x80 => "Monitoring Error",
            0x81 => "Communication Error",
            0x82 => "Protocol Error",
            0x90 => "External Error",
            0xF0 => "Additional Functions Error",
            0xFF => "Device Specific Error",
            _ => "Unknown Error"
        };
    }
}
