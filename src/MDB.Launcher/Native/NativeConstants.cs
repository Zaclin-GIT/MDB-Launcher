namespace MDB.Launcher.Native;

/// <summary>
/// Native Windows API constants used for DLL injection and process operations.
/// </summary>
internal static class NativeConstants
{
    // OpenProcess access rights
    internal const uint PROCESS_CREATE_THREAD = 0x0002;
    internal const uint PROCESS_VM_OPERATION = 0x0008;
    internal const uint PROCESS_VM_READ = 0x0010;
    internal const uint PROCESS_VM_WRITE = 0x0020;
    internal const uint PROCESS_QUERY_INFORMATION = 0x0400;

    internal const uint PROCESS_INJECTION_ACCESS =
        PROCESS_CREATE_THREAD |
        PROCESS_VM_OPERATION |
        PROCESS_VM_WRITE |
        PROCESS_VM_READ |
        PROCESS_QUERY_INFORMATION;

    // VirtualAllocEx allocation type
    internal const uint MEM_COMMIT = 0x00001000;
    internal const uint MEM_RESERVE = 0x00002000;
    internal const uint MEM_RELEASE = 0x00008000;

    // VirtualAllocEx protection
    internal const uint PAGE_READWRITE = 0x04;

    // WaitForSingleObject
    internal const uint INFINITE = 0xFFFFFFFF;
    internal const uint WAIT_OBJECT_0 = 0x00000000;
    internal const uint WAIT_TIMEOUT = 0x00000102;

    // CreateToolhelp32Snapshot flags
    internal const uint TH32CS_SNAPMODULE = 0x00000008;
    internal const uint TH32CS_SNAPMODULE32 = 0x00000010;

    // Invalid handle
    internal static readonly nint INVALID_HANDLE_VALUE = new(-1);
}
