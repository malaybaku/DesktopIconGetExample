using System;
using System.Runtime.InteropServices;

namespace Baku.DesktopIconGetExample.Native
{
    public static class NativeMethods
    {
#if UNITY_5_5_OR_NEWER
        private const string Kernel32Dll = "kernel32";
        private const string User32Dll = "user32";
#else
        private const string Kernel32Dll = "kernel32.dll";
        private const string User32Dll = "user32.dll";
#endif

        [DllImport(Kernel32Dll)]
        public static extern IntPtr VirtualAllocEx(
            IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect
            );
        [DllImport(Kernel32Dll)]
        public static extern bool VirtualFreeEx(
            IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType
            );
        [DllImport(Kernel32Dll)]
        public static extern bool CloseHandle(IntPtr handle);
        [DllImport(Kernel32Dll)]
        public static extern bool WriteProcessMemory(
            IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer,
            int nSize, ref uint vNumberOfBytesRead
            );
        [DllImport(Kernel32Dll)]
        public static extern bool ReadProcessMemory(
            IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer,
            int nSize, ref uint vNumberOfBytesRead
            );
        [DllImport(Kernel32Dll)]
        public static extern IntPtr OpenProcess(
            uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId
            );
        [DllImport(User32Dll)]
        public static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);
        [DllImport(User32Dll)]
        public static extern IntPtr FindWindow(string lpszClass, string lpszWindow);
        [DllImport(User32Dll)]
        public static extern IntPtr FindWindowEx(
            IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow
            );
        [DllImport(User32Dll)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint dwProcessId);



        public const uint LVM_FIRST = 0x1000;
        public const uint LVM_GETITEMCOUNT = LVM_FIRST + 4;
        public const uint LVM_GETITEMW = LVM_FIRST + 75;
        public const uint LVM_GETITEMRECT = LVM_FIRST + 14;
        public const uint LVM_GETITEMPOSITION = LVM_FIRST + 16;
        public const int LVIF_TEXT = 0x0001;

        public const int LVIR_BOUNDS = 0;
        public const int LVIR_ICON = 1;
        public const int LVIR_LABEL = 2;
        public const int LVIR_SELECTBOUNDS = 3;

        public const uint PROCESS_VM_OPERATION = 0x0008;
        public const uint PROCESS_VM_READ = 0x0010;
        public const uint PROCESS_VM_WRITE = 0x0020;
        public const uint MEM_COMMIT = 0x1000;
        public const uint MEM_RELEASE = 0x8000;
        public const uint MEM_RESERVE = 0x2000;
        public const uint PAGE_READWRITE = 4;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LVITEM
    {
        public int mask;
        public int iItem;
        public int iSubItem;
        public int state;
        public int stateMask;
        public IntPtr pszText; // string
        public int cchTextMax;
        public int iImage;
        public IntPtr lParam;
        public int iIndent;
        public int iGroupId;
        public int cColumns;
        public IntPtr puColumns;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public override string ToString()
            => $"(L={Left}, T={Top}, W={Right - Left}, H={Bottom - Top}";
    }
}
