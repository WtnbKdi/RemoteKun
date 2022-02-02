using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RemoteKunServer
{
    internal static class WindowsAPI
    {
        // マウスカーソルを移動させる為の関数
        [DllImport("USER32.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern void SetCursorPos(int X, int Y);

        // マウスイベントを発生させる為の関数
        [DllImport("USER32.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        // 入力を送信する関数 ホイール操作用
        [DllImport("user32.dll")]
        public extern static void SendInput(int nInputs, ref INPUT pInputs, int cbsize);

        [StructLayout(LayoutKind.Sequential)]
        public struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public int mouseData;
            public int dwFlags;
            public int time;
            public IntPtr dwExtraInfo;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public short wVk;
            public short wScan;
            public int dwFlags;
            public int time;
            public IntPtr dwExtraInfo;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct HARDWAREINPUT
        {
            public int uMsg;
            public short wParamL;
            public short wParamH;
        };

        [StructLayout(LayoutKind.Explicit)]
        public struct INPUT_UNION
        {
            [FieldOffset(0)] public MOUSEINPUT mouse;
            [FieldOffset(0)] public KEYBDINPUT keyboard;
            [FieldOffset(0)] public HARDWAREINPUT hardware;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public int type;
            public INPUT_UNION ui;
        };

        // 定数定義
        public const int INPUT_MOUSE = 0;
        public const int INPUT_KEYBOARD = 1;
        public const int INPUT_HARDWARE = 2;
        public const int MOUSEEVENTF_MOVE = 0x1;
        public const int MOUSEEVENTF_ABSOLUTE = 0x8000;
        public const int MOUSEEVENTF_LEFTDOWN = 0x2;
        public const int MOUSEEVENTF_LEFTUP = 0x4;
        public const int MOUSEEVENTF_RIGHTDOWN = 0x8;
        public const int MOUSEEVENTF_RIGHTUP = 0x10;
        public const int MOUSEEVENTF_MIDDLEDOWN = 0x20;
        public const int MOUSEEVENTF_MIDDLEUP = 0x40;
        public const int MOUSEEVENTF_WHEEL = 0x800;
        public const int WHEEL_DELTA = 120;
        public const int KEYEVENTF_KEYDOWN = 0x0;
        public const int KEYEVENTF_KEYUP = 0x2;
        public const int KEYEVENTF_EXTENDEDKEY = 0x1;

        // クリックイベント
        public static class MouseEvent
        {
            public static readonly int MOUSEEVENTF_LEFTDOWN = 0x0002;
            public static readonly int MOUSEEVENTF_LEFTUP = 0x0004;
            public static readonly int MOUSEEVENTF_RIGHTDOWN = 0x0008;
            public static readonly int MOUSEEVENTF_RIGHTUP = 0x0010;
        }
    }
}
