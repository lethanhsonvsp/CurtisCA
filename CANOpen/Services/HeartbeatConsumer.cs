using System.Collections.Concurrent;
using CANOpen.Enums;
using CANOpen.Interfaces;
using CANOpen.Models;

namespace CANOpen.Services;

/// <summary>
/// Heartbeat Consumer - theo dõi heartbeat messages từ các nodes và phát hiện node timeout
/// </summary>
public class HeartbeatConsumer : IDisposable
{
    private readonly ICanBus _canBus;
    private readonly ConcurrentDictionary<byte, NodeHeartbeatInfo> _nodes;
    private readonly Timer _checkTimer;
    
    public event EventHandler<HeartbeatReceivedEventArgs>? HeartbeatReceived;
    public event EventHandler<HeartbeatTimeoutEventArgs>? HeartbeatTimeout;
    
    public HeartbeatConsumer(ICanBus canBus, int checkIntervalMs = 100)
    {
        _canBus = canBus;
        _nodes = new ConcurrentDictionary<byte, NodeHeartbeatInfo>();
        
        _canBus.FrameReceived += OnFrameReceived;
        _checkTimer = new Timer(CheckHeartbeats, null, checkIntervalMs, checkIntervalMs);
    }
    
    /// <summary>
    /// Bắt đầu monitor heartbeat của một node
    /// </summary>
    /// <param name="nodeId">Node ID</param>
    /// <param name="timeoutMs">Timeout in milliseconds (thường 1000-3000ms)</param>
    public void MonitorNode(byte nodeId, int timeoutMs)
    {
        var info = new NodeHeartbeatInfo
        {
            NodeId = nodeId,
            TimeoutMs = timeoutMs,
            LastState = NmtState.Unknown,
            LastReceived = DateTime.MinValue,
            IsAlive = false
        };
        
        _nodes[nodeId] = info;
    }
    
    /// <summary>
    /// Dừng monitor heartbeat của một node
    /// </summary>
    public void StopMonitoring(byte nodeId)
    {
        _nodes.TryRemove(nodeId, out _);
    }
    
    /// <summary>
    /// Lấy thông tin heartbeat của một node
    /// </summary>
    public NodeHeartbeatInfo? GetNodeInfo(byte nodeId)
    {
        return _nodes.TryGetValue(nodeId, out var info) ? info : null;
    }
    
    /// <summary>
    /// Lấy tất cả nodes đang được monitor
    /// </summary>
    public IReadOnlyDictionary<byte, NodeHeartbeatInfo> GetAllNodes()
    {
        return _nodes;
    }
    
    private void OnFrameReceived(object? sender, CanFrameReceivedEventArgs e)
    {
        // Heartbeat COB-ID: 0x700 + NodeID
        uint baseHeartbeatCobId = (uint)CanMessageType.Heartbeat;
        
        if (e.CanId >= baseHeartbeatCobId && e.CanId < baseHeartbeatCobId + 0x7F)
        {
            byte nodeId = (byte)(e.CanId - baseHeartbeatCobId);
            
            if (_nodes.TryGetValue(nodeId, out var info) && e.Data.Length >= 1)
            {
                var heartbeat = HeartbeatMessage.FromBytes(nodeId, e.Data);
                
                info.LastState = heartbeat.State;
                info.LastReceived = DateTime.UtcNow;
                info.IsAlive = true;
                
                HeartbeatReceived?.Invoke(this, new HeartbeatReceivedEventArgs(heartbeat));
            }
        }
    }
    
    private void CheckHeartbeats(object? state)
    {
        var now = DateTime.UtcNow;
        
        foreach (var kvp in _nodes)
        {
            var info = kvp.Value;
            
            if (info.IsAlive && info.LastReceived != DateTime.MinValue)
            {
                var elapsed = (now - info.LastReceived).TotalMilliseconds;
                
                if (elapsed > info.TimeoutMs)
                {
                    info.IsAlive = false;
                    HeartbeatTimeout?.Invoke(this, new HeartbeatTimeoutEventArgs(
                        info.NodeId, 
                        info.LastState, 
                        (int)elapsed));
                }
            }
        }
    }
    
    public void Dispose()
    {
        _checkTimer?.Dispose();
        _nodes.Clear();
    }
}

/// <summary>
/// Thông tin heartbeat của một node
/// </summary>
public class NodeHeartbeatInfo
{
    public byte NodeId { get; set; }
    public int TimeoutMs { get; set; }
    public NmtState LastState { get; set; }
    public DateTime LastReceived { get; set; }
    public bool IsAlive { get; set; }
    
    public int TimeSinceLastHeartbeat => 
        LastReceived == DateTime.MinValue 
            ? -1 
            : (int)(DateTime.UtcNow - LastReceived).TotalMilliseconds;
}

/// <summary>
/// Event args cho Heartbeat received
/// </summary>
public class HeartbeatReceivedEventArgs : EventArgs
{
    public HeartbeatMessage Heartbeat { get; }
    
    public HeartbeatReceivedEventArgs(HeartbeatMessage heartbeat)
    {
        Heartbeat = heartbeat;
    }
}

/// <summary>
/// Event args cho Heartbeat timeout
/// </summary>
public class HeartbeatTimeoutEventArgs : EventArgs
{
    public byte NodeId { get; }
    public NmtState LastKnownState { get; }
    public int ElapsedMs { get; }
    
    public HeartbeatTimeoutEventArgs(byte nodeId, NmtState lastKnownState, int elapsedMs)
    {
        NodeId = nodeId;
        LastKnownState = lastKnownState;
        ElapsedMs = elapsedMs;
    }
}
