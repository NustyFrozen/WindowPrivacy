using ImGuiNET;
using Microsoft.Win32;
using System.Numerics;
using System.Runtime.InteropServices;
namespace WindowPrivacy
{
    internal class Extentions
    {
        #region Window styles
        [Flags]
        public enum ExtendedWindowStyles
        {
            // ...
            WS_EX_TOOLWINDOW = 0x00000080,
            // ...
        }

        public enum GetWindowLongFields
        {
            // ...
            GWL_EXSTYLE = (-20),
            // ...
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            int error = 0;
            IntPtr result = IntPtr.Zero;
            // Win32 SetWindowLong doesn't clear error on success
            SetLastError(0);

            if (IntPtr.Size == 4)
            {
                // use SetWindowLong
                Int32 tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(tempResult);
            }
            else
            {
                // use SetWindowLongPtr
                result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
                error = Marshal.GetLastWin32Error();
            }

            if ((result == IntPtr.Zero) && (error != 0))
            {
                throw new System.ComponentModel.Win32Exception(error);
            }

            return result;
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern Int32 IntSetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);

        private static int IntPtrToInt32(IntPtr intPtr)
        {
            return unchecked((int)intPtr.ToInt64());
        }

        [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
        public static extern void SetLastError(int dwErrorCode);
        #endregion
        [DllImport("user32.dll")]
        public static extern uint SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);
        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.Dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr parentHandle, Win32Callback callback, IntPtr lParam);
        public delegate bool Win32Callback(IntPtr hwnd, IntPtr lParam);
        public static List<IntPtr> GetRootWindowsOfProcess(int pid)
        {
            List<IntPtr> rootWindows = GetChildWindows(IntPtr.Zero);
            List<IntPtr> dsProcRootWindows = new List<IntPtr>();
            foreach (IntPtr hWnd in rootWindows)
            {
                uint lpdwProcessId;
                GetWindowThreadProcessId(hWnd, out lpdwProcessId);
                if (lpdwProcessId == pid)
                    dsProcRootWindows.Add(hWnd);
            }
            return dsProcRootWindows;
        }
        public static void SetStartup(bool startup)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (rk is null) return;
            if (startup)
                rk.SetValue("WindowPrivacy", Application.ExecutablePath);
            else
                rk.DeleteValue("WindowPrivacy", false);

        }
        public static List<IntPtr> GetChildWindows(IntPtr parent)
        {
            List<IntPtr> result = new List<IntPtr>();
            GCHandle listHandle = GCHandle.Alloc(result);
            try
            {
                Win32Callback childProc = new Win32Callback(EnumWindow);
                EnumChildWindows(parent, childProc, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated)
                    listHandle.Free();
            }
            return result;
        }

        private static bool EnumWindow(IntPtr handle, IntPtr pointer)
        {
            GCHandle gch = GCHandle.FromIntPtr(pointer);
            List<IntPtr> list = gch.Target as List<IntPtr>;
            if (list == null)
            {
                throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
            }
            list.Add(handle);
            //  You can modify this to check to see if you want to cancel the operation, then return a null here
            return true;
        }

    }
    public static class Vector2Extention
    {
        public static Point toPoint(this Vector2 vec)
        {
            return new Point(Convert.ToInt16(vec.X), Convert.ToInt16(vec.Y));
        }
    }
    public static class ColorExtention
    {
        public static int toInt(this ImGuiCol col)
        {
            return (int)col;

        }
        public static Vector4 toVec4(this System.Drawing.Color clr)
        {
            return new Vector4(clr.R / 255.0f, clr.G / 255.0f, clr.B / 255.0f, clr.A / 255.0f);
        }
        public static Color toColor(this Vector4 vec)
        {
            return Color.FromArgb((int)(vec.W * 255.0f), (int)(vec.X * 255.0f), (int)(vec.Y * 255.0), (int)(vec.Z * 255.0));
        }
        public static Color toColor(this uint col)
        {
            var a = (byte)(col >> 24);
            var r = (byte)(col >> 16);
            var g = (byte)(col >> 8);
            var b = (byte)(col >> 0);
            a = (byte)((float)a);
            return Color.FromArgb(a, b, g, r);
        }
        public static int lerp(this int x, int final, double progress)
        {
            return Convert.ToInt16((1 - progress) * x + final * progress);
        }
        public static float lerp(this float x, float final, double progress)
        {
            return (float)((1 - progress) * x + final * progress);
        }
        public static Color Brightness(this Color A, float t) //linear interpolation
        {


            return Color.FromArgb(Convert.ToInt32(A.R * t), Convert.ToInt32(A.G * t), Convert.ToInt32(A.B * t));
        }
        public static Color lerp(this Color A, Color B, double t) //linear interpolation
        {

            double R = (1 - t) * A.R + B.R * t;
            double G = (1 - t) * A.G + B.G * t;
            double BB = (1 - t) * A.B + B.B * t;
            return Color.FromArgb((int)(255.0f), Convert.ToInt32(R), Convert.ToInt32(G), Convert.ToInt32(BB));
        }
        public static Color Rainbow(this System.Drawing.Color clr, float progress)
        {
            float div = (System.Math.Abs(progress % 1) * 6);
            int ascending = (int)((div % 1) * 255);
            int descending = 255 - ascending;

            switch ((int)div)
            {
                case 0:
                    return Color.FromArgb(255, 255, ascending, 0);
                case 1:
                    return Color.FromArgb(255, descending, 255, 0);
                case 2:
                    return Color.FromArgb(255, 0, 255, ascending);
                case 3:
                    return Color.FromArgb(255, 0, descending, 255);
                case 4:
                    return Color.FromArgb(255, ascending, 0, 255);
                default: // case 5:
                    return Color.FromArgb(255, 255, 0, descending);
            }
        }

        public static uint ToUint(this System.Drawing.Color c)
        {

            return (uint)(((c.A << 24) | (c.B << 16) | (c.G << 8) | c.R) & 0xffffffffL);
        }
    }
    public static class ImageSharpExtensions
    {




        public static SixLabors.ImageSharp.Image<TPixel> ToImageSharpImage<TPixel>(this System.Drawing.Bitmap bitmap) where TPixel : unmanaged, SixLabors.ImageSharp.PixelFormats.IPixel<TPixel>
        {
            using (var memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);

                memoryStream.Seek(0, SeekOrigin.Begin);

                return SixLabors.ImageSharp.Image.Load<TPixel>(memoryStream);
            }
        }
    }
}
