using CANOpen.Enums;
using CANOpen.Interfaces;
using CANOpen.Models;
using CANOpen.Services;

namespace CANOpen;

/// <summary>
/// CANOpen Device với đầy đủ các protocols: SDO, NMT, PDO, SYNC, Emergency, Heartbeat
/// </summary>
public class CanOpenDevice : ICanOpenDevice, IDisposable
{
    private readonly ICanBus _canBus;
    private readonly SdoClient _sdoClient;
    private readonly NmtMaster _nmtMaster;
    private readonly PdoManager _pdoManager;
    private readonly EmergencyMonitor _emergencyMonitor;
    private readonly HeartbeatConsumer _heartbeatConsumer;
    private SyncProducer? _syncProducer;
    
    public byte NodeId { get; }
    public NmtState State { get; private set; }
    
    // Expose các services để user có thể truy cập
    public PdoManager Pdo => _pdoManager;
    public EmergencyMonitor Emergency => _emergencyMonitor;
    public HeartbeatConsumer Heartbeat => _heartbeatConsumer;
    public SyncProducer? Sync => _syncProducer;
    
    // Events từ các protocols
    public event EventHandler<PdoReceivedEventArgs>? PdoReceived
    {
        add => _pdoManager.PdoReceived += value;
        remove => _pdoManager.PdoReceived -= value;
    }
    
    public event EventHandler<EmergencyReceivedEventArgs>? EmergencyReceived
    {
        add => _emergencyMonitor.EmergencyReceived += value;
        remove => _emergencyMonitor.EmergencyReceived -= value;
    }
    
    public event EventHandler<HeartbeatReceivedEventArgs>? HeartbeatReceived
    {
        add => _heartbeatConsumer.HeartbeatReceived += value;
        remove => _heartbeatConsumer.HeartbeatReceived -= value;
    }
    
    public event EventHandler<HeartbeatTimeoutEventArgs>? HeartbeatTimeout
    {
        add => _heartbeatConsumer.HeartbeatTimeout += value;
        remove => _heartbeatConsumer.HeartbeatTimeout -= value;
    }
    
    public CanOpenDevice(ICanBus canBus, byte nodeId)
    {
        if (nodeId == 0 || nodeId > 127)
            throw new ArgumentException("NodeId must be between 1 and 127", nameof(nodeId));
        
        _canBus = canBus;
        NodeId = nodeId;
        State = NmtState.PreOperational;
        
        _sdoClient = new SdoClient(canBus, nodeId);
        _nmtMaster = new NmtMaster(canBus);
        _pdoManager = new PdoManager(canBus, nodeId);
        _emergencyMonitor = new EmergencyMonitor(canBus);
        _heartbeatConsumer = new HeartbeatConsumer(canBus);
    }
    
    #region SDO Operations
    
    public async Task<byte[]> ReadObjectAsync(ushort index, byte subIndex, CancellationToken cancellationToken = default)
    {
        return await _sdoClient.UploadAsync(index, subIndex, cancellationToken);
    }
    
    public async Task WriteObjectAsync(ushort index, byte subIndex, byte[] data, CancellationToken cancellationToken = default)
    {
        await _sdoClient.DownloadAsync(index, subIndex, data, cancellationToken);
    }
    
    public async Task<byte> ReadUInt8Async(ushort index, byte subIndex, CancellationToken cancellationToken = default)
    {
        var data = await ReadObjectAsync(index, subIndex, cancellationToken);
        return data.Length > 0 ? data[0] : (byte)0;
    }
    
    public async Task<ushort> ReadUInt16Async(ushort index, byte subIndex, CancellationToken cancellationToken = default)
    {
        var data = await ReadObjectAsync(index, subIndex, cancellationToken);
        return data.Length >= 2 ? BitConverter.ToUInt16(data, 0) : (ushort)0;
    }
    
    public async Task<uint> ReadUInt32Async(ushort index, byte subIndex, CancellationToken cancellationToken = default)
    {
        var data = await ReadObjectAsync(index, subIndex, cancellationToken);
        return data.Length >= 4 ? BitConverter.ToUInt32(data, 0) : 0u;
    }
    
    public async Task<short> ReadInt16Async(ushort index, byte subIndex, CancellationToken cancellationToken = default)
    {
        var data = await ReadObjectAsync(index, subIndex, cancellationToken);
        return data.Length >= 2 ? BitConverter.ToInt16(data, 0) : (short)0;
    }
    
    public async Task<int> ReadInt32Async(ushort index, byte subIndex, CancellationToken cancellationToken = default)
    {
        var data = await ReadObjectAsync(index, subIndex, cancellationToken);
        return data.Length >= 4 ? BitConverter.ToInt32(data, 0) : 0;
    }
    
    public async Task WriteUInt8Async(ushort index, byte subIndex, byte value, CancellationToken cancellationToken = default)
    {
        await WriteObjectAsync(index, subIndex, new[] { value }, cancellationToken);
    }
    
    public async Task WriteUInt16Async(ushort index, byte subIndex, ushort value, CancellationToken cancellationToken = default)
    {
        await WriteObjectAsync(index, subIndex, BitConverter.GetBytes(value), cancellationToken);
    }
    
