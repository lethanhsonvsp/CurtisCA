namespace CANOpen.Models;

public readonly struct ObjectDictionaryAddress(ushort index, byte subIndex = 0)
{
    public ushort Index { get; init; } = index;
    public byte SubIndex { get; init; } = subIndex;

    public override string ToString() => $"0x{Index:X4}:{SubIndex:X2}";
    
    public static bool operator ==(ObjectDictionaryAddress left, ObjectDictionaryAddress right)
        => left.Index == right.Index && left.SubIndex == right.SubIndex;
    
    public static bool operator !=(ObjectDictionaryAddress left, ObjectDictionaryAddress right)
        => !(left == right);
    
    public override bool Equals(object? obj)
        => obj is ObjectDictionaryAddress address && this == address;
    
    public override int GetHashCode()
        => HashCode.Combine(Index, SubIndex);
}
