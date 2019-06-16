using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using System.Drawing;
using System.Drawing.Imaging;


namespace tnt_sender
{
    class my_graphics
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        [DllImport("user32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, uint wParam, int lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy,
            SetWindowPosFlags uFlags);

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(ref Point lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll")]
        static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private Random _random = new Random(Environment.TickCount);


        // Определим перечисление SetWindowPosFlags.
        [Flags()]
        private enum SetWindowPosFlags : uint
        {
            SynchronousWindowPosition = 0x4000,
            DeferErase = 0x2000,
            DrawFrame = 0x0020,
            FrameChanged = 0x0020,
            HideWindow = 0x0080,
            DoNotActivate = 0x0010,
            DoNotCopyBits = 0x0100,
            IgnoreMove = 0x0002,
            DoNotChangeOwnerZOrder = 0x0200,
            DoNotRedraw = 0x0008,
            DoNotReposition = 0x0200,
            DoNotSendChangingEvent = 0x0400,
            IgnoreResize = 0x0001,
            IgnoreZOrder = 0x0004,
            ShowWindow = 0x0040,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Width, Height;
        };

        

        static public void write_log(string s)
        {

        }


        static public void MakeScreen(IntPtr hwnd, string fname)
        {
            RECT rect;
            GetWindowRect(hwnd, out rect);
            
            using (var image = new Bitmap(rect.Width, rect.Height)) ///(rect.Right - rect.Left, rect.Bottom - rect.Top))
            {
                using (var graphics = Graphics.FromImage(image))
                {
                    var hdcBitmap = graphics.GetHdc();
                    PrintWindow(hwnd, hdcBitmap, 0);
                    graphics.ReleaseHdc(hdcBitmap);
                }

                // System.Drawing.Color pixel = image.GetPixel(x, y);

                try
                {
                    image.Save(fname, ImageFormat.Png);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message + "fname=" + fname);
                    //write_log(e.ToString());
                    //throw;
                }

                //string spixel = pixel.ToString();
              //  write_log("GetPixelColor (" + x.ToString() + "," + y.ToString() + ")=" + spixel);

                return;
            }
        }



        static public string GetPixelColor(IntPtr hwnd, int x, int y)
        {
            RECT rect;
            GetWindowRect(hwnd, out rect);

            using (var image = new Bitmap(800, 800)) ///(rect.Right - rect.Left, rect.Bottom - rect.Top))
            {
                using (var graphics = Graphics.FromImage(image))
                {
                    var hdcBitmap = graphics.GetHdc();
                    PrintWindow(hwnd, hdcBitmap, 0);
                    graphics.ReleaseHdc(hdcBitmap);
                }

                System.Drawing.Color pixel = image.GetPixel(x, y);


        //        image.Save("screens\\" + "image_" + hwnd.ToString() + "_" + _random.Next(1000).ToString() + ".png", ImageFormat.Png);

                string spixel = pixel.ToString();
                write_log("GetPixelColor (" + x.ToString() + "," + y.ToString() + ")=" + spixel);

                return spixel;
            }
        }

        public int CheckPixelColor(IntPtr hwnd, int x, int y, string color)
        {
            int ret = 0;
            System.Diagnostics.Stopwatch myStopwatch = new System.Diagnostics.Stopwatch();
            myStopwatch.Start(); //запуск

            string spixel = GetPixelColor(hwnd, x, y);

            if (spixel == color)
            {
                write_log("Цвет пикселя: " + spixel + " => Вацап есть !!!");
                // whatsapp_counter++;
                //   label13.Text = "Номеров c whatsapp: " + whatsapp_counter.ToString();
                ret = 1;
            }
            else
            {
                write_log("Цвет пикселя: " + spixel + " => Вацапа нет !!!");
                ret = 0;
            }

            // тут у вас есть картинка, вы можете, например, сохранить её

            myStopwatch.Stop(); //остановить
            TimeSpan ts = myStopwatch.Elapsed;
            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            write_log("Время создания скрина: " + elapsedTime);

            return ret;
        }


        /*
        public static Bitmap CaptureHWnd_WM_PRINT(UIntPtr hWnd)
        {
            Bitmap bitmap = null;
            UIntPtr hDC = User32.GetWindowDC(hWnd); //Get the device context of hWnd
            if (hDC != UIntPtr.Zero)
            {
                UIntPtr hMemDC = Gdi32.CreateCompatibleDC(hDC); //Create a memory device context, compatible with hDC
                if (hMemDC != UIntPtr.Zero)
                {
                    RECT rc; MyGetWindowRect(hWnd, out rc); //Get bounds of hWnd
                    UIntPtr hbitmap = Gdi32.CreateCompatibleBitmap(hDC, rc.Width, rc.Height); //Create a bitmap handle, compatible with hDC
                    if (hbitmap != UIntPtr.Zero)
                    {
                        UIntPtr hOld = Gdi32.SelectObject(hMemDC, hbitmap); //Select hbitmap into hMemDC
                        //#also tried: User32.SendMessage(hWnd, WM.PRINT, hMemDC, (UIntPtr)(DrawingOptions.PRF_CLIENT | DrawingOptions.PRF_CHILDREN | DrawingOptions.PRF_NONCLIENT | DrawingOptions.PRF_OWNED));
                        User32.DefWindowProc(hWnd, WM.PRINT, hMemDC, (UIntPtr)(DrawingOptions.PRF_CLIENT | DrawingOptions.PRF_CHILDREN | DrawingOptions.PRF_NONCLIENT | DrawingOptions.PRF_OWNED));
                        bitmap = Image.FromHbitmap(hbitmap.ToIntPtr()); //Create a managed Bitmap out of hbitmap

                        bitmap.Save("screen.bmp");

                        Gdi32.SelectObject(hMemDC, hOld); //Select hOld into hMemDC (the previously replaced object), to leave hMemDC as found
                        Gdi32.DeleteObject(hbitmap); //Free hbitmap
                    }
                    Gdi32.DeleteDC(hMemDC); //Free hdcMem
                }
                User32.ReleaseDC(hWnd, hDC); //Free hDC
            }
            return bitmap;
        }
        */
    }
}
