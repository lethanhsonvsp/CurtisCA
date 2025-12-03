namespace CANOpen.Enums;

public enum SdoCommand : byte
{
    DownloadInitiate = 0x20,
    DownloadSegment = 0x00,
    UploadInitiate = 0x40,
    UploadSegment = 0x60,
    Abort = 0x80,
    BlockDownload = 0xC0,
    BlockUpload = 0xA0
}
