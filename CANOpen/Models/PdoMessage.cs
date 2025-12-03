using System;
using System.Collections.Generic;
using System.Linq;
using CANOpen.Enums;

namespace CANOpen.Models;

/// <summary>
/// PDO Mapping entry (đại diện cho 1 object trong PDO)
/// </summary>
public readonly struct PdoMapping
{
    public ushort Index { get; init; }
    public byte SubIndex { get; init; }
    public byte BitLength { get; init; }

    public PdoMapping(ushort index, byte subIndex, byte bitLength)
    {
        Index = index;
        SubIndex = subIndex;
        BitLength = bitLength;
    }

    /// <summary>
    /// Convert sang mapping value format (theo DS301)
    /// Format: [Index:16][SubIndex:8][BitLength:8]
    /// </summary>
    public uint ToMappingValue()
    {
        return ((uint)Index << 16) | ((uint)SubIndex << 8) | BitLength;
    }

    public static PdoMapping FromMappingValue(uint mappingValue)
    {
        ushort index = (ushort)((mappingValue >> 16) & 0xFFFF);
        byte subIndex = (byte)((mappingValue >> 8) & 0xFF);
        byte bitLength = (byte)(mappingValue & 0xFF);

        return new PdoMapping(index, subIndex, bitLength);
    }
}

/// <summary>
/// PDO Configuration (TPDO hoặc RPDO)
/// Note: Adjusted IsValid to reflect standard 11-bit CAN IDs (Curtis).
/// </summary>
public class PdoConfiguration
{
    private readonly List<PdoMapping> _mappings = new();

    public PdoConfiguration() { }

    public PdoConfiguration(byte pdoNumber, uint cobId, PdoTransmissionType transmissionType = PdoTransmissionType.Asynchronous)
    {
        if (pdoNumber < 1 || pdoNumber > 4)
            throw new ArgumentException("PDO number must be between 1 and 4", nameof(pdoNumber));

        PdoNumber = pdoNumber;
        CobId = cobId;
        TransmissionType = transmissionType;
    }

    public byte PdoNumber { get; set; }
    public uint CobId { get; set; }
    public PdoTransmissionType TransmissionType { get; set; }
    public ushort InhibitTime { get; set; }
    public ushort EventTimer { get; set; }
    public IReadOnlyList<PdoMapping> Mappings => _mappings.AsReadOnly();

    /// <summary>
    /// Curtis uses standard 11-bit IDs. Validate accordingly.
    /// </summary>
    public bool IsValid => CobId <= 0x7FFu;

    /// <summary>
    /// For our implementation, RTT/RTR is not used with Curtis. Keep property for compatibility.
    /// </summary>
    public bool RtrAllowed => false;

    public int TotalMappedBits => _mappings.Sum(m => m.BitLength);

    public bool IsMappingValid => TotalMappedBits <= 64;

    public void AddMapping(PdoMapping mapping)
    {
        if (mapping.BitLength == 0 || mapping.BitLength > 64)
            throw new ArgumentException("BitLength must be between 1 and 64", nameof(mapping));

        if (TotalMappedBits + mapping.BitLength > 64)
            throw new InvalidOperationException($"Adding this mapping would exceed 64 bits limit. Current: {TotalMappedBits}, Adding: {mapping.BitLength}");

        _mappings.Add(mapping);
    }

    public bool RemoveMapping(PdoMapping mapping) => _mappings.Remove(mapping);
    public void ClearMappings() => _mappings.Clear();

    public ValidationResult ValidateConfiguration()
    {
        var errors = new List<string>();

        if (PdoNumber < 1 || PdoNumber > 4)
            errors.Add("PDO number must be between 1 and 4");

        if (!IsValid)
            errors.Add("PDO is disabled or invalid (COB-ID must be 11-bit standard)");

        if (!IsMappingValid)
            errors.Add($"Total mapped bits ({TotalMappedBits}) exceeds 64 bits limit");

        if (_mappings.Count == 0)
            errors.Add("No mappings configured");

        for (int i = 0; i < _mappings.Count; i++)
        {
            var m = _mappings[i];
            if (m.BitLength == 0) errors.Add($"Mapping #{i} has zero bit length");
        }

        return new ValidationResult(errors.Count == 0, errors);
    }
}

/// <summary>
/// PDO Data (received or to-send)
/// </summary>
public readonly struct PdoData(byte pdoNumber, uint cobId, byte[] data, DateTime timestamp)
{
    public byte PdoNumber { get; init; } = pdoNumber;
    public uint CobId { get; init; } = cobId;
    public byte[] Data { get; init; } = data;
    public DateTime Timestamp { get; init; } = timestamp;

    /// <summary>
    /// Extract một value từ PDO data theo mapping
    /// </summary>
    public T ExtractValue<T>(PdoMapping mapping) where T : struct
    {
        if (mapping.BitLength == 0 || mapping.BitLength > 64)
            throw new ArgumentException("BitLength must be 1-64");

        int bitOffset = 0;
        foreach (var _ in Enumerable.Range(0, mapping.SubIndex))
        {
            // Tính bit offset dựa trên các mappings trước đó
            // (simplified - trong thực tế cần biết tất cả mappings)
        }

        return ExtractValue<T>(bitOffset, mapping.BitLength);
    }

    /// <summary>
    /// Extract value tại bit offset cụ thể
    /// </summary>
    public T ExtractValue<T>(int bitOffset, int bitLength) where T : struct
    {
        ulong value = 0;
        int byteOffset = bitOffset / 8;
        int bitInByteOffset = bitOffset % 8;
        int bitsRead = 0;

        while (bitsRead < bitLength && byteOffset < Data.Length)
        {
            int bitsToRead = Math.Min(8 - bitInByteOffset, bitLength - bitsRead);
            byte mask = (byte)((1 << bitsToRead) - 1);
            byte byteValue = (byte)((Data[byteOffset] >> bitInByteOffset) & mask);

            value |= (ulong)byteValue << bitsRead;

            bitsRead += bitsToRead;
            byteOffset++;
            bitInByteOffset = 0;
        }

        // Convert to target type with sign extension if needed
        return ConvertValue<T>(value, bitLength);
    }

    private static T ConvertValue<T>(ulong value, int bitLength) where T : struct
    {
        var type = typeof(T);

        if (type == typeof(bool))
            return (T)(object)(value != 0);

        if (type == typeof(byte))
            return (T)(object)(byte)value;

        if (type == typeof(sbyte))
        {
            // Sign extension
            if (bitLength < 8 && (value & (1UL << (bitLength - 1))) != 0)
                value |= (ulong)((1L << bitLength) - 1) << bitLength;
            return (T)(object)(sbyte)value;
        }

        if (type == typeof(ushort))
            return (T)(object)(ushort)value;

        if (type == typeof(short))
        {
            if (bitLength < 16 && (value & (1UL << (bitLength - 1))) != 0)
                value |= (ulong)((1L << bitLength) - 1) << bitLength;
            return (T)(object)(short)value;
        }

        if (type == typeof(uint))
            return (T)(object)(uint)value;

        if (type == typeof(int))
        {
            if (bitLength < 32 && (value & (1UL << (bitLength - 1))) != 0)
                value |= (ulong)((1L << bitLength) - 1) << bitLength;
            return (T)(object)(int)value;
        }

        if (type == typeof(ulong))
            return (T)(object)value;

        if (type == typeof(long))
        {
            if (bitLength < 64 && (value & (1UL << (bitLength - 1))) != 0)
                value |= ulong.MaxValue << bitLength;
            return (T)(object)(long)value;
        }

        throw new NotSupportedException($"Type {type.Name} is not supported");
    }
}

