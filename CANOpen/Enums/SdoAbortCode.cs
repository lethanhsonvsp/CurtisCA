namespace CANOpen.Enums;

/// <summary>
/// SDO Abort Codes theo CANOpen DS301
/// </summary>
public enum SdoAbortCode : uint
{
    // Protocol Errors
    ToggleBitNotAlternated = 0x05030000,
    SdoProtocolTimedOut = 0x05040000,
    CommandSpecifierNotValidOrUnknown = 0x05040001,
    InvalidBlockSize = 0x05040002,
    InvalidSequenceNumber = 0x05040003,
    CrcError = 0x05040004,
    OutOfMemory = 0x05040005,
    UnsupportedAccessToAnObject = 0x06010000,
    AttemptToReadAWriteOnlyObject = 0x06010001,
    AttemptToWriteAReadOnlyObject = 0x06010002,
    
    // Object Dictionary Errors
    ObjectDoesNotExist = 0x06020000,
    ObjectCannotBeMappedToThePdo = 0x06040041,
    NumberAndLengthOfObjectsToBeExceed = 0x06040042,
    GeneralParameterIncompatibility = 0x06040043,
    GeneralInternalIncompatibility = 0x06040047,
    ObjectAccessFailedDueToHardwareError = 0x06060000,
    DataTypeMismatchLengthOfService = 0x06070010,
    DataTypeMismatchLengthOfServiceTooHigh = 0x06070012,
    DataTypeMismatchLengthOfServiceTooLow = 0x06070013,
    SubIndexDoesNotExist = 0x06090011,
    InvalidValueForParameter = 0x06090030,
    ValueOfParameterWrittenTooHigh = 0x06090031,
    ValueOfParameterWrittenTooLow = 0x06090032,
    MaximumLessMinimum = 0x06090036,
    ResourceNotAvailable = 0x060A0023,
    
    // General Errors
    GeneralError = 0x08000000,
    DataCannotBeTransferredOrStoredToApplication = 0x08000020,
    DataCannotBeTransferredLocalControl = 0x08000021,
    DataCannotBeTransferredDeviceState = 0x08000022,
    ObjectDictionaryDynamicGenerationFails = 0x08000023,
    NoDataAvailable = 0x08000024,
    
    // Manufacturer Specific (0x0000 0000 - 0x0000 FFFF không được sử dụng)
    // User Defined (0x2000 0000 - 0xFFFF FFFF)
    
    // Custom/Unknown
    Unknown = 0xFFFFFFFF
}

/// <summary>
/// Extension methods cho SdoAbortCode
/// </summary>
public static class SdoAbortCodeExtensions
{
    /// <summary>
    /// Get human readable description của abort code
    /// </summary>
    public static string GetDescription(this SdoAbortCode code)
    {
        return code switch
        {
            SdoAbortCode.ToggleBitNotAlternated => "Toggle bit not alternated",
            SdoAbortCode.SdoProtocolTimedOut => "SDO protocol timed out",
            SdoAbortCode.CommandSpecifierNotValidOrUnknown => "Command specifier not valid or unknown",
            SdoAbortCode.InvalidBlockSize => "Invalid block size",
            SdoAbortCode.InvalidSequenceNumber => "Invalid sequence number",
            SdoAbortCode.CrcError => "CRC error",
            SdoAbortCode.OutOfMemory => "Out of memory",
            SdoAbortCode.UnsupportedAccessToAnObject => "Unsupported access to an object",
            SdoAbortCode.AttemptToReadAWriteOnlyObject => "Attempt to read a write-only object",
            SdoAbortCode.AttemptToWriteAReadOnlyObject => "Attempt to write a read-only object",
            SdoAbortCode.ObjectDoesNotExist => "Object does not exist in the object dictionary",
            SdoAbortCode.ObjectCannotBeMappedToThePdo => "Object cannot be mapped to the PDO",
            SdoAbortCode.NumberAndLengthOfObjectsToBeExceed => "Number and length of the objects to be mapped would exceed PDO length",
            SdoAbortCode.GeneralParameterIncompatibility => "General parameter incompatibility reason",
            SdoAbortCode.GeneralInternalIncompatibility => "General internal incompatibility in the device",
            SdoAbortCode.ObjectAccessFailedDueToHardwareError => "Access failed due to hardware error",
            SdoAbortCode.DataTypeMismatchLengthOfService => "Data type does not match, length of service parameter does not match",
            SdoAbortCode.DataTypeMismatchLengthOfServiceTooHigh => "Data type does not match, length of service parameter too high",
            SdoAbortCode.DataTypeMismatchLengthOfServiceTooLow => "Data type does not match, length of service parameter too low",
            SdoAbortCode.SubIndexDoesNotExist => "Sub-index does not exist",
            SdoAbortCode.InvalidValueForParameter => "Invalid value for parameter (download only)",
            SdoAbortCode.ValueOfParameterWrittenTooHigh => "Value of parameter written too high (download only)",
            SdoAbortCode.ValueOfParameterWrittenTooLow => "Value of parameter written too low (download only)",
            SdoAbortCode.MaximumLessMinimum => "Maximum value is less than minimum value",
            SdoAbortCode.ResourceNotAvailable => "Resource not available: SDO connection",
            SdoAbortCode.GeneralError => "General error",
            SdoAbortCode.DataCannotBeTransferredOrStoredToApplication => "Data cannot be transferred or stored to the application",
            SdoAbortCode.DataCannotBeTransferredLocalControl => "Data cannot be transferred or stored to the application because of local control",
            SdoAbortCode.DataCannotBeTransferredDeviceState => "Data cannot be transferred or stored to the application because of the present device state",
            SdoAbortCode.ObjectDictionaryDynamicGenerationFails => "Object dictionary dynamic generation fails or no object dictionary is present",
            SdoAbortCode.NoDataAvailable => "No data available",
            SdoAbortCode.Unknown => "Unknown error",
            _ => $"SDO Abort Code: 0x{(uint)code:X8}"
        };
    }
    
    /// <summary>
    /// Check if abort code is manufacturer specific
    /// </summary>
    public static bool IsManufacturerSpecific(this SdoAbortCode code)
    {
        uint value = (uint)code;
        return value >= 0x20000000 && value <= 0xFFFFFFFF;
    }
    
    /// <summary>
    /// Check if abort code is protocol error
    /// </summary>
    public static bool IsProtocolError(this SdoAbortCode code)
    {
        uint value = (uint)code;
        return value >= 0x05030000 && value <= 0x05040005;
    }
    
    /// <summary>
    /// Check if abort code is object dictionary error
    /// </summary>
    public static bool IsObjectDictionaryError(this SdoAbortCode code)
    {
        uint value = (uint)code;
        return (value >= 0x06010000 && value <= 0x06010002) ||
               (value >= 0x06020000 && value <= 0x060A0023);
    }
}