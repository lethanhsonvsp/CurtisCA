using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using SocketCANSharp;
using CANOpen.Interfaces;

namespace CANOpen.Services;

public class SocketCanBus : ICanBus
{
    private SafeFileDescriptorHandle? _socketHandle;
    private readonly string _interfaceName;
    private bool _isConnected;
    private Task? _receiveTask;
    private CancellationTokenSource? _receiveCts;
    
    public string InterfaceName => _interfaceName;
    public bool IsConnected => _isConnected;
    
    public event EventHandler<CanFrameReceivedEventArgs>? FrameReceived;
    
    public SocketCanBus(string interfaceName)
    {
        _interfaceName = interfaceName;
    }
    
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_isConnected)
            return;
        
        await Task.Run(() =>
        {
            _socketHandle = LibcNativeMethods.Socket(
                SocketCanConstants.PF_CAN, 
                SocketType.Raw, 
                SocketCanProtocolType.CAN_RAW);
            
            if (_socketHandle.IsInvalid)
                throw new InvalidOperationException("Failed to create CAN socket");
            
            var ifr = new Ifreq(_interfaceName);
            int ioctlResult = LibcNativeMethods.Ioctl(_socketHandle, SocketCanConstants.SIOCGIFINDEX, ifr);
            if (ioctlResult == -1)
                throw new InvalidOperationException($"Failed to find interface {_interfaceName}");
            
            var addr = new SockAddrCan(ifr.IfIndex);
            int bindResult = LibcNativeMethods.Bind(_socketHandle, addr, Marshal.SizeOf<SockAddrCan>());
            if (bindResult == -1)
                throw new InvalidOperationException("Failed to bind to CAN interface");
            
            _isConnected = true;
        }, cancellationToken);
        
        _receiveCts = new CancellationTokenSource();
        _receiveTask = Task.Run(() => ReceiveLoop(_receiveCts.Token), _receiveCts.Token);
    }
    
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
            return;
        
        _isConnected = false;
        
        if (_receiveCts != null)
        {
            _receiveCts.Cancel();
            if (_receiveTask != null)
                await _receiveTask;
            _receiveCts.Dispose();
            _receiveCts = null;
        }
        
        _socketHandle?.Dispose();
        _socketHandle = null;
    }
    
    public Task SendFrameAsync(uint canId, byte[] data, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_isConnected || _socketHandle == null)
                throw new InvalidOperationException("Not connected to CAN bus");

            if (data.Length > 8)
                throw new ArgumentException("CAN frame data cannot exceed 8 bytes");

            return Task.Run(() =>
            {
                var frame = new CanFrame
                {
                    CanId = canId,
                    Length = (byte)data.Length,
                    Data = new byte[8]
                };

                for (int i = 0; i < data.Length; i++)
                    frame.Data[i] = data[i];

                int frameSize = Marshal.SizeOf<CanFrame>();
                int bytesWritten = LibcNativeMethods.Write(_socketHandle, ref frame, frameSize);

                if (bytesWritten != frameSize)
                    throw new InvalidOperationException("Failed to send CAN frame");

            }, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ SendFrameAsync({canId:X3}) failed: {ex.Message}");
            throw;
        }

    }

    private void ReceiveLoop(CancellationToken cancellationToken)
    {
        if (_socketHandle == null)
            return;
        
        int frameSize = Marshal.SizeOf<CanFrame>();
        
        while (!cancellationToken.IsCancellationRequested && _isConnected)
        {
            try
            {
                var readFrame = new CanFrame();
                int nReadBytes = LibcNativeMethods.Read(_socketHandle, ref readFrame, frameSize);
                
                if (nReadBytes > 0)
                {
                    var timeval = new Timeval();
                    int result = LibcNativeMethods.Ioctl(_socketHandle, SocketCanConstants.SIOCGSTAMP, timeval);
                    
                    DateTime timestamp = result != -1
                        ? DateTimeOffset.FromUnixTimeSeconds(timeval.Seconds)
                            .AddMicroseconds(timeval.Microseconds).DateTime
                        : DateTime.UtcNow;
                    
                    byte[] data = new byte[readFrame.Length];
                    for (int i = 0; i < readFrame.Length; i++)
                        data[i] = readFrame.Data[i];
                    
                    FrameReceived?.Invoke(this, new CanFrameReceivedEventArgs(
                        readFrame.CanId,
                        data,
                        timestamp));
                }
            }
            catch (Exception)
            {
                if (!cancellationToken.IsCancellationRequested)
                    throw;
            }
        }
    }
    
    public void Dispose()
    {
        DisconnectAsync().GetAwaiter().GetResult();
    }
}
