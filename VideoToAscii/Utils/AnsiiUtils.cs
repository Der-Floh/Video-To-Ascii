using System.Runtime.InteropServices;

namespace VideoToAscii.Utils;

public static class AnsiiUtils
{
    public static void EnableAnsiSupport()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Assume ANSI is already supported on Unix-like systems (Linux, macOS)
            return;
        }

        const int STD_OUTPUT_HANDLE = -11;
        const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

        var handle = GetStdHandle(STD_OUTPUT_HANDLE);

        if (!GetConsoleMode(handle, out var mode))
        {
            return; // Couldn't get mode; likely not a real console
        }

        mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;

        // Try to set the new mode with VT processing enabled
        SetConsoleMode(handle, mode);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
}
