using CANOpen.Enums;
using CANOpen.Interfaces;

namespace CANOpen.Services;

/// <summary>
/// SYNC Producer - phát tin nhắn SYNC định kỳ cho synchronous PDOs
/// </summary>
public class SyncProducer : IDisposable
{
    private readonly ICanBus _canBus;
    private readonly uint _cobId;
    private Timer? _timer;
    private byte _counter;
    private bool _useCounter;
    private bool _isRunning;
    
    public bool IsRunning => _isRunning;
    public int IntervalMs { get; private set; }
    
    public SyncProducer(ICanBus canBus, uint cobId = (uint)CanMessageType.Sync)
    {
        _canBus = canBus;
        _cobId = cobId;
        _counter = 0;
        _useCounter = false;
        _isRunning = false;
    }
    
    /// <summary>
    /// Start SYNC producer với interval tính bằng milliseconds
    /// </summary>
    /// <param name="intervalMs">SYNC interval in milliseconds (thường 1-100ms)</param>
    /// <param name="useCounter">Nếu true, SYNC message sẽ có counter byte (1-240)</param>
    public void Start(int intervalMs, bool useCounter = false)
    {
        if (_isRunning)
            Stop();
        
        IntervalMs = intervalMs;
        _useCounter = useCounter;
        _counter = 0;
        _isRunning = true;
        
        _timer = new Timer(SendSyncCallback, null, 0, intervalMs);
    }
    
    /// <summary>
    /// Stop SYNC producer
    /// </summary>
    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
        _isRunning = false;
        _counter = 0;
    }
    
    /// <summary>
    /// Gửi một SYNC message thủ công (không dùng timer)
    /// </summary>
    public async Task SendSyncAsync(CancellationToken ct = default)
    {
        byte[] data = Array.Empty<byte>();
        
        if (_useCounter)
        {
            _counter++;
            if (_counter > 240)
                _counter = 1;
            
            data = new byte[] { _counter };
        }
        
        await _canBus.SendFrameAsync(_cobId, data, ct);
    }
    
    private async void SendSyncCallback(object? state)
    {
        try
        {
            await SendSyncAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Log error nhưng không dừng timer
            Console.WriteLine($"SYNC send error: {ex.Message}");
        }
    }
    
    public void Dispose()
    {
        Stop();
    }
}
