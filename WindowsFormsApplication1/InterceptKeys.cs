using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using AppleKeyboard;
using System.Collections.Generic;

class InterceptKeys
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private static LowLevelKeyboardProc _proc = HookCallback;
    private static IntPtr _hookID = IntPtr.Zero;

    /*public static void Main()
    {
        _hookID = SetHook(_proc);
        Application.Run();
        UnhookWindowsHookEx(_hookID);
    }*/

    static Form1 form;

    static bool isBusy;

    public InterceptKeys(Form1 f)
    {
        _hookID = SetHook(_proc);
        form = f;
        isBusy = false;
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private delegate IntPtr LowLevelKeyboardProc(
        int nCode, IntPtr wParam, IntPtr lParam);

    private static IntPtr HookCallback(
        int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (!form.FnDown || isBusy) return CallNextHookEx(_hookID, nCode, wParam, lParam);

        int vkCode = Marshal.ReadInt32(lParam);

        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            //Console.WriteLine((Keys)vkCode);
            //form.label5.Text = vkCode.ToString() + "_down";

            isBusy = true;
            Keyboard.SimulateKeyStroke((char)vkCode, true);
            isBusy = false;
        }

        //form.label5.Text = ((Keys)vkCode).ToString();

        return (IntPtr)1;
    }
    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    public static extern short GetAsyncKeyState(int vkey);
    [DllImport("user32.dll", SetLastError = true)]
    static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

    public const int KEYEVENTF_EXTENDEDKEY = 0x0001; //Key down flag
    public const int KEYEVENTF_KEYUP = 0x0002; //Key up flag
    public const int VK_LCONTROL = 0xA2; //Left Control key code
    // import the function in your class
    [DllImport("User32.dll")]
    static extern int SetForegroundWindow(IntPtr point);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook,
        LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
        IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    static extern IntPtr GetMessageExtraInfo();

    public static class Keyboard
    {
        public static void SimulateKeyStroke(char key, bool ctrl = false, bool alt = false, bool shift = false)
        {
            List<ushort> keys = new List<ushort>();

            if (ctrl)
                keys.Add(VK_CONTROL);

            if (alt)
                keys.Add(VK_MENU);

            if (shift)
                keys.Add(VK_SHIFT);

            keys.Add(char.ToUpper(key));

            INPUT input = new INPUT();
            input.type = INPUT_KEYBOARD;
            int inputSize = Marshal.SizeOf(input);

            for (int i = 0; i < keys.Count; ++i)
            {
                input.mkhi.ki.wVk = keys[i];

                bool isKeyDown = (GetAsyncKeyState(keys[i]) & 0x10000) != 0;

                if (!isKeyDown)
                    SendInput(1, ref input, inputSize);
            }

            input.mkhi.ki.dwFlags = KEYEVENTF_KEYUP;
            for (int i = keys.Count - 1; i >= 0; --i)
            {
                input.mkhi.ki.wVk = keys[i];
                SendInput(1, ref input, inputSize);
            }
        }

        [DllImport("user32.dll")]
        static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(ushort vKey);

        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        struct HARDWAREINPUT
        {
            public int uMsg;
            public short wParamL;
            public short wParamH;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;

            [FieldOffset(0)]
            public KEYBDINPUT ki;

            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        struct INPUT
        {
            public int type;
            public MOUSEKEYBDHARDWAREINPUT mkhi;
        }

        const int INPUT_KEYBOARD = 1;
        const uint KEYEVENTF_KEYUP = 0x0002;

        const ushort VK_SHIFT = 0x10;
        const ushort VK_CONTROL = 0x11;
        const ushort VK_MENU = 0x12;

        public const ushort VK_DELETE = 0x2E;
    }

}