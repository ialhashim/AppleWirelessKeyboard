using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace AppleKeyboard
{
    public partial class Form1 : Form
    {
        Stream _stream;
        bool _FnDown;
        bool _EjectDown;

        //Properties
        public bool FnDown {
            get { return _FnDown;}
            set
            {
                _FnDown = value;
                //this.label1.Text = _FnDown.ToString();
            }
        }
        public bool EjectDown { get { return _EjectDown; } set { 
            _EjectDown = value;
            //this.label2.Text = _EjectDown.ToString();

            if(!_EjectDown)
            {
                InterceptKeys.Keyboard.SimulateKeyStroke((char)InterceptKeys.Keyboard.VK_DELETE);
            }
        }}
        public bool PowerButtonDown { get; set; }
        public bool Registered
        {
            get { return _stream != null; }
        }

        public Form1()
        {
            InitializeComponent();

            InterceptKeys k = new InterceptKeys(this);

            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false; // This is optional

            Opacity = 0;

            starting();
        }


        internal class KeyDownMessageFilter : IMessageFilter
        {
            public const int WM_KEYDOWN = 0x0100;
            public const int WM_KEYUP = 0x0101;

            static Form1 form;
            public KeyDownMessageFilter(Form1 f)
            {
                form = f;
            }

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg == WM_KEYDOWN)
                {
                    var k = (Keys)m.WParam;
                    var c = (char)k;
                    // and some other stuff

                    //form.label5.Text = k.ToString() + "_down";
                }
                if(m.Msg == WM_KEYUP)
                {
                    var k = (Keys)m.WParam;
                    var c = (char)k;
                    // and some other stuff

                    //form.label5.Text = k.ToString() + "_up";
                }
                return false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            starting();
        }

        private void starting()
        {
            Guid guid; 
            SP_DEVICE_INTERFACE_DATA sp_device_interface_data = new SP_DEVICE_INTERFACE_DATA() { cbSize = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA)) };

            HidD_GetHidGuid(out guid);
            IntPtr hDevInfo = SetupDiGetClassDevs(ref guid, null, IntPtr.Zero, 0x10);

            int num = 0;
            while (SetupDiEnumDeviceInterfaces(hDevInfo, IntPtr.Zero, ref guid, num++, ref sp_device_interface_data))
            {
                uint num2;
                SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData = new SP_DEVICE_INTERFACE_DETAIL_DATA { cbSize = (IntPtr.Size == 8) ? (uint)8 : (uint)5 };

                SetupDiGetDeviceInterfaceDetail(hDevInfo, ref sp_device_interface_data, IntPtr.Zero, 0, out num2, IntPtr.Zero);
                if (SetupDiGetDeviceInterfaceDetail(hDevInfo, ref sp_device_interface_data, ref deviceInterfaceDetailData, num2, out num2, IntPtr.Zero))
                {
                    HIDD_ATTRIBUTES hidd_attributes = new HIDD_ATTRIBUTES() { Size = Marshal.SizeOf(typeof(HIDD_ATTRIBUTES)) };

                    var xxx = deviceInterfaceDetailData.DevicePath;
                    SafeFileHandle handle = CreateFile(xxx, FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, EFileAttributes.Overlapped, IntPtr.Zero);

                    if (HidD_GetAttributes(handle.DangerousGetHandle(), ref hidd_attributes))
                    {
                        _stream = new FileStream(handle, FileAccess.ReadWrite, 0x16, true);
                    }
                }
            }

            SpecialKeyStateChanged();


        }

        private async void SpecialKeyStateChanged()
        {
            byte[] buffer = new byte[0x16];
            //_stream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(SpecialKeyStateChanged), buffer);

            int numBytesRead = await _stream.ReadAsync(buffer, 0, 0x16);

            if (numBytesRead > 0)
            {
                var asyncState = buffer;

                // Original code to handle the wireless keyboard left completely intact.

                if (asyncState[0] == 0x11)
                {
                    switch (asyncState[1])
                    {
                        case 24:
                            {
                                EjectDown = true;
                                FnDown = true;

                            }
                            break;
                        case 16:
                            {
                                if (EjectDown)
                                {
                                    EjectDown = false;
                                }
                                FnDown = true;
                            }
                            break;
                        case 8:
                            {
                                if (FnDown)
                                {
                                    FnDown = false;
                                }
                                EjectDown = true;
                            }
                            break;
                        case 0:
                            {
                                if (EjectDown)
                                {
                                    EjectDown = false;
                                }
                                if (FnDown)
                                {
                                    FnDown = false;
                                }
                            }
                            break;
                    }
                }
                else if (asyncState[0] == 0x13)
                {
                }
            }

            // Read again.
            if (numBytesRead > 0)
            {
                SpecialKeyStateChanged();
            }
        }

        public const int DIGCF_DEFAULT = 0x00000001; // only valid with DIGCF_DEVICEINTERFACE
        public const int DIGCF_PRESENT = 0x00000002;
        public const int DIGCF_ALLCLASSES = 0x00000004;
        public const int DIGCF_PROFILE = 0x00000008;
        public const int DIGCF_DEVICEINTERFACE = 0x00000010;

        [Flags]
        public enum EFileAttributes : uint
        {
            Readonly = 0x00000001,
            Hidden = 0x00000002,
            System = 0x00000004,
            Directory = 0x00000010,
            Archive = 0x00000020,
            Device = 0x00000040,
            Normal = 0x00000080,
            Temporary = 0x00000100,
            SparseFile = 0x00000200,
            ReparsePoint = 0x00000400,
            Compressed = 0x00000800,
            Offline = 0x00001000,
            NotContentIndexed = 0x00002000,
            Encrypted = 0x00004000,
            Write_Through = 0x80000000,
            Overlapped = 0x40000000,
            NoBuffering = 0x20000000,
            RandomAccess = 0x10000000,
            SequentialScan = 0x08000000,
            DeleteOnClose = 0x04000000,
            BackupSemantics = 0x02000000,
            PosixSemantics = 0x01000000,
            OpenReparsePoint = 0x00200000,
            OpenNoRecall = 0x00100000,
            FirstPipeInstance = 0x00080000
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVINFO_DATA
        {
            public uint cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public int Flags;
            public IntPtr RESERVED;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public UInt32 cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string DevicePath;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HIDD_ATTRIBUTES
        {
            public int Size;
            public short VendorID;
            public short ProductID;
            public short VersionNumber;
        }

        [DllImport(@"hid.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern void HidD_GetHidGuid(out Guid gHid);

        [DllImport("hid.dll")]
        public static extern Boolean HidD_GetAttributes(IntPtr HidDeviceObject, ref HIDD_ATTRIBUTES Attributes);

        [DllImport("hid.dll")]
        internal extern static bool HidD_SetOutputReport(
            IntPtr HidDeviceObject,
            byte[] lpReportBuffer,
            uint ReportBufferLength);

        [DllImport(@"setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetupDiGetClassDevs(
            ref Guid ClassGuid,
            [MarshalAs(UnmanagedType.LPTStr)] string Enumerator,
            IntPtr hwndParent,
            UInt32 Flags
            );

        [DllImport(@"setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern Boolean SetupDiEnumDeviceInterfaces(
            IntPtr hDevInfo,
            //ref SP_DEVINFO_DATA devInfo,
            IntPtr devInvo,
            ref Guid interfaceClassGuid,
            Int32 memberIndex,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData
        );

        [DllImport(@"setupapi.dll", SetLastError = true)]
        public static extern Boolean SetupDiGetDeviceInterfaceDetail(
            IntPtr hDevInfo,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
            IntPtr deviceInterfaceDetailData,
            UInt32 deviceInterfaceDetailDataSize,
            out UInt32 requiredSize,
            IntPtr deviceInfoData
        );

        [DllImport(@"setupapi.dll", SetLastError = true)]
        public static extern Boolean SetupDiGetDeviceInterfaceDetail(
            IntPtr hDevInfo,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
            ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData,
            UInt32 deviceInterfaceDetailDataSize,
            out UInt32 requiredSize,
            IntPtr deviceInfoData
        );

        [DllImport(@"setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern UInt16 SetupDiDestroyDeviceInfoList(IntPtr hDevInfo);

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern SafeFileHandle CreateFile(
            string fileName,
            [MarshalAs(UnmanagedType.U4)] FileAccess fileAccess,
            [MarshalAs(UnmanagedType.U4)] FileShare fileShare,
            IntPtr securityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
            [MarshalAs(UnmanagedType.U4)] EFileAttributes flags,
            IntPtr template);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetupDiGetDeviceRegistryProperty(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            uint Property,
            out UInt32 PropertyRegDataType,
            IntPtr PropertyBuffer,
            uint PropertyBufferSize,
            out UInt32 RequiredSize);

        public enum SPDRP
        {
            SPDRP_DEVICEDESC = 0x00000000,
            SPDRP_HARDWAREID = 0x00000001,
            SPDRP_COMPATIBLEIDS = 0x00000002,
            SPDRP_NTDEVICEPATHS = 0x00000003,
            SPDRP_SERVICE = 0x00000004,
            SPDRP_CONFIGURATION = 0x00000005,
            SPDRP_CONFIGURATIONVECTOR = 0x00000006,
            SPDRP_CLASS = 0x00000007,
            SPDRP_CLASSGUID = 0x00000008,
            SPDRP_DRIVER = 0x00000009,
            SPDRP_CONFIGFLAGS = 0x0000000A,
            SPDRP_MFG = 0x0000000B,
            SPDRP_FRIENDLYNAME = 0x0000000C,
            SPDRP_LOCATION_INFORMATION = 0x0000000D,
            SPDRP_PHYSICAL_DEVICE_OBJECT_NAME = 0x0000000E,
            SPDRP_CAPABILITIES = 0x0000000F,
            SPDRP_UI_NUMBER = 0x00000010,
            SPDRP_UPPERFILTERS = 0x00000011,
            SPDRP_LOWERFILTERS = 0x00000012,
            SPDRP_MAXIMUM_PROPERTY = 0x00000013,
        }

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet, uint MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

        public enum DataCommand : uint
        {
            RID_HEADER = 0x10000005, // Get the header information from the RAWINPUT structure.
            RID_INPUT = 0x10000003   // Get the raw data from the RAWINPUT structure.
        }

        public static class DeviceType
        {
            public const int RimTypemouse = 0;
            public const int RimTypekeyboard = 1;
            public const int RimTypeHid = 2;
        }

        internal enum RawInputDeviceInfo : uint
        {
            RIDI_DEVICENAME = 0x20000007,
            RIDI_DEVICEINFO = 0x2000000b,
            PREPARSEDDATA = 0x20000005
        }

        enum BroadcastDeviceType
        {
            DBT_DEVTYP_OEM = 0,
            DBT_DEVTYP_DEVNODE = 1,
            DBT_DEVTYP_VOLUME = 2,
            DBT_DEVTYP_PORT = 3,
            DBT_DEVTYP_NET = 4,
            DBT_DEVTYP_DEVICEINTERFACE = 5,
            DBT_DEVTYP_HANDLE = 6,
        }

        [DllImport("User32.dll", SetLastError = true)]
        internal static extern int GetRawInputData(IntPtr hRawInput, DataCommand command, [Out] out InputData buffer, [In, Out] ref int size, int cbSizeHeader);

        [DllImport("User32.dll", SetLastError = true)]
        internal static extern int GetRawInputData(IntPtr hRawInput, DataCommand command, [Out] IntPtr pData, [In, Out] ref int size, int sizeHeader);

        [DllImport("User32.dll", SetLastError = true)]
        internal static extern uint GetRawInputDeviceInfo(IntPtr hDevice, RawInputDeviceInfo command, IntPtr pData, ref uint size);



        [DllImport("User32.dll", SetLastError = true)]
        internal static extern uint GetRawInputDeviceList(IntPtr pRawInputDeviceList, ref uint numberDevices, uint size);


        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool UnregisterDeviceNotification(IntPtr handle);
        [StructLayout(LayoutKind.Sequential)]
        internal struct Rawinputdevicelist
        {
            public IntPtr hDevice;
            public uint dwType;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct RawData
        {
            [FieldOffset(0)]
            internal Rawmouse mouse;
            [FieldOffset(0)]
            internal Rawkeyboard keyboard;
            [FieldOffset(0)]
            internal Rawhid hid;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct InputData
        {
            public Rawinputheader header;           // 64 bit header size is 24  32 bit the header size is 16
            public RawData data;                    // Creating the rest in a struct allows the header size to align correctly for 32 or 64 bit
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rawinputheader
        {
            public uint dwType;                     // Type of raw input (RIM_TYPEHID 2, RIM_TYPEKEYBOARD 1, RIM_TYPEMOUSE 0)
            public uint dwSize;                     // Size in bytes of the entire input packet of data. This includes RAWINPUT plus possible extra input reports in the RAWHID variable length array. 
            public IntPtr hDevice;                  // A handle to the device generating the raw input data. 
            public IntPtr wParam;                   // RIM_INPUT 0 if input occurred while application was in the foreground else RIM_INPUTSINK 1 if it was not.

            public override string ToString()
            {
                return string.Format("RawInputHeader\n dwType : {0}\n dwSize : {1}\n hDevice : {2}\n wParam : {3}", dwType, dwSize, hDevice, wParam);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Rawhid
        {
            public uint dwSizHid;
            public uint dwCount;
            public byte bRawData;

            public override string ToString()
            {
                return string.Format("Rawhib\n dwSizeHid : {0}\n dwCount : {1}\n bRawData : {2}\n", dwSizHid, dwCount, bRawData);
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct Rawmouse
        {
            [FieldOffset(0)]
            public ushort usFlags;
            [FieldOffset(4)]
            public uint ulButtons;
            [FieldOffset(4)]
            public ushort usButtonFlags;
            [FieldOffset(6)]
            public ushort usButtonData;
            [FieldOffset(8)]
            public uint ulRawButtons;
            [FieldOffset(12)]
            public int lLastX;
            [FieldOffset(16)]
            public int lLastY;
            [FieldOffset(20)]
            public uint ulExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Rawkeyboard
        {
            public ushort Makecode;                 // Scan code from the key depression
            public ushort Flags;                    // One or more of RI_KEY_MAKE, RI_KEY_BREAK, RI_KEY_E0, RI_KEY_E1
            public ushort Reserved;                 // Always 0    
            public ushort VKey;                     // Virtual Key Code
            public uint Message;                    // Corresponding Windows message for exmaple (WM_KEYDOWN, WM_SYASKEYDOWN etc)
            public uint ExtraInformation;           // The device-specific addition information for the event (seems to always be zero for keyboards)

            public override string ToString()
            {
                return string.Format("Rawkeyboard\n Makecode: {0}\n Makecode(hex) : {0:X}\n Flags: {1}\n Reserved: {2}\n VKeyName: {3}\n Message: {4}\n ExtraInformation {5}\n",
                                                    Makecode, Flags, Reserved, VKey, Message, ExtraInformation);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RawInputDevice
        {
            internal HidUsagePage UsagePage;
            internal HidUsage Usage;
            internal RawInputDeviceFlags Flags;
            internal IntPtr Target;

            public override string ToString()
            {
                return string.Format("{0}/{1}, flags: {2}, target: {3}", UsagePage, Usage, Flags, Target);
            }
        }

        public enum HidUsagePage : ushort
        {
            /// <summary>Unknown usage page.</summary>
            UNDEFINED = 0x00,
            /// <summary>Generic desktop controls.</summary>
            GENERIC = 0x01,
            /// <summary>Simulation controls.</summary>
            SIMULATION = 0x02,
            /// <summary>Virtual reality controls.</summary>
            VR = 0x03,
            /// <summary>Sports controls.</summary>
            SPORT = 0x04,
            /// <summary>Games controls.</summary>
            GAME = 0x05,
            /// <summary>Keyboard controls.</summary>
            KEYBOARD = 0x07,
        }

        public enum HidUsage : ushort
        {
            /// <summary>Unknown usage.</summary>
            Undefined = 0x00,
            /// <summary>Pointer</summary>
            Pointer = 0x01,
            /// <summary>Mouse</summary>
            Mouse = 0x02,
            /// <summary>Joystick</summary>
            Joystick = 0x04,
            /// <summary>Game Pad</summary>
            Gamepad = 0x05,
            /// <summary>Keyboard</summary>
            Keyboard = 0x06,
            /// <summary>Keypad</summary>
            Keypad = 0x07,
            /// <summary>Muilt-axis Controller</summary>
            SystemControl = 0x80,
            /// <summary>Tablet PC controls</summary>
            Tablet = 0x80,
            /// <summary>Consumer</summary>
            Consumer = 0x0C,
        }

        [Flags]
        internal enum RawInputDeviceFlags
        {
            /// <summary>No flags.</summary>
            NONE = 0,
            /// <summary>If set, this removes the top level collection from the inclusion list. This tells the operating system to stop reading from a device which matches the top level collection.</summary>
            REMOVE = 0x00000001,
            /// <summary>If set, this specifies the top level collections to exclude when reading a complete usage page. This flag only affects a TLC whose usage page is already specified with PageOnly.</summary>
            EXCLUDE = 0x00000010,
            /// <summary>If set, this specifies all devices whose top level collection is from the specified UsagePage. Note that Usage must be zero. To exclude a particular top level collection, use Exclude.</summary>
            PAGEONLY = 0x00000020,
            /// <summary>If set, this prevents any devices specified by UsagePage or Usage from generating legacy messages. This is only for the mouse and keyboard.</summary>
            NOLEGACY = 0x00000030,
            /// <summary>If set, this enables the caller to receive the input even when the caller is not in the foreground. Note that WindowHandle must be specified.</summary>
            INPUTSINK = 0x00000100,
            /// <summary>If set, the mouse button click does not activate the other window.</summary>
            CAPTUREMOUSE = 0x00000200,
            /// <summary>If set, the application-defined keyboard device hotkeys are not handled. However, the system hotkeys; for example, ALT+TAB and CTRL+ALT+DEL, are still handled. By default, all keyboard hotkeys are handled. NoHotKeys can be specified even if NoLegacy is not specified and WindowHandle is NULL.</summary>
            NOHOTKEYS = 0x00000200,
            /// <summary>If set, application keys are handled.  NoLegacy must be specified.  Keyboard only.</summary>
            APPKEYS = 0x00000400,
            /// If set, this enables the caller to receive input in the background only if the foreground application
            /// does not process it. In other words, if the foreground application is not registered for raw input,
            /// then the background application that is registered will receive the input.
            /// </summary>
            EXINPUTSINK = 0x00001000,
            DEVNOTIFY = 0x00002000
        }
    }
}
