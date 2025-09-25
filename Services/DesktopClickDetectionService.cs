using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;

namespace RadialMenu.Services
{
    public class DesktopClickDetectionService : IDisposable
    {
        #region Windows API
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT Point);

        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        #endregion

        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;

        private LowLevelMouseProc _proc = null!;
        private IntPtr _hookID = IntPtr.Zero;
        private DispatcherTimer? _holdTimer;
        private bool _isHoldingOnDesktop;
        private POINT _holdStartPoint;
        
        public event Action<int, int>? DesktopHoldCompleted;

        public DesktopClickDetectionService()
        {
            _proc = HookCallback;
            _hookID = SetHook(_proc);
            
            // Initialize the hold timer (1 second)
            _holdTimer = new DispatcherTimer();
            _holdTimer.Interval = TimeSpan.FromMilliseconds(1000);
            _holdTimer.Tick += OnHoldTimerTick;
        }

        private IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule!)
            {
                return SetWindowsHookEx(
                    WH_MOUSE_LL,
                    proc,
                    GetModuleHandle(curModule.ModuleName!),
                    0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (wParam == (IntPtr)WM_LBUTTONDOWN)
                {
                    MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    
                    if (IsClickOnDesktop(hookStruct.pt))
                    {
                        _isHoldingOnDesktop = true;
                        _holdStartPoint = hookStruct.pt;
                        _holdTimer?.Start();
                    }
                }
                else if (wParam == (IntPtr)WM_LBUTTONUP)
                {
                    if (_isHoldingOnDesktop)
                    {
                        _holdTimer?.Stop();
                        _isHoldingOnDesktop = false;
                    }
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private bool IsClickOnDesktop(POINT point)
        {
            IntPtr hwnd = WindowFromPoint(point);
            if (hwnd == IntPtr.Zero)
                return false;

            // Get the desktop and shell window handles
            IntPtr desktopWindow = GetDesktopWindow();
            IntPtr shellWindow = GetShellWindow();

            // Check if clicked on desktop or shell window
            if (hwnd == desktopWindow || hwnd == shellWindow)
                return true;

            // Get the window class name to check for desktop-related windows
            var className = new System.Text.StringBuilder(256);
            if (GetClassName(hwnd, className, className.Capacity) > 0)
            {
                string windowClass = className.ToString().ToLower();
                
                // Check for common desktop/wallpaper window classes
                if (windowClass.Contains("progman") ||           // Program Manager (desktop)
                    windowClass.Contains("workerw") ||           // Desktop Worker Window
                    windowClass.Contains("shelldll_defview") ||  // Shell Desktop View
                    windowClass.Contains("syslistview32"))       // Desktop icon list view
                {
                    return true;
                }
            }

            // Check if it's the explorer.exe process (which handles desktop)
            if (GetWindowThreadProcessId(hwnd, out uint processId) != 0)
            {
                try
                {
                    Process process = Process.GetProcessById((int)processId);
                    if (process.ProcessName.ToLower() == "explorer" && 
                        (hwnd == desktopWindow || hwnd == shellWindow))
                    {
                        return true;
                    }
                }
                catch
                {
                    // Ignore process access errors
                }
            }

            return false;
        }

        private void OnHoldTimerTick(object? sender, EventArgs e)
        {
            _holdTimer?.Stop();
            
            if (_isHoldingOnDesktop)
            {
                // Trigger the desktop hold completed event
                DesktopHoldCompleted?.Invoke(_holdStartPoint.x, _holdStartPoint.y);
                _isHoldingOnDesktop = false;
            }
        }

        public void Dispose()
        {
            _holdTimer?.Stop();
            _holdTimer = null;
            
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
            
            GC.SuppressFinalize(this);
        }

        ~DesktopClickDetectionService()
        {
            Dispose();
        }
    }
}