    public async Task WriteUInt32Async(ushort index, byte subIndex, uint value, CancellationToken cancellationToken = default)
    {
        await WriteObjectAsync(index, subIndex, BitConverter.GetBytes(value), cancellationToken);
    }
    
    public async Task WriteInt16Async(ushort index, byte subIndex, short value, CancellationToken cancellationToken = default)
    {
        await WriteObjectAsync(index, subIndex, BitConverter.GetBytes(value), cancellationToken);
    }
    
    public async Task WriteInt32Async(ushort index, byte subIndex, int value, CancellationToken cancellationToken = default)
    {
        await WriteObjectAsync(index, subIndex, BitConverter.GetBytes(value), cancellationToken);
    }
    
    #endregion
    
    #region NMT Operations
    
    public async Task SendNmtCommandAsync(NmtCommand command, CancellationToken cancellationToken = default)
    {
        await _nmtMaster.SendCommandAsync(command, NodeId, cancellationToken);
        
        State = command switch
        {
            NmtCommand.Start => NmtState.Operational,
            NmtCommand.Stop => NmtState.Stopped,
            NmtCommand.PreOperational => NmtState.PreOperational,
            _ => State
        };
    }
    
    public async Task StartNodeAsync(CancellationToken cancellationToken = default)
    {
        await SendNmtCommandAsync(NmtCommand.Start, cancellationToken);
    }
    
    public async Task StopNodeAsync(CancellationToken cancellationToken = default)
    {
        await SendNmtCommandAsync(NmtCommand.Stop, cancellationToken);
    }
    
    public async Task ResetNodeAsync(CancellationToken cancellationToken = default)
    {
        await SendNmtCommandAsync(NmtCommand.ResetNode, cancellationToken);
    }
    
    public async Task ResetCommunicationAsync(CancellationToken cancellationToken = default)
    {
        await SendNmtCommandAsync(NmtCommand.ResetCommunication, cancellationToken);
    }
    
    #endregion
    
    #region SYNC Operations
    
    /// <summary>
    /// Khởi tạo SYNC producer (chỉ dùng cho master node)
    /// </summary>
    public void EnableSyncProducer(int intervalMs, bool useCounter = false)
    {
        _syncProducer ??= new SyncProducer(_canBus);
        _syncProducer.Start(intervalMs, useCounter);
    }
    
    /// <summary>
    /// Dừng SYNC producer
    /// </summary>
    public void DisableSyncProducer()
    {
        _syncProducer?.Stop();
    }
    
    #endregion
    
    #region PDO Configuration Helpers
    
    /// <summary>
    /// Configure standard TPDO1-4 theo CANOpen DS301
    /// </summary>
    public void ConfigureStandardTPDOs()
    {
        // TPDO1: COB-ID 0x180 + NodeId
        _pdoManager.ConfigureTPDO(new PdoConfiguration(1, (uint)(0x180 + NodeId)));
        
        // TPDO2: COB-ID 0x280 + NodeId
        _pdoManager.ConfigureTPDO(new PdoConfiguration(2, (uint)(0x280 + NodeId)));
        
        // TPDO3: COB-ID 0x380 + NodeId
        _pdoManager.ConfigureTPDO(new PdoConfiguration(3, (uint)(0x380 + NodeId)));
        
        // TPDO4: COB-ID 0x480 + NodeId
        _pdoManager.ConfigureTPDO(new PdoConfiguration(4, (uint)(0x480 + NodeId)));
    }
    
    /// <summary>
    /// Configure standard RPDO1-4 theo CANOpen DS301
    /// </summary>
    public void ConfigureStandardRPDOs()
    {
        // RPDO1: COB-ID 0x200 + NodeId
        _pdoManager.ConfigureRPDO(new PdoConfiguration(1, (uint)(0x200 + NodeId)));
        
        // RPDO2: COB-ID 0x300 + NodeId
        _pdoManager.ConfigureRPDO(new PdoConfiguration(2, (uint)(0x300 + NodeId)));
        
        // RPDO3: COB-ID 0x400 + NodeId
        _pdoManager.ConfigureRPDO(new PdoConfiguration(3, (uint)(0x400 + NodeId)));
        
        // RPDO4: COB-ID 0x500 + NodeId
        _pdoManager.ConfigureRPDO(new PdoConfiguration(4, (uint)(0x500 + NodeId)));
    }
    
    #endregion
    
    #region Heartbeat Operations
    
    /// <summary>
    /// Bắt đầu monitor heartbeat của node này
    /// </summary>
    public void EnableHeartbeatMonitoring(int timeoutMs = 2000)
    {
        _heartbeatConsumer.MonitorNode(NodeId, timeoutMs);
    }
    
    /// <summary>
    /// Dừng monitor heartbeat
    /// </summary>
    public void DisableHeartbeatMonitoring()
    {
        _heartbeatConsumer.StopMonitoring(NodeId);
    }
    
    #endregion
    
    public void Dispose()
    {
        _syncProducer?.Dispose();
        _heartbeatConsumer?.Dispose();
    }
}
