using System.Collections.Concurrent;
using CANOpen.Enums;
using CANOpen.Exceptions;
using CANOpen.Interfaces;
using CANOpen.Models;

namespace CANOpen.Services;

public class SdoClient
{
    private readonly ICanBus _canBus;
    private readonly byte _nodeId;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<SdoResponse>> _pendingRequests;
    private readonly TimeSpan _timeout;
    
    public SdoClient(ICanBus canBus, byte nodeId, TimeSpan? timeout = null)
    {
        _canBus = canBus;
        _nodeId = nodeId;
        _timeout = timeout ?? TimeSpan.FromSeconds(1);
        _pendingRequests = new ConcurrentDictionary<string, TaskCompletionSource<SdoResponse>>();
        
        _canBus.FrameReceived += OnFrameReceived;
    }
    
    public async Task<byte[]> UploadAsync(ushort index, byte subIndex, CancellationToken cancellationToken = default)
    {
        var request = SdoRequest.CreateUpload(index, subIndex);
        var response = await SendRequestAsync(request, cancellationToken);
        
        if (response.IsAbort)
        {
            var abortCode = (SdoAbortCode)(uint)response.AbortCode;
            var message = $"SDO Upload failed: {abortCode.GetDescription()}";
            throw new SdoException(_nodeId, index, subIndex, (uint)response.AbortCode, message);
        }
        
        return response.GetDataBytes();
    }
    
    public async Task DownloadAsync(ushort index, byte subIndex, byte[] data, CancellationToken cancellationToken = default)
    {
        if (data.Length > 4)
            throw new NotSupportedException("Only expedited SDO transfer (up to 4 bytes) is currently supported");
        
        var request = SdoRequest.CreateDownload(index, subIndex, data);
        var response = await SendRequestAsync(request, cancellationToken);
        
        if (response.IsAbort)
        {
            var abortCode = (SdoAbortCode)(uint)response.AbortCode;
            var message = $"SDO Download failed: {abortCode.GetDescription()}";
            throw new SdoException(_nodeId, index, subIndex, (uint)response.AbortCode, message);
        }
    }
    
    private async Task<SdoResponse> SendRequestAsync(SdoRequest request, CancellationToken cancellationToken)
    {
        string requestKey = $"{request.Index:X4}:{request.SubIndex:X2}";
        var tcs = new TaskCompletionSource<SdoResponse>();
        
        if (!_pendingRequests.TryAdd(requestKey, tcs))
            throw new InvalidOperationException($"A request for {requestKey} is already pending");
        
        try
        {
            uint cobId = (uint)(CanMessageType.Rsdo) + _nodeId;
            await _canBus.SendFrameAsync(cobId, request.ToBytes(), cancellationToken);
            
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_timeout);
            
            try
            {
                return await tcs.Task.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                var message = $"SDO request for object {request.Index:X4}h.{request.SubIndex:X2}h";
                throw new CanOpenTimeoutException($"SDO {(request.CommandSpecifier == (byte)SdoCommand.UploadInitiate ? "Upload" : "Download")}", _timeout, message);
            }
        }
        finally
        {
            _pendingRequests.TryRemove(requestKey, out _);
        }
    }
    
    private void OnFrameReceived(object? sender, CanFrameReceivedEventArgs e)
    {
        uint expectedCobId = (uint)(CanMessageType.Tsdo) + _nodeId;
        if (e.CanId != expectedCobId || e.Data.Length < 8)
            return;
        
        try
        {
            var response = SdoResponse.FromBytes(e.Data);
            string requestKey = $"{response.Index:X4}:{response.SubIndex:X2}";
            
            if (_pendingRequests.TryGetValue(requestKey, out var tcs))
            {
                tcs.TrySetResult(response);
            }
        }
        catch
        {
        }
    }
}


