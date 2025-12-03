using CANOpen.Interfaces;
using CANOpen.Models;
using CANOpen.Services;

namespace CurtisCA;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Replace with your concrete implementation of ICanBus
        // e.g. var canBus = new SocketCanBus("can0");
        ICanBus canBus = CreateYourCanBus(); // <-- implement or obtain from your project

        byte nodeId = 1; // Curtis node ID
        var pdoManager = new PdoManager(canBus, nodeId);

        // Configure RPDO1 to node's RPDO default COB-ID (standard 11-bit)
        uint rpdo1Id = 0x200u + nodeId; // 0x201 for node 1
        var rpdoConfig = new PdoConfiguration(pdoNumber: 1, cobId: rpdo1Id);
        pdoManager.ConfigureRPDO(rpdoConfig);

        Console.WriteLine($"Sending NMT Start to node {nodeId}...");
        // NMT Start frame: COB-ID = 0x000, data = [0x01, nodeId]
        await canBus.SendFrameAsync(0x000u, new byte[] { 0x01, nodeId }, CancellationToken.None);

        // Give node some time to come up
        await Task.Delay(250);

        // Safety checks: in real system check heartbeat/emcy/monitor faults before commanding.
        Console.WriteLine("Sending forward + throttle (20%)...");
        await pdoManager.SendRPDOAsync(1, new byte[] { 0x01, 20 }, CancellationToken.None);

        // Run for 1 second
        await Task.Delay(1000);

        Console.WriteLine("Stopping (0)...");
        await pdoManager.SendRPDOAsync(1, new byte[] { 0x00, 0x00 }, CancellationToken.None);

        Console.WriteLine("Done.");
        return 0;
    }

    static ICanBus CreateYourCanBus()
    {
        // Placeholder - you must return your concrete ICanBus implementation.
        // Example:
        // return new SocketCanBus("can0");
        throw new NotImplementedException("Replace CreateYourCanBus with your ICanBus implementation (SocketCanBus/PCAN/etc.).");
    }
}
