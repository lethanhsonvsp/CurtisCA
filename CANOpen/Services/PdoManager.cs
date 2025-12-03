using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CANOpen.Enums;
using CANOpen.Interfaces;
using CANOpen.Models;

namespace CANOpen.Services
{
    /// <summary>
    /// PDO Manager để xử lý Transmit và Receive PDOs
    /// - Adjusted for Curtis 1232E: standard 11-bit CAN IDs only, no RTR requests.
    /// </summary>
    public class PdoManager
    {
        private readonly ICanBus _canBus;
        private readonly byte _nodeId;
        private readonly ConcurrentDictionary<byte, PdoConfiguration> _tpdoConfigs;
        private readonly ConcurrentDictionary<byte, PdoConfiguration> _rpdoConfigs;

        public event EventHandler<PdoReceivedEventArgs>? PdoReceived;

        public PdoManager(ICanBus canBus, byte nodeId)
        {
            _canBus = canBus ?? throw new ArgumentNullException(nameof(canBus));
            _nodeId = nodeId;
            _tpdoConfigs = new ConcurrentDictionary<byte, PdoConfiguration>();
            _rpdoConfigs = new ConcurrentDictionary<byte, PdoConfiguration>();

            _canBus.FrameReceived += OnFrameReceived;
        }

        public void ConfigureTPDO(PdoConfiguration config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _tpdoConfigs[config.PdoNumber] = config;
        }

        public void ConfigureRPDO(PdoConfiguration config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _rpdoConfigs[config.PdoNumber] = config;
        }

        /// <summary>
        /// Gửi RPDO (Receive PDO) đến device (Curtis expects standard 11-bit IDs like 0x200 + node)
        /// </summary>
        public async Task SendRPDOAsync(byte pdoNumber, byte[] data, CancellationToken ct = default)
        {
            if (!_rpdoConfigs.TryGetValue(pdoNumber, out var config))
                throw new InvalidOperationException($"RPDO {pdoNumber} is not configured");

            if (!config.IsValid)
                throw new InvalidOperationException($"RPDO {pdoNumber} is not valid (invalid COB-ID)");

            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length > 8)
                throw new ArgumentException("PDO data cannot exceed 8 bytes");

            // Ensure 11-bit standard CAN ID
            uint cobId = config.CobId & 0x7FFu;

            // Many CAN libraries require a flag for extended/standard.
            // Here we call the simple SendFrameAsync(id, data, ct).
            await _canBus.SendFrameAsync(cobId, data, ct);
        }

        /// <summary>
        /// Curtis 1232E does not use RTR TPDO requests in normal operation.
        /// Expose as NotSupported to avoid misuse.
        /// </summary>
        public Task RequestTPDOAsync(byte pdoNumber, CancellationToken ct = default)
        {
            throw new NotSupportedException("Curtis 1232E does not support TPDO RTR requests (RequestTPDOAsync is not supported).");
        }

        private void OnFrameReceived(object? sender, CanFrameReceivedEventArgs e)
        {
            // Check if this is a TPDO for this node
            foreach (var config in _tpdoConfigs.Values)
            {
                uint expectedCobId = config.CobId & 0x7FFu; // standard 11-bit
                if (e.CanId == expectedCobId && config.IsValid)
                {
                    var pdoData = new PdoData(config.PdoNumber, e.CanId, e.Data, e.Timestamp);
                    PdoReceived?.Invoke(this, new PdoReceivedEventArgs(pdoData, PdoType.Transmit));
                    return;
                }
            }
        }
    }

    public class PdoReceivedEventArgs : EventArgs
    {
        public PdoData Data { get; }
        public PdoType Type { get; }

        public PdoReceivedEventArgs(PdoData data, PdoType type)
        {
            Data = data;
            Type = type;
        }
    }
}
