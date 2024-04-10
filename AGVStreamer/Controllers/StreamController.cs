using Microsoft.AspNetCore.Mvc;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace AGVStreamer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StreamController : ControllerBase
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private IntPtr FindWindowByCaption(string windowTitle)
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows(delegate (IntPtr wnd, IntPtr param)
            {
                if (IsWindowVisible(wnd))
                {
                    StringBuilder buffer = new StringBuilder(GetWindowTextLength(wnd) + 1);
                    GetWindowText(wnd, buffer, buffer.Capacity);
                    if (buffer.ToString().Contains(windowTitle))
                    {
                        found = wnd;
                        return false; // Znaleziono okno, zatrzymaj enumerację
                    }
                }
                return true; // Kontynuuj enumerację
            }, IntPtr.Zero);
            return found;
        }

        private Bitmap CaptureWindow(IntPtr hWnd)
        {
            GetWindowRect(hWnd, out RECT windowRect);
            int width = windowRect.Right - windowRect.Left;
            int height = windowRect.Bottom - windowRect.Top;
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics gfx = Graphics.FromImage(bmp))
            {
                gfx.CopyFromScreen(windowRect.Left, windowRect.Top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
            }
            return bmp;
        }

        [HttpGet]
        public IActionResult GetImage()
        {
            string windowTitle = "AgWay Wireless Navigation Framework";
            IntPtr hWnd = FindWindowByCaption(windowTitle);
            if (hWnd == IntPtr.Zero)
            {
                return NotFound("AgWay Wireless Navigation Framework not running");
            }
            using var bmp = CaptureWindow(hWnd);
            using var memoryStream = new MemoryStream();
            bmp.Save(memoryStream, ImageFormat.Png);
            byte[] byteImage = memoryStream.ToArray();
            return File(byteImage, "image/png");
        }

        /*
        [HttpGet("Partial")]
        public IActionResult GetPartialImage()
        {
            string windowTitle = "AgWay Wireless Navigation Framework";
            IntPtr hWnd = FindWindowByCaption(windowTitle);
            if (hWnd == IntPtr.Zero)
            {
                return NotFound("AgWay Wireless Navigation Framework not running");
            }

            using var bmp = CapturePartialWindow(hWnd);
            using var memoryStream = new MemoryStream();
            bmp.Save(memoryStream, ImageFormat.Png);
            byte[] byteImage = memoryStream.ToArray();
            return File(byteImage, "image/png");
        }

        private Bitmap CapturePartialWindow(IntPtr hWnd)
        {
            GetWindowRect(hWnd, out RECT windowRect);
            int fullWidth = windowRect.Right - windowRect.Left;
            int fullHeight = windowRect.Bottom - windowRect.Top;

            // Calculate the dimensions based on the specified percentages.
            int width = (int)(fullWidth * 0.8); // 80% of the width
            int height = (int)(fullHeight * 0.6); // 60% of the height

            // Adjust starting points to capture the right upper corner part.
            int startX = windowRect.Left + (fullWidth - width);
            int startY = windowRect.Top;

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics gfx = Graphics.FromImage(bmp))
            {
                gfx.CopyFromScreen(startX, startY, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
            }
            return bmp;
        }
        */
    }
}
