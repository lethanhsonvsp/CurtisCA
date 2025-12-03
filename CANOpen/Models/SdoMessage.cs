using System;
using CANOpen.Enums;

namespace CANOpen.Models;

public readonly struct SdoRequest
{
    public byte CommandSpecifier { get; init; }
    public ushort Index { get; init; }
    public byte SubIndex { get; init; }
    public uint Data { get; init; }

    public SdoRequest(SdoCommand command, ushort index, byte subIndex, uint data = 0)
    {
        CommandSpecifier = (byte)command;
        Index = index;
        SubIndex = subIndex;
        Data = data;
    }

    public SdoRequest(SdoCommand command, ObjectDictionaryAddress address, uint data = 0)
        : this(command, address.Index, address.SubIndex, data)
    {
    }

    public byte[] ToBytes()
    {
        var bytes = new byte[8];
        bytes[0] = CommandSpecifier;
        bytes[1] = (byte)(Index & 0xFF);
        bytes[2] = (byte)((Index >> 8) & 0xFF);
        bytes[3] = SubIndex;
        bytes[4] = (byte)(Data & 0xFF);
        bytes[5] = (byte)((Data >> 8) & 0xFF);
        bytes[6] = (byte)((Data >> 16) & 0xFF);
        bytes[7] = (byte)((Data >> 24) & 0xFF);
        return bytes;
    }

    public static SdoRequest CreateUpload(ushort index, byte subIndex)
        => new(SdoCommand.UploadInitiate, index, subIndex);

    public static SdoRequest CreateDownload(ushort index, byte subIndex, byte[] data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (data.Length > 4)
            throw new ArgumentException("Expedited SDO transfer only supports up to 4 bytes");

        byte cs;
        // Expedited download: choose correct command specifier for 1,2,4 bytes
        if (data.Length == 1)
        {
            // expedited, size indicated, n = 3 (4 - 1 = 3)
            cs = (byte)((byte)SdoCommand.DownloadInitiate | 0x01); // using explicit encoding below
            // We'll build properly according to DS301:
            // CommandSpecifier byte format for expedited: b7..b5 = 001 (download init), b4..b2 = n (number of empty bytes), b1 = e, b0 = s
            // Simpler: compute per DS301:
            int n = 4 - data.Length;
            cs = (byte)((byte)SdoCommand.DownloadInitiate);
            cs |= (byte)((n << 2) & 0x0C); // bits 3..2
            cs |= 0x02; // expedited flag bit (e)
        }
        else if (data.Length == 2)
        {
            int n = 4 - data.Length;
            cs = (byte)((byte)SdoCommand.DownloadInitiate);
            cs |= (byte)((n << 2) & 0x0C);
            cs |= 0x02;
        }
        else // data.Length == 3 or 4 (we only allow up to 4)
        {
            int n = 4 - data.Length;
            cs = (byte)((byte)SdoCommand.DownloadInitiate);
            cs |= (byte)((n << 2) & 0x0C);
            cs |= 0x02;
        }

        uint dataValue = 0;
        for (int i = 0; i < data.Length; i++)
            dataValue |= (uint)(data[i] << (i * 8));

        // If less than 4 bytes, upper bytes remain zero.
        return new SdoRequest { CommandSpecifier = cs, Index = index, SubIndex = subIndex, Data = dataValue };
    }
}

public readonly struct SdoResponse
{
    public byte CommandSpecifier { get; init; }
    public ushort Index { get; init; }
    public byte SubIndex { get; init; }
    public uint Data { get; init; }

    public bool IsAbort => (CommandSpecifier & 0xE0) == (byte)SdoCommand.Abort;
    public SdoAbortCode AbortCode => (SdoAbortCode)Data;

    public static SdoResponse FromBytes(byte[] data)
    {
        if (data.Length < 8)
            throw new ArgumentException("SDO response must be 8 bytes");

        return new SdoResponse
        {
            CommandSpecifier = data[0],
            Index = (ushort)(data[1] | (data[2] << 8)),
            SubIndex = data[3],
            Data = (uint)(data[4] | (data[5] << 8) | (data[6] << 16) | (data[7] << 24))
        };
    }

    public byte[] GetDataBytes()
    {
        // If expedited (bit1 set), extract number of valid bytes from (CommandSpecifier >> 2) & 0x03
        if ((CommandSpecifier & 0x02) != 0)
        {
            int n = (CommandSpecifier >> 2) & 0x03;
            int dataLength = 4 - n;
            var bytes = new byte[dataLength];
            for (int i = 0; i < dataLength; i++)
                bytes[i] = (byte)((Data >> (i * 8)) & 0xFF);
            return bytes;
        }

        return BitConverter.GetBytes(Data);
    }
}
