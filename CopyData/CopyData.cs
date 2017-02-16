using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace CopyDataExtensions
{
    enum MessageTypes
    {
        WM_COPYDATA = 0x4A
    }

    public class CopyData
    {
        public event EventHandler OnDataReceived;
        Window listeningWindow = null;

        #region user32.dll Specifichandlers

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SendMessage(IntPtr hwnd, int wMsg, int wParam, ref COPYDATASTRUCT lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        // Delegate to filter which windows to include 
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        /// <summary> Find all windows that contain the given title text </summary>
        /// <param name="titleText"> The text that the window title must contain. </param>
        public static IEnumerable<IntPtr> FindWindowsByTitle(string titleText)
        {
            return FindWindows(delegate(IntPtr wnd, IntPtr param)
            {
                return GetWindowText(wnd).ToLower().Contains(titleText.ToLower());
            });
        }

        /// <summary> Find all windows that match the given filter </summary>
        /// <param name="filter"> A delegate that returns true for windows
        ///    that should be returned and false for windows that should
        ///    not be returned </param>
        public static IEnumerable<IntPtr> FindWindows(EnumWindowsProc filter)
        {
            IntPtr found = IntPtr.Zero;
            List<IntPtr> windows = new List<IntPtr>();

            EnumWindows(delegate(IntPtr wnd, IntPtr param)
            {
                if (filter(wnd, param))
                {
                    // only add the windows that pass the filter
                    windows.Add(wnd);
                }

                // but return true here so that we iterate all windows
                return true;
            }, IntPtr.Zero);

            return windows;
        }

        /// <summary> Get the text for the window pointed to by hWnd </summary>
        public static string GetWindowText(IntPtr hWnd)
        {
            int size = GetWindowTextLength(hWnd);
            if (size > 0)
            {
                var builder = new StringBuilder(size + 1);
                GetWindowText(hWnd, builder, builder.Capacity);
                return builder.ToString();
            }

            return String.Empty;
        }
        #endregion

        #region Constructors
        public CopyData(Window listeningWindow, bool startListen = true)
        {
            if (listeningWindow == null)
                throw new ArgumentNullException("listeningWindow", "The listening window cannot be null");

            this.listeningWindow = listeningWindow;

            this.listeningWindow.Closing += listeningWindow_Closing;
            if (startListen)
                StartListening();
        }
        #endregion

        #region Private Methods
        void listeningWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                StopListening();
            }
            finally { }
        }

        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // address the messages you are receiving using msg, wParam, lParam
            switch (msg)
            {
                case (int)MessageTypes.WM_COPYDATA:
                    if (OnDataReceived != null)
                    {
                        COPYDATASTRUCT CD = (COPYDATASTRUCT)Marshal.PtrToStructure(lParam, (typeof(COPYDATASTRUCT)));
                        byte[] B = new byte[CD.cbData];
                        IntPtr lpData = CD.lpData;
                        Marshal.Copy(lpData, B, 0, CD.cbData);
                        string message = Encoding.Unicode.GetString(B);
                        message = message.Length >= 1 ? message.Remove(message.Length - 1) : message;
                        OnDataReceived(message, EventArgs.Empty);
                    }
                    break;

            }

            return IntPtr.Zero;
        }

        private CopyStatus SendMessage(IntPtr hTargetWnd, string sMsg, MessageTypes messageType = MessageTypes.WM_COPYDATA)
        {
            if (hTargetWnd == IntPtr.Zero)
                return CopyStatus.WindowNotFound;

            try
            {
                COPYDATASTRUCT cds = new COPYDATASTRUCT();
                cds.dwData = (IntPtr)1;
                cds.cbData = sMsg.Length * 2 + 1;
                cds.lpData = Marshal.StringToHGlobalAuto(sMsg);


                NativeMethod.SendMessage(hTargetWnd, (int)messageType, IntPtr.Zero, ref cds);

                Marshal.FreeCoTaskMem(cds.lpData);

                int result = Marshal.GetLastWin32Error();
                if (result != 0)
                {
                    return CopyStatus.Fail;
                }
            }
            catch
            {
                return CopyStatus.Fail;
            }

            return CopyStatus.Success;
        }

        #endregion

        #region Public Methods

        public void StartListening()
        {

            IntPtr windowHandle = (new WindowInteropHelper(listeningWindow)).Handle;
            if (windowHandle == IntPtr.Zero)
                throw new InvalidOperationException("The listening window cannot be found. Make this call from the 'Loaded' event instead of the constructor, since the window is not actualy created yet in constructor.");

            HwndSource src = HwndSource.FromHwnd(windowHandle);
            src.AddHook(new HwndSourceHook(WndProc));

        }

        public void StopListening()
        {
            IntPtr windowHandle = (new WindowInteropHelper(listeningWindow)).Handle;
            if (windowHandle == IntPtr.Zero)
                throw new InvalidOperationException("The listening window cannot be found.");

            HwndSource src = HwndSource.FromHwnd(windowHandle);
            src.RemoveHook(new HwndSourceHook(this.WndProc));
        }

        public CopyStatus SendMessage(string sMsg, string windowName)
        {
            IntPtr hTargetWnd = IntPtr.Zero;
            var windows = FindWindowsByTitle(windowName);
            switch (windows.Count())
            {
                case 0:
                    return CopyStatus.WindowNotFound;
                case 1:
                    hTargetWnd = windows.ElementAt(0);
                    break;
                default:
                    return CopyStatus.TooManyWindowsFound;
            }

            return SendMessage(hTargetWnd, sMsg);
        }

        #endregion Public Methods

    }

    [StructLayout(LayoutKind.Sequential)]
    struct COPYDATASTRUCT
    {
        public IntPtr dwData;
        public int cbData;
        public IntPtr lpData;
    }

   

    [SuppressUnmanagedCodeSecurity]
    class NativeMethod
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg,
            IntPtr wParam, ref COPYDATASTRUCT lParam);
    }

    public enum CopyStatus
    {
        WindowNotFound = 0,
        TooManyWindowsFound = 1,
        Success = 2,
        Fail = 3

    }
}
