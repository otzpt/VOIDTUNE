using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace VOIDTUNE.WinUI.Services;

/// <summary>
/// Reads a file bypassing the Windows page cache, for benchmarks that must measure the device
/// rather than a cache hit on data this process just wrote. FILE_FLAG_NO_BUFFERING requires the
/// file offset, the read length and the buffer's memory address to be sector-aligned, so callers
/// read multiples of 4096 at 4096-aligned offsets into a buffer from <see cref="AllocateAligned"/>.
/// </summary>
internal static class UnbufferedIo
{
    private const int Alignment = 4096;
    private const uint GenericRead = 0x80000000;
    private const uint OpenExisting = 3;
    private const uint FileFlagNoBuffering = 0x20000000;
    private const uint FileFlagSequentialScan = 0x08000000;
    private const uint FileFlagRandomAccess = 0x10000000;

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFileW(string lpFileName, uint dwDesiredAccess,
        uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition,
        uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    public static FileStream Open(string path, bool sequential)
    {
        uint hint = sequential ? FileFlagSequentialScan : FileFlagRandomAccess;
        var handle = CreateFileW(path, GenericRead, 0, IntPtr.Zero, OpenExisting,
            FileFlagNoBuffering | hint, IntPtr.Zero);
        if (handle.IsInvalid) throw new IOException($"CreateFileW failed (0x{Marshal.GetLastWin32Error():X})");
        return new FileStream(handle, FileAccess.Read, bufferSize: 0);
    }

    /// <summary>Returns a pinned array and the index at which its 4096-aligned region starts.</summary>
    public static (byte[] Buffer, int Offset) AllocateAligned(int size)
    {
        var buffer = GC.AllocateUninitializedArray<byte>(size + Alignment, pinned: true);
        long address = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0).ToInt64();
        return (buffer, (int)((Alignment - address % Alignment) % Alignment));
    }
}
