using System;

namespace LanExchange.Plugin.Windows.Enums
{
    // Describes the event that has occurred
    [Flags]
    public enum SHCNE : uint
    {
        RENAMEITEM = 0x00000001,
        CREATE = 0x00000002,
        DELETE = 0x00000004,
        MKDIR = 0x00000008,
        RMDIR = 0x00000010,
        MEDIAINSERTED = 0x00000020,
        MEDIAREMOVED = 0x00000040,
        DRIVEREMOVED = 0x00000080,
        DRIVEADD = 0x00000100,
        NETSHARE = 0x00000200,
        NETUNSHARE = 0x00000400,
        ATTRIBUTES = 0x00000800,
        UPDATEDIR = 0x00001000,
        UPDATEITEM = 0x00002000,
        SERVERDISCONNECT = 0x00004000,
        UPDATEIMAGE = 0x00008000,
        DRIVEADDGUI = 0x00010000,
        RENAMEFOLDER = 0x00020000,
        FREESPACE = 0x00040000,
        EXTENDED_EVENT = 0x04000000,
        ASSOCCHANGED = 0x08000000,
        DISKEVENTS = 0x0002381F,
        GLOBALEVENTS = 0x0C0581E0,
        ALLEVENTS = 0x7FFFFFFF,
        INTERRUPT = 0x80000000
    }
}