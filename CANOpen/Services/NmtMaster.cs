using CANOpen.Enums;
using CANOpen.Interfaces;
using CANOpen.Models;

namespace CANOpen.Services;

public class NmtMaster
{
    private readonly ICanBus _canBus;
    
    public NmtMaster(ICanBus canBus)
    {
        _canBus = canBus;
    }
    
    public async Task SendCommandAsync(NmtCommand command, byte nodeId, CancellationToken cancellationToken = default)
    {
        var message = new NmtMessage(command, nodeId);
        await _canBus.SendFrameAsync((uint)CanMessageType.Nmt, message.ToBytes(), cancellationToken);
    }
    
    public async Task BroadcastCommandAsync(NmtCommand command, CancellationToken cancellationToken = default)
    {
        await SendCommandAsync(command, 0, cancellationToken);
    }
    
    public async Task StartNodeAsync(byte nodeId, CancellationToken cancellationToken = default)
    {
        await SendCommandAsync(NmtCommand.Start, nodeId, cancellationToken);
    }
    
    public async Task StopNodeAsync(byte nodeId, CancellationToken cancellationToken = default)
    {
        await SendCommandAsync(NmtCommand.Stop, nodeId, cancellationToken);
    }
    
    public async Task SetPreOperationalAsync(byte nodeId, CancellationToken cancellationToken = default)
    {
        await SendCommandAsync(NmtCommand.PreOperational, nodeId, cancellationToken);
    }
    
    public async Task ResetNodeAsync(byte nodeId, CancellationToken cancellationToken = default)
    {
        await SendCommandAsync(NmtCommand.ResetNode, nodeId, cancellationToken);
    }
    
    public async Task ResetCommunicationAsync(byte nodeId, CancellationToken cancellationToken = default)
    {
        await SendCommandAsync(NmtCommand.ResetCommunication, nodeId, cancellationToken);
    }
}
