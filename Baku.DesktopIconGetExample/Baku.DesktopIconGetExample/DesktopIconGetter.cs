using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Baku.DesktopIconGetExample.Native;

namespace Baku.DesktopIconGetExample
{

    public class DesktopIconGetter : IDisposable
    {
        public DesktopIconGetter()
        {
            //RAIIっぽくしたいのでこの時点でリソース取りに行く。usingステートメントとの相性も考慮している
            Initialize();
        }

        public bool IsValid { get; private set; }
        public int ItemCount { get; private set; }

        public void Initialize()
        {
            if (IsValid)
            {
                return;
            }

            // get the handle of the desktop listview
            IntPtr hWnd = NativeMethods.FindWindow("Progman", "Program Manager");
            hWnd = NativeMethods.FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null);
            hWnd = NativeMethods.FindWindowEx(hWnd, IntPtr.Zero, "SysListView32", "FolderView");
            _windowHandle = hWnd;

            NativeMethods.GetWindowThreadProcessId(hWnd, out uint vProcessId);

            IntPtr handleProcess = NativeMethods.OpenProcess(
                NativeMethods.PROCESS_VM_OPERATION |
                NativeMethods.PROCESS_VM_READ |
                NativeMethods.PROCESS_VM_WRITE,
                false,
                vProcessId
                );

            if (handleProcess != IntPtr.Zero)
            {
                _processHandle = handleProcess;
                _virtualMemoryHandle = NativeMethods.VirtualAllocEx(
                    handleProcess,
                    IntPtr.Zero,
                    4096,
                    NativeMethods.MEM_RESERVE | NativeMethods.MEM_COMMIT, NativeMethods.PAGE_READWRITE
                    );
                IsValid = true;
            }

            UpdateCount();
        }
        public void UpdateCount()
        {
            if (!IsValid)
            {
                Initialize();
            }

            ItemCount = NativeMethods.SendMessage(_windowHandle, NativeMethods.LVM_GETITEMCOUNT, 0, 0);
        }

        private IntPtr _processHandle = IntPtr.Zero;
        private IntPtr _windowHandle = IntPtr.Zero;
        private IntPtr _virtualMemoryHandle = IntPtr.Zero;

        public IntPtr ProcessHandle => _processHandle;
        public IntPtr WindowHandle => _windowHandle;
        public IntPtr VirtualMemoryHandle => _virtualMemoryHandle;
        public IntPtr VirtualMemoryAfterLvItemHandle
            => (VirtualMemoryHandle == IntPtr.Zero) ?
            IntPtr.Zero :
            (IntPtr)((int)VirtualMemoryHandle + Marshal.SizeOf<LVITEM>());

        public void Dispose()
        {
            NativeMethods.VirtualFreeEx(_processHandle, _virtualMemoryHandle, 0, NativeMethods.MEM_RELEASE);
            NativeMethods.CloseHandle(_processHandle);
            _processHandle = IntPtr.Zero;
            _windowHandle = IntPtr.Zero;
            _virtualMemoryHandle = IntPtr.Zero;
            IsValid = false;
        }

        public IEnumerable<DesktopIconInfo> GetAllIconInfo()
        {
            return GetAllIconInfo(true);
        }

        public IEnumerable<DesktopIconInfo> GetAllIconInfo(bool checkCount)
        {
            if (checkCount)
            {
                UpdateCount();
            }

            if (ItemCount == 0)
            {
                return Enumerable.Empty<DesktopIconInfo>();
            }

            //遅延評価したくない+パフォーマンス下がると嫌なので配列使用
            var result = new DesktopIconInfo[ItemCount];
            for (int i = 0; i < result.Length; i++)
            {
                var bound = GetIconBoundAt(i);
                result[i] = new DesktopIconInfo(
                    GetIconNameAt(i),
                    bound.Left,
                    bound.Top,
                    bound.Right,
                    bound.Bottom
                    );
            }
            return result;
        }

        private string GetIconNameAt(int i)
        {
            uint vNumberOfBytesRead = 0;
            var iconNameBytes = new byte[256];

            //前半. アイコン名の出力先アドレスを指定
            //配列形式にする理由はMarshalのAPIに渡すうえで都合いいから
            var lvItems = new LVITEM[]
            {
                new LVITEM()
                {
                    mask = NativeMethods.LVIF_TEXT,
                    iItem = i,
                    iSubItem = 0,
                    cchTextMax = iconNameBytes.Length,
                    pszText = VirtualMemoryAfterLvItemHandle
                }
            };
            NativeMethods.WriteProcessMemory(
                ProcessHandle,
                VirtualMemoryHandle,
                Marshal.UnsafeAddrOfPinnedArrayElement(lvItems, 0),
                Marshal.SizeOf<LVITEM>(),
                ref vNumberOfBytesRead
                );

            //後半. 指定したアドレスにアイコン名が書き込まれたハズなので取得
            NativeMethods.SendMessage(
                WindowHandle,
                NativeMethods.LVM_GETITEMW,
                i,
                VirtualMemoryHandle.ToInt32()
                );
            NativeMethods.ReadProcessMemory(
                ProcessHandle,
                VirtualMemoryAfterLvItemHandle,
                Marshal.UnsafeAddrOfPinnedArrayElement(iconNameBytes, 0),
                iconNameBytes.Length,
                ref vNumberOfBytesRead
                );

            //そのままだとnull終端になってない(固定長で256バイトとってるせい)ので受け取った後でトリムする(ちょっと効率悪いが)
            string result = Encoding
                .Unicode
                .GetString(iconNameBytes, 0, (int)vNumberOfBytesRead)
                .TrimEnd('\0');

            var clear = new byte[iconNameBytes.Length];
            NativeMethods.WriteProcessMemory(
                   ProcessHandle,
                   VirtualMemoryAfterLvItemHandle,
                   Marshal.UnsafeAddrOfPinnedArrayElement(clear, 0),
                   clear.Length,
                   ref vNumberOfBytesRead
                   );

            return result;
        }

        private Rect GetIconBoundAt(int i)
        {
            //使わない
            uint numOfBytesRead = 0;

            //前半. Boundの種類を指定して渡す
            var rects = new Rect[]
            {
                new Rect()
                {
                    Left =NativeMethods.LVIR_SELECTBOUNDS
                }
            };
            NativeMethods.WriteProcessMemory(
                ProcessHandle,
                VirtualMemoryHandle,
                Marshal.UnsafeAddrOfPinnedArrayElement(rects, 0),
                Marshal.SizeOf<Rect>(),
                ref numOfBytesRead
                );

            //後半. 指定したBoundを出力させて読み取る
            NativeMethods.SendMessage(
                WindowHandle,
                NativeMethods.LVM_GETITEMRECT,
                i,
                VirtualMemoryHandle.ToInt32());
            NativeMethods.ReadProcessMemory(
                ProcessHandle,
                VirtualMemoryHandle,
                Marshal.UnsafeAddrOfPinnedArrayElement(rects, 0),
                Marshal.SizeOf<Rect>(),
                ref numOfBytesRead
                );

            return rects[0];
        }


    }

    public class DesktopIconInfo
    {
        public DesktopIconInfo(string name, int left, int top, int right, int bottom)
        {
            Name = name;
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public string Name { get; }
        public int Left { get; }
        public int Top { get; }
        public int Right { get; }
        public int Bottom { get; }

        public int Width
        {
            get { return Right - Left; }
        }
        public int Height
        {
            get { return Bottom - Top; }
        }

    }

}
