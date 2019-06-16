using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using System.Configuration;
using System.Management;
using System.Drawing;
using System.Drawing.Imaging;
using System.Xml.Linq;
using System.Net;
using System.Collections.Specialized;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using Newtonsoft.Json;


namespace tnt_sender
{
    public partial class Form1 : Form
    {        
        Dictionary<string, adbDevice> Devices = new Dictionary<string, adbDevice>();
        public static Form1 LastInstance { get; protected set; }

        public Form1()
        {
            InitializeComponent();
            LastInstance = this;
        }


        //Если текстбокс объявлен как защищенный 
        public static void SetTextLog(string text)
        {
            LastInstance.log.Text = text;
        }

        static string nox_path = ConfigurationManager.AppSettings["nox_path"];
        static string nox_exe { get { return nox_path + "\\bin\\nox_adb.exe"; } }
        //static string memu_path = "D:\\Program Files\\Microvirt\\MEmu\\";
        private string version = "v4.7 (build 07.06.2019)";
        static public string apikey;
        static public string apiurl;
        string mysql_table_name = "base_viber";

        public adb adb = new adb("nox");
        public mysql_db mysqldb = new mysql_db();
        public nox nox = new nox();
        public memu memu = new memu();

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
        private int seconds_from_start = 0;

        my_sms sms_service = new my_sms(apiurl, apikey);

        private string server_name = "tnt-nets.ru"; //"192.168.0.100"; //

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


        public static Object locker = new Object();

        public string GET(string Url, string Data)
        {
            try
            {
                System.Net.WebRequest req = System.Net.WebRequest.Create(Url + "?" + Data);
                System.Net.WebResponse resp = req.GetResponse();
                System.IO.Stream stream = resp.GetResponseStream();
                System.IO.StreamReader sr = new System.IO.StreamReader(stream);
                string Out = sr.ReadToEnd();
                sr.Close();
                return Out;
            }
            catch (Exception ex)
            {
                //write_log(ex.ToString());

                write_log("Нет интернета ?" + ex.ToString());
                return ("error");
            }
        }

        public string GET(string Url)
        {
            try
            {
                System.Net.WebRequest req = System.Net.WebRequest.Create(Url);
                System.Net.WebResponse resp = req.GetResponse();
                System.IO.Stream stream = resp.GetResponseStream();
                System.IO.StreamReader sr = new System.IO.StreamReader(stream);
                string Out = sr.ReadToEnd();
                sr.Close();
                return Out;
            }
            catch (Exception ex)
            {
                //write_log(ex.ToString());

                write_log("Нет интернета ?" + ex.ToString());
                return ("error");
            }
        }

        private void sortOutputHandler(object sendingProcess, DataReceivedEventArgs data)
        {
            //используем делегат для доступа к элементу формы из другого потока
            BeginInvoke(new MethodInvoker(delegate
            {
                if (!String.IsNullOrEmpty(data.Data))
                {
                    //выводим результат в консоль
                    tbRawDevs.AppendText(data.Data + Environment.NewLine);
                }
            }));
        }

        private void whenExitProcess(Object sender, EventArgs e)
        {
            BeginInvoke(new MethodInvoker(UpdateDevList));
        }

        private void UpdateDevList()
        {
            var values = tbRawDevs.Lines
                .Select(v => v.Split(' '))
                .Where(v => !String.IsNullOrEmpty(v[0]) && v[0] != "List" && v[0].Substring(0, 1) != "*")
                .Select(v => v[0])
                .ToList()
                .OrderBy(v => v)
                .ToArray();

            var avatar = textBox_avatar.Text;
            var photo = textBox_photo.Text;

            var apk1 = textBox_apk1.Text;
            var apk2 = textBox_apk2.Text;

            var Func = "Viber";

            if (cbFunc.SelectedIndex!=-1)
                Func = cbFunc.Items[cbFunc.SelectedIndex].ToString();


            var Class = cbClass.Items[cbClass.SelectedIndex].ToString();  

            var export_enable = checkBox_export.Checked;
            var checker_enable = checkBox_checker_enable.Checked;


            int? tempDev = cbDevAdr.SelectedIndex;
            tempDev = tempDev < 0 ? null : tempDev;
            listDevs.Items.Clear();
            cbDevAdr.Items.Clear();
            listDevs.Items.AddRange(values);
            cbDevAdr.Items.AddRange(values);
            if (cbDevAdr.Items.Count == 0) cbDevAdr.SelectedIndex = -1;
            else cbDevAdr.SelectedIndex = tempDev ?? 0;
            if (chAg.Checked)
            {
                var newDevs = values.Except(Devices.Keys).Select(adr => new adbDevice(adr)).ToList();

                newDevs.ForEach(dev => Devices.Add(dev.device, dev));
                newDevs.Select(dev => dev.MyTh = new Thread(() =>
                {
                    Logger(() =>
                    {
                        apksname.ToList().ForEach(apk => dev.InstallAPK1(apk));

                        dev.Shell("rm /sdcard/DCIM/*");

                        if (checkBox_set_avatar.Enabled) // 11-06
                        {
                            string pic_path1 = Path.GetDirectoryName(Application.ExecutablePath);
                            dev.PushFile(pic_path1 + '\\' + avatar, "/sdcard/DCIM/");
                        }


                        if (checkBox_send_picture.Enabled) // 11-06
                        {
                            string pic_path2 = Path.GetDirectoryName(Application.ExecutablePath);
                            dev.PushFile(pic_path2 + '\\' + photo, "/sdcard/DCIM/");
                        }

                        do
                        {
                            doloop(apk1, apk2, Class, Func, export_enable, checker_enable, dev);
                        } while (MyInv(() => checkBox_doloop.Checked));
                    });
                }))
                .ToList()
                .ForEach(th => th.Start());
            }
        }


        public T MyInv<T>(Func<T> a)
        {
            if (this.InvokeRequired)
            {
                return (T)Invoke(a);
            }
            return a();
        }

        public void MyInv(Action a, bool begin = true)
        {
            if (this.InvokeRequired)
            {
                if (begin) { BeginInvoke(a); }
                else Invoke(a);
            }
            a();
        }

        public T Logger<T>(Func<T> a)
        {
            try
            {
                return a();
            }
            catch (Exception easdf)
            {
                Program.OnLog(null, easdf.ToString());
            }
            return default(T);
        }

        public void Logger(Action a)
        {
            try
            {
                a();
            }
            catch (Exception easdf)
            {
                Program.OnLog(null, easdf.ToString());
            }
        }

        private void doloop(string apk1, string apk2, string Class, string Func, bool export_enable, bool checker_enable, adbDevice dev)
        {
            string app = "com.viber.voip";
            
            if (Func.IndexOf("Viber") > -1)
            {
                app = "com.viber.voip";
            }
            if (Func.IndexOf("WhatsApp") > -1)
            {
                app = "com.whatsapp";
            }

            if (!checker_enable)
            {
                dev.Shell("am start com.android.gallery3d/.app.Gallery");
                Thread.Sleep(3000);
            }

            dev.Shell("pm clear " + app);


            var dd = new[] 
                    { 
                        new { f1 = apk1, f2 = "/data/local/tmp/com.BasicSample" } ,
                        new { f1 = apk2, f2 = "/data/local/tmp/com.BasicSample.test" 
                    }            
                    }.ToList();

            var Pkg = "com.BasicSample";

                dd.ForEach(a => dev.PushFile(a.f1, a.f2));
                dd.ForEach(a => dev.InstallAPK2(a.f2));

            if (!checker_enable)
            {
                dev.Instrument(Pkg, Class, Func);
            }
            else
            {
                dev.Instrument(Pkg, Class, "ViberReg");
                //dev.PushFile("checker.vcf", "checker.vcf");
                string current_path=Path.GetDirectoryName(Application.ExecutablePath);
                dev.PushFile(current_path + "\\checker.vcf", "/sdcard/DCIM/");

                dev.Shell("am start -t \"text/x-vcard\" -d \"file:///sdcard/DCIM/checker.vcf\" -a android.intent.action.VIEW com.android.contacts"); // # импорт VCF
                Thread.Sleep(60000);
            }

            if (Func == "Viber")
            {
                if (export_enable)
                {
                    write_log("Закончили рассылку, экспортируем аккаунт");
                    string export_ret = dev.ExportAccountViber();
                }
            }
            if (Func == "WhatsApp")
            {
                if (export_enable)
                {
                    write_log("Закончили рассылку, экспортируем аккаунт");
                    string export_ret = dev.ExportAccountWhatsApp();
                }
            }
        }




        public string stx = "|\\-/";
        public void GetDeviceList()
        {
            stx = stx[stx.Length - 1] + stx.Substring(0, stx.Length - 1);
            _ll.Text = "" + stx[0];

            tbRawDevs.Text = "";
            string s = " devices -l # список устройств";
            bool output = true;

            Process _process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();

            //if (radioButton_memu.Checked)
            //    startInfo.FileName = memu_path + "\\adb.exe";
            //else
            startInfo.FileName = nox_path + "\\bin\\nox_adb.exe";

            if (!File.Exists(startInfo.FileName))
            {
                MessageBox.Show("Укажите в tnt_sender.config верный путь к Nox'у !!!!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
                        
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            s = s.Substring(0, s.IndexOf("#"));
            startInfo.Arguments = s;

            if (output)
            {
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;
                _process.OutputDataReceived += new DataReceivedEventHandler(sortOutputHandler);
                _process.EnableRaisingEvents = true;
                _process.Exited += new EventHandler(whenExitProcess);
            }


            _process.StartInfo = startInfo;
            _process.Start();

            if (output)
                _process.BeginOutputReadLine();
        }


        void ___Program_Logger(object _sender, string text)
        {
            this.BeginInvoke((Action<object, string>)Program_Logger, (object)_sender, (object)text);
        }

        void _file__Program_Logger(object _sender, string text)
        {
            this.BeginInvoke((Action<object, string>)Program_Loggerfile, (object)_sender, (object)text);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Program.Logger += ___Program_Logger;
            Program.Logger += _file__Program_Logger;

            cbClass.SelectedIndex = 1; //////// ПЕРЕХОД НА JSON
            comboBox_memu.SelectedIndex = 0;
            comboBox_tablename.SelectedIndex = 0;
            comboBox_profile.SelectedIndex = 0;
            checkedListBox1.SetItemChecked(0, true);
            timer_init.Enabled = true;
        }

        void Program_Logger(object sender, string text)
        {
            output1(text);
        }

        void Program_Loggerfile(object sender, string text)
        {
            string logs_path = Path.GetDirectoryName(Application.ExecutablePath) + "\\logs\\";
            Directory.CreateDirectory(logs_path);

            var fname = sender;
            string log_name = "";

            if (sender == null)
            {
                log_name = "MAIN.log";
            }
            else
            {
                log_name = sender.ToString() + ".log";
            }

            log_name = new Regex("[:?*><\\/|\"]").Replace(log_name, "_");

            logs_path += log_name;

            //log.AppendText(DateTime.Now + " :: " + s + "\r\n");
            //MessageBox.Show(fname);
            ///File.AppendText()

            System.IO.StreamWriter writer = new System.IO.StreamWriter(logs_path, true);
            writer.WriteLine(DateTime.Now + " :: " + text);
            writer.Close();
        }

        private void timer_init_Tick(object sender, EventArgs e)
        {
            timer_init.Enabled = false;
           
            vm_params.Text = ConfigurationManager.AppSettings["vm_params"];

            string hwid = GetHDDSerial();

            if (hwid != "10942216")
            {
                textBox_apk1.Text = Path.GetDirectoryName(Application.ExecutablePath) + "\\app-debug.apk";
                textBox_apk2.Text = Path.GetDirectoryName(Application.ExecutablePath) + "\\app-debug-androidTest.apk";
            }

            write_log("Init start");
            this.Text = "TNT-SENDER " + version;
            string ip = GET("http://" + server_name + "/ip.php", "");
            write_log("Current IP: " + ip);
            textBox10.Text = ip;

            for (int i = 0; i < 4; i++)
            {
                comboBox1.Items.Add(ConfigurationManager.AppSettings["service" + i.ToString()]);

                string s1 = ConfigurationManager.AppSettings["apiurl" + i.ToString()];
                string s2 = ConfigurationManager.AppSettings["apikey" + i.ToString()];

                //listBox_sms_services.Items.Add(s1 + "#" + s2);
            }

            btn_getdata_Click(null, null);

            write_log("check_number=" + textBox_checknumber.Text);

            apikey = ConfigurationManager.AppSettings["apikey0"];
            apiurl = ConfigurationManager.AppSettings["apiurl0"];
            nox_path = ConfigurationManager.AppSettings["nox_path"];

            if (File.Exists("C:\\Program Files (x86)\\Nox\\bin\\Nox.exe"))
                nox_path = "C:\\Program Files (x86)\\Nox\\";

            write_log("nox_path=" + nox_path);

            GetDeviceList();
            GetHandles("Nox", false, true);
            Thread.Sleep(100);
            button19_Click(null, null);

            write_log("Get accounts list");

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Application.ExecutablePath) + "\\accounts\\");
                Directory.CreateDirectory(Path.GetDirectoryName(Application.ExecutablePath) + "\\accounts\\viber\\");
            }
            catch (Exception ex)
            {
                write_log("Ошибка создания директории" + ex.Message);
            }
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Application.ExecutablePath) + "\\accounts\\");
                Directory.CreateDirectory(Path.GetDirectoryName(Application.ExecutablePath) + "\\accounts\\viber\\");

                System.IO.DirectoryInfo info = new System.IO.DirectoryInfo(Path.GetDirectoryName(Application.ExecutablePath) + "\\accounts\\viber\\");
                System.IO.DirectoryInfo[] dirs = info.GetDirectories();
                System.IO.FileInfo[] files = info.GetFiles();

                for (int i = 0; i < dirs.Length; i++)
                {
                    if (dirs[i].ToString().IndexOf("2019") > 0)
                        listBox_accs_by_date.Items.Add(dirs[i].ToString());
                }
            }
            catch (Exception ex)
            {
                write_log("ОШИБКА !!! папки accounts " + ex.Message);
            }

            write_log("Init end");

            balance0.Text = get_balance(0);
            balance1.Text = get_balance(1);
            balance2.Text = get_balance(2);
            balance3.Text = get_balance(3);
            balance4.Text = get_balance(4);
        }

        private void SortHandles()
        {
            int n = listBox2.Items.Count;
            string[] strmas = new string[n];
            for (int i = 0; i < n; i++)
                strmas[i] = listBox2.Items[i].ToString();
            StringComparer comparer = StringComparer.InvariantCulture;
            Array.Sort(strmas, comparer);
            listBox2.Items.Clear();
            listBox2.Items.AddRange(strmas);
        }

        private void SetText(Control ctrl, string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => SetText(ctrl, text)));
                return;
            }

            ctrl.Text = text;
        }

        public void output1(string text)
        {
            log.AppendText(text);
        }
        public void output1Ln(string text)
        {
            log.AppendText(text + Environment.NewLine);
        }
        public void output2(string text)
        {
            phones.AppendText(text + Environment.NewLine);
        }

        public void write_log(string s)
        {
            Program.OnLog(this, s + Environment.NewLine);
        }

        public void add_phone(string s)
        {
            Invoke((MethodInvoker)(() => output2(DateTime.Now + " :: " + s)));
            Application.DoEvents();
        }

        private void write_log(string fname, string s)
        {
            Program.OnLog(fname, s);
        }

        const int WM_MOUSEMOVE = 512;
        const int WM_LBUTTONDOWN = 0x0201;
        const int WM_LBUTTONUP = 0x0202;
        private const int WM_PASTE = 0x0302;

        static public int MakeLParam(int LoWord, int HiWord)
        {
            return (int)((HiWord << 16) | (LoWord & 0xFFFF));
        }

        public void AdbScreenshot(string device, string fname)
        {
            adb.PerformAdbCommand(device, "shell screencap -p /sdcard/" + fname + " #1");
            Thread.Sleep(300);
            adb.PerformAdbCommand(device, "pull /sdcard/" + fname + " #1 ");
            Thread.Sleep(300);
            adb.PerformAdbCommand(device, "shell rm /sdcard/" + fname + " #1");
        }

        public void PixelLoop(Thread_Params c, string s)
        {
            string temp = s.Replace("!", "");
            var dataspt = temp.Split('|');
            int x = Convert.ToInt32(dataspt[0]);
            int y = Convert.ToInt32(dataspt[1]);

        prev:

            string pixel = my_graphics.GetPixelColor(c.hwnd, x, y);
            if (pixel == dataspt[2])
                write_log(c.logname,
                    c.device + " : " + "Выполнено условие цвет точки" + x.ToString() + "," + y.ToString() + "=" +
                    dataspt[2]);
            else
            {
                goto prev;
            }
        }



        public class Thread_Params
        {
            public string apikey = "51c4fc3e2c923fa4d9aa3fd8d762e21e";
            public string host = "http://china-numbers.shn-host.ru/";
            public string sid = "13408";

            public int start_sleep;
            public bool start_loop;
            public bool viber;

            public IntPtr hwnd;
            public int method_sms;
            public int step_interval = 1000;
            public int loop_step_interval = 300;

            public string device;
            public string logname;
            public string emulname;
            public string nick;
            public string status;
            public string text;

            public string country;
            public string country_code;

            public string check_number;

            public string template_url_reg;
            public string template_url_loop;

        }

        string GetWindowText(IntPtr hWnd)
        {
            int len = GetWindowTextLength(hWnd) + 1;
            StringBuilder sb = new StringBuilder(len);
            len = GetWindowText(hWnd, sb, len);
            return sb.ToString(0, len);
        }

        string GetClassName(IntPtr hWnd)
        {
            int len = 255;
            StringBuilder sb = new StringBuilder(len);
            len = GetClassName(hWnd, sb, len);
            return sb.ToString(0, len);
        }

        public void GetHandles(string name, bool vistroit_okna, bool setforeground)
        {
            string str;
            int x = 0;
            var pList = Process.GetProcesses();
            listBox2.Items.Clear();
            if (pList.Count() != 0)
            {
                foreach (var process in pList)
                {
                    int window_width = 700;
                    if (process.ProcessName == name)
                    {
                        str = process.Id.ToString() + " " + process.MainWindowHandle;
                        //MessageBox.Show(str);

                        if (true)
                        {
                            if (setforeground)
                                SetForegroundWindow(process.MainWindowHandle);
                            Thread.Sleep(100);

                            if (vistroit_okna)
                            {
                                RECT rect;
                                GetWindowRect(process.MainWindowHandle, out rect);
                                SetWindowPos(process.MainWindowHandle, IntPtr.Zero, x, 0, rect.Width - rect.Left, rect.Height - rect.Top, 0);
                                window_width = rect.Width - rect.Left;
                            }
                        }
                        else
                        {
                            if (setforeground)
                                SetForegroundWindow(process.MainWindowHandle);
                            Thread.Sleep(100);
                            if (vistroit_okna)
                                SetWindowPos(process.MainWindowHandle, IntPtr.Zero, x, 0, 300, 500, 0);
                            //SetWindowPos(process.MainWindowHandle, IntPtr.Zero, x, 0, 724, 1330, 0);
                        }


                        // richTextBox1.Text = Convert.ToString(process.ProcessName) + "\t" + pID + "\t";
                        listBox2.Items.Add(GetWindowText(process.MainWindowHandle) + ":" + process.MainWindowHandle);

                        if (true)
                            x += window_width;
                        else
                            x += 724;

                        Application.DoEvents();
                    }
                }
            }

            // SetForegroundWindow(this.Handle);
        }

        private List<IntPtr> GetBockovayPanel()
        {
            List<IntPtr> result = new List<IntPtr>();

            IntPtr need_hwnd = (IntPtr)0;
            EnumWindows((hWnd, lParam) =>
            {
                if (GetWindowTextLength(hWnd) != 0) //IsWindowVisible(hWnd) && 
                {
                    string text = GetWindowText(hWnd);
                    if (text == "Form")
                    {
                        //listBox2.Items.Add(hWnd + ":" + text + ":" + GetClassName(hWnd));
                        need_hwnd = hWnd;
                        result.Add(hWnd);
                    }
                }

                return true;
            }, IntPtr.Zero);

            return result;
        }

        static public string GetHDDSerial()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk");

            foreach (ManagementObject wmi_HD in searcher.Get())
            {
                // get the hardware serial no.
                if (wmi_HD["VolumeSerialNumber"] != null)
                {
                    //MessageBox.Show(wmi_HD["VolumeSerialNumber"].ToString());
                    return wmi_HD["VolumeSerialNumber"].ToString();
                }
            }

            return string.Empty;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Point defPnt = new Point();
            GetCursorPos(ref defPnt);

            Process[] anotherApps = Process.GetProcessesByName("Nox");
            //SetForegroundWindow(anotherApps[0].MainWindowHandle);
            // if (anotherApps.Count() > 0)
            //    nox_hwnd = anotherApps[0].MainWindowHandle;

            toolStripStatusLabel1.Text = "X = " + defPnt.X.ToString();
            toolStripStatusLabel2.Text = "Y = " + defPnt.Y.ToString();
            //toolStripStatusLabel3.Text = "hwnd = " + nox_hwnd;  
        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            seconds_from_start++;
            toolStripStatusLabel3.Text = seconds_from_start.ToString() + "сек. ";

            if (seconds_from_start % 10 == 0)
            {
                updatelist_btn_Click(null, null);
                button6_Click_1(null, null);                
            }
        }

        public void PostClickXY(IntPtr hwnd, int x, int y)
        {
            write_log("PostClickXY (" + x.ToString() + "," + y.ToString() + ") - осторожно! данный метод работает при определенном размере окна !");
            SetForegroundWindow(hwnd);
            PostMessage(hwnd, WM_MOUSEMOVE, 1, MakeLParam(x, y));
            PostMessage(hwnd, WM_LBUTTONDOWN, 1, MakeLParam(x, y));
            Thread.Sleep(10);
            PostMessage(hwnd, WM_LBUTTONUP, 0, MakeLParam(x, y));
            PostMessage(hwnd, WM_MOUSEMOVE, 0, MakeLParam(x, y));
            PostMessage(hwnd, WM_PASTE, 0, 0);
        }

        private void pristegnutpanel_btn_Click(object sender, EventArgs e)
        {
            write_log("Пристегиваем боковую панель");
            List<IntPtr> handles = GetBockovayPanel();

            for (int i = 0; i < handles.Count; i++)
            {
                IntPtr hwnd = handles[i];
                PostClickXY(hwnd, 5, 20);

                Thread.Sleep(500);
                PostClickXY(hwnd, 5, 20);
            }
        }


        private void chinalogin_btn_Click(object sender, EventArgs e)
        {
            string auth = GET("http://china-numbers.shn-host.ru", "action=loginIn&name=DimaMar333&password=DenisAlina2018");
            MessageBox.Show(auth);
        }

        private void getphones_btn_Click(object sender, EventArgs e)
        {
            //////UPDATE base SET state=0 WHERE 1=1
        }


        private void get_num_presents_Click(object sender, EventArgs e)
        {
            
        }

        private string get_balance(int i)
        {
            if (i==0)
            {
                write_log("Получаем баланс на нашем сервисе");
                string hwid = GetHDDSerial();
            
                string Answer1 = GET("http://tnt-nets.ru/sms2/?req=getbalance1");
                //MessageBox.Show(Answer1);

                toolStripStatusLabel4.Text = "Balance: " + Answer1 + " руб. ";
                return Answer1;
            }
            else
            {
                string service = ConfigurationManager.AppSettings["service" + (i - 1).ToString()];
                string api_url = ConfigurationManager.AppSettings["apiurl" + (i - 1).ToString()];
                string api_key = ConfigurationManager.AppSettings["apikey" + (i - 1).ToString()];
                write_log("Get balance " + service);

                string Answer = GET(api_url, "api_key=" + api_key + "&action=getBalance");
                var dataspt = Answer.Split(':');

                if (dataspt.Count() > 1)
                {
                    toolStripStatusLabel4.Text = "Balance: " + dataspt[1] + " руб. ";
                    return dataspt[1];
                }
                else
                {
                    toolStripStatusLabel4.Text = "Error get balance";
                    return "NULL";
                }
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            get_balance(comboBox1.SelectedIndex);
        }

        private void ChangeIP()
        {
            Process _process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = Path.GetDirectoryName(Application.ExecutablePath) + "\\changeip.exe";
            startInfo.Arguments = "";
            _process.StartInfo = startInfo;
            _process.Start();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            ChangeIP();
            string ip = GET("http://" + server_name + "/ip.php", "");
            write_log("Текущий IP: " + ip);
        }

        private void podbor_frm_btn_Click_1(object sender, EventArgs e)
        {
            Form2 f = new Form2();
            string[] values = listBox2.Items[0].ToString().Split(':');
            f.textBox1.Text = values[1].ToString();
            f.ShowDialog();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show(GetHDDSerial());
            write_log(GetHDDSerial());
        }

        private void button6_Click(object sender, EventArgs e)
        {
            //d:\Program Files\Nox\bin\BignoxVMS\Nox_1\

        }

        private string[] GetPhonesAndPullVCF(string device, int n, string base_name)
        {
            string phones = textBox_phones.Text;
                ///GetPhonesString(n, base_name);
            string[] strs = phones.Split('\n');

            string final = "";

            foreach (string phone in strs)
            {
                string p = phone.Replace("\r", "");

                if (p.Length > 1)
                {
                    if (p[0] != '+')
                        p = '+' + p;
                }

                final += "BEGIN:VCARD\r\nVERSION:3.0\r\nTEL;TYPE=VOICE,CELL;VALUE=text:" + p + "\r\nEND:VCARD\r\n";
            }
            textBox_vcf.Text = final;
            File.WriteAllText(textBox_vcfname.Text, textBox_vcf.Text);
            //PushFile(device, textBox_vcfname.Text, "//sdcard//DCIM//");
            //adb.PerformAdbCommand(device, "shell am start -t \"text/x-vcard\" -d \"file:///sdcard/DCIM/" + textBox_vcfname.Text + "\" -a android.intent.action.VIEW com.android.contacts # импорт VCF");
            return strs;
        }


        private void button7_Click(object sender, EventArgs e)
        {
            GetPhonesAndPullVCF(listDevs.Items[0].ToString(), (int)numericUpDown_msgs_count.Value, comboBox_tablename.Items[comboBox_tablename.SelectedIndex].ToString());
        }

        private void button10_Click(object sender, EventArgs e)
        {
            textBox_checked_phones.Text = adb.SqliteRequestValue(textBox_viberdb.Text, textBox_sqlite_cmd1.Text);
        }

        private void button11_Click(object sender, EventArgs e)
        {
            string dbname = "C:\\Users\\User\\Documents\\Visual Studio 2012\\Projects\\tnt_sender\\tnt_sender\\bin\\Debug\\accounts\\10.04.2019\\vi_\\com.viber.voip\\databases\\viber_messages";
            string req = "SELECT * FROM messages"; //address, status, body

            List<string> fieldsList = new List<string> { "address", "status", "body" };
            textBox_checked_phones.Text = adb.SqliteRequestTable(dbname, req, fieldsList);
            textBox_checked_phones.Text += "_____________________________________________";
            req = "SELECT * FROM participants_info"; //address, status, body

            List<string> fieldsList2 = new List<string> { "id", "number", "display_name" };
            textBox_checked_phones.Text += adb.SqliteRequestTable(dbname, req, fieldsList2);

            //textBox_checked_phones.Text = adb.SqliteRequest2(textBox_viberdb.Text, textBox_sqlite_cmd2.Text);
        }

        string GetDBState()
        {
            try
            {
                String data = GET("http://" + server_name + "/get_stat.php", "id=" + GetHDDSerial());

                string[] delim = { "<br>" };
                string[] data_lines = data.Split(delim, StringSplitOptions.None);

                string s1 = data_lines[0];
                string s2 = data_lines[1];

                return "Разослано: " + s1 + " из " + s2;
            }
            catch (Exception ex)
            {
                write_log("ОШИБКА !!! Не удается получить статистику базы (база не загружена)" + ex.Message);
                return "error db state";
            }
        }

        private void button19_Click(object sender, EventArgs e)
        {
            GetDBState();
        }

        private void chinalogin_btn_Click_1(object sender, EventArgs e)
        {
            sms_service.china_login();
        }

        private void button20_Click(object sender, EventArgs e)
        {

        }

        private void button21_Click(object sender, EventArgs e)
        {

        }

        private void PushFile(string device, string fname, string path)
        {
            string s = " -s " + device + " " + "push " + fname + " " + path;
            write_log(s);
            Process _process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.FileName = nox_path + "\\bin\\nox_adb.exe";
            startInfo.WorkingDirectory = Path.GetDirectoryName(Application.ExecutablePath);

            //startInfo.CreateNoWindow = true;
            //startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            if (s.IndexOf("#") > 0)
                s = s.Substring(0, s.IndexOf("#"));
            startInfo.Arguments = s;

            _process.StartInfo = startInfo;
            _process.Start();
        }

        public string[] apksname { get { return checkedListBox1.CheckedItems.Cast<string>().ToArray(); } }


        private void button24_Click(object sender, EventArgs e)
        {
            adb.ExportAccountWhatsApp(listDevs.Items[0].ToString());
        }

 

        private void listBox_cmds_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBox2.Text = listBox_cmds.Items[listBox_cmds.SelectedIndex].ToString();
        }

        private void pristegnutpanel_btn_Click_1(object sender, EventArgs e)
        {
            write_log("Пристегиваем боковую панель");

            List<IntPtr> handles = GetBockovayPanel();

            for (int i = 0; i < handles.Count; i++)
            {
                IntPtr hwnd = handles[i];
                PostClickXY(hwnd, 5, 20);

                Thread.Sleep(500);
                PostClickXY(hwnd, 5, 20);
            }
        }

        private void button27_Click(object sender, EventArgs e)
        {

        }


        private void button17_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < listBox_cmds.Items.Count; i++)
            {
                listBox_cmds.SelectedIndex++;
                string s = listBox_cmds.Items[listBox_cmds.SelectedIndex].ToString();
                textBox2.Text = s;
                perfom_cmd_Click(null, null);
                Thread.Sleep(1000);
            }
        }

        private void button28_Click(object sender, EventArgs e)
        {
            textBox2.Text = "shell screencap -p /sdcard/screen.png # сделать скрин";
            perfom_cmd_Click(null, null);
            Thread.Sleep(100);
            textBox2.Text = "pull /sdcard/screen.png # скачать скрин";
            perfom_cmd_Click(null, null);
        }


        private void button32_Click(object sender, EventArgs e)
        {
            //mysqldb.SqlQuery("TRUNCATE TABLE " + comboBox_tablename.Items[comboBox_tablename.SelectedIndex].ToString() + ";", "");
            GET("http://tnt-nets.ru/dropbd.php?id=" + GetHDDSerial());
        }

        private void button29_Click_1(object sender, EventArgs e)
        {
        }

        private void button33_Click_1(object sender, EventArgs e)
        {

        }

        private void button34_Click_1(object sender, EventArgs e)
        {
            string vm = comboBox_memu.SelectedItem.ToString();
            memu.SetVMOption(vm, " resolution_height 480");
            memu.SetVMOption(vm, " resolution_width 480");
            memu.SetVMOption(vm, " lac 0");
        }

        private void button35_Click(object sender, EventArgs e)
        {
            string vm = comboBox_memu.SelectedItem.ToString();
            MessageBox.Show(comboBox_memu.SelectedItem.ToString());
            memu.MemuConsole(vm);
        }

        private void updatelist_btn_Click(object sender, EventArgs e)
        {
            GetDeviceList();
            GetHandles("Nox", false, false);
            //SortHandles();            
        }

        string GetPhonesString(int n, string base_name)
        {
            string[] phones = mysqldb.GetPhones(n, base_name);
            textBox_phones.Text = "";

            string sout = "";

            for (int i = 0; i < n; i++)
            {
                //MessageBox.Show(phones[i]);
                sout += phones[i] + "\r\n";
            }

            return sout;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            string url = "http://" + server_name + "/set_txt.php";

            using (var webClient = new WebClient())
            {
                // Создаём коллекцию параметров
                var pars = new NameValueCollection();
                // Добавляем необходимые параметры в виде пар ключ, значение
                pars.Add("id", GetHDDSerial());
                pars.Add("txt", textBox_message.Text);
                var response = webClient.UploadValues(url, pars);
            }
        }

        private void comboBox_selected_service_SelectedIndexChanged(object sender, EventArgs e)
        {
            vm_params.Text = "-resolution:700x650 -dpi:160 -screen:vertical";

            if (cbFunc.SelectedIndex == 0)
            {
                mysql_table_name = "base_viber";
                //apkname.Text = "Viber.apk";
                //radioButton_method3.Checked = true;
                radioButton_viber.Checked = true;
                groupBox2.Enabled = false;
                groupBox4.Enabled = true;
            }
            else
            {
                mysql_table_name = "base_whatsapp";
                //apkname.Text = "WhatsApp.apk";
                //radioButton_method1.Checked = true;
                radioButton_whatsapp.Checked = true;
                groupBox2.Enabled = true;
                groupBox4.Enabled = false;
            }
        }

        private void uploadpics_btn_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < listDevs.Items.Count; i++)
            {
                string device = listDevs.Items[i].ToString();

                PushFile(device, textBox_avatar.Text, "//sdcard//DCIM//");
                Thread.Sleep(300);
                PushFile(device, textBox_photo.Text, "//sdcard//DCIM//");
                Thread.Sleep(300);
                adb.PerformAdbCommand(device, " shell monkey -p com.android.gallery3d 1 #запуск whatsapp");
                Thread.Sleep(300);
            }

            // perfom_cmd_Click_1(null, null);
        }

        private void button23_Click(object sender, EventArgs e)
        {
            GetHandles("Nox", true, false);
        }

        private bool DownloadNow = false;

        void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            write_log("Скачивание успешно завершено");
            toolStripProgressBar1.Value = 0;
            DownloadNow = false;
        }
        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            toolStripProgressBar1.Maximum = (int)e.TotalBytesToReceive / 100;
            toolStripProgressBar1.Value = (int)e.BytesReceived / 100;
        }

        void DownloadAPK(string apkname)
        {
            write_log("Скачивание " + apkname);
            DownloadNow = true;
            string fileName = Path.GetDirectoryName(Application.ExecutablePath) + "\\apk\\" + apkname;

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            WebClient client = new WebClient();
            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
            client.DownloadFileCompleted += new AsyncCompletedEventHandler(client_DownloadFileCompleted);
            client.DownloadFileAsync(new Uri("http://" + server_name + "/apk/" + apkname), fileName);

            while (true)
            {
                if (DownloadNow == false)
                    break;
                Thread.Sleep(100);
            }
        }

        private void button37_Click(object sender, EventArgs e)
        {

        }

        System.Diagnostics.Process process = null;

        void Copy(string source, string dest)
        {

        }

        void Install(string source, string dest)
        {
            var com1 = @"nox_adb.exe -s 127.0.0.1:62025 push C:\Users\User\Desktop\1\android-testing-master\android-testing-master\ui\uiautomator\BasicSample\app\build\outputs\apk\debug\app-debug.apk /data/local/tmp/com.example.android.testing.uiautomator.BasicSample";
            var deviceAddress = "127.0.0.1:62025";
            var args = new[] { "" }.ToList();
            args.Add("-s");
            args.Add(deviceAddress);
            args.Add("push");
            var appDest = "/data/local/tmp/com.example.android.testing.uiautomator.BasicSample";
            var adrApp = @"C:\Users\User\Desktop\1\android-testing-master\android-testing-master\ui\uiautomator\BasicSample\app\build\outputs\apk\debug\app-debug.apk";
            args.Add(adrApp);
            args.Add(appDest);

            string.Join(" ", args);
            var p = new System.Diagnostics.Process()
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = nox_exe,
                    Arguments = string.Join(" ", args),
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true

                }
            };
            p.EnableRaisingEvents = true;
            p.Start();
            new Thread(() =>
            {
                var buffer = new char[1024 * 64];
                try
                {
                    while (true)
                    {
                        int n1 = p.StandardOutput.Read(buffer, 0, buffer.Length);
                        if (n1 == 0) break;
                        var str = new string(buffer, 0, n1);
                        MessageBox.Show(str);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());

                }
            }).Start();

            new Thread(() =>
            {
                var buffer = new char[1024 * 64];
                try
                {
                    while (true)
                    {
                        int n1 = process.StandardError.Read(buffer, 0, buffer.Length);
                        if (n1 == 0) break;
                        var str = new string(buffer, 0, n1);
                        MessageBox.Show(str);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());

                }

            }).Start();

            p.Exited += p_Exited;

        }

        void p_Exited(object sender, EventArgs e)
        {
            MessageBox.Show("sdfgdfg");
        }

        void Start(string cmd, IList<string> args, Action Exit = null, Action<string> Out = null, Action<string> Err = null)
        {
            //  Out = Out ?? ((_) => { });
            // Err = Err ?? ((_) => { });
            //  Exit = Exit ?? (() => { });

            var p = new System.Diagnostics.Process()
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = cmd,
                    Arguments = string.Join(" ", args),
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true

                }
            };
            p.EnableRaisingEvents = true;
            p.Start();
            if (Out != null) new Thread(() =>
                {
                    var buffer = new char[1024 * 64];
                    try
                    {
                        while (true)
                        {
                            int n1 = p.StandardOutput.Read(buffer, 0, buffer.Length);
                            if (n1 == 0) break;
                            var str = new string(buffer, 0, n1);
                            Out(str);
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.ToString());

                    }
                }).Start();

            if (Err != null) new Thread(() =>
               {
                   var buffer = new char[1024 * 64];
                   try
                   {
                       while (true)
                       {
                           int n1 = process.StandardError.Read(buffer, 0, buffer.Length);
                           if (n1 == 0) break;
                           var str = new string(buffer, 0, n1);
                           Err(str);
                       }
                   }
                   catch (Exception e)
                   {
                       MessageBox.Show(e.ToString());

                   }

               }).Start();

            if (Exit != null) p.Exited += (_1, _2) => Exit();

        }
        void V1()
        {
            if (process == null)
            {
                process = new System.Diagnostics.Process()
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = nox_exe,
                        RedirectStandardInput = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                //process.EnableRaisingEvents = true;
                process.Start();
                new Thread(() =>
                {
                    MessageBox.Show("xdgbcbb");
                    var buffer = new char[1024 * 64];
                    try
                    {
                        while (true)
                        {
                            int n1 = process.StandardOutput.Read(buffer, 0, buffer.Length);
                            var str = new string(buffer, 0, n1);
                            MessageBox.Show(str);
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.ToString());

                    }


                }).Start();
                new Thread(() =>
                {
                    var buffer = new char[1024 * 64];
                    try
                    {
                        while (true)
                        {
                            int n1 = process.StandardError.Read(buffer, 0, buffer.Length);
                            var str = new string(buffer, 0, n1);
                            MessageBox.Show(str);
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.ToString());

                    }

                }).Start();

            }
            else
            {
                process.Close();
                process = null;
            }
            Thread.Sleep(100);
            try
            {
                process.StandardInput.Write("dir\r\n");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Program.Logger -= ___Program_Logger;
        }

        private void button38_Click_1(object sender, EventArgs e)
        {
            string url = "http://" + server_name + "/set_api.php";

            using (var webClient = new WebClient())
            {
                // Создаём коллекцию параметров
                var pars = new NameValueCollection();
                // Добавляем необходимые параметры в виде пар ключ, значение
                pars.Add("id", GetHDDSerial());
                pars.Add("api_url", textBox_api_url.Text);
                pars.Add("api_key", textBox_api_key.Text);
                byte[] response = webClient.UploadValues(url, pars);
                string str = System.Text.Encoding.UTF8.GetString(response);
                MessageBox.Show(str);
            }
        }

        private void button40_Click(object sender, EventArgs e)
        {
            string url = "http://" + server_name + "/set_txt.php";

            using (var webClient = new WebClient())
            {
                // Создаём коллекцию параметров
                var pars = new NameValueCollection();
                // Добавляем необходимые параметры в виде пар ключ, значение
                pars.Add("id", GetHDDSerial());
                pars.Add("txt", textBox_message.Text);
                var response = webClient.UploadValues(url, pars);
                string str = System.Text.Encoding.UTF8.GetString(response);
                MessageBox.Show(str);
            }
        }

        private void button41_Click(object sender, EventArgs e)
        {
            string url = "http://" + server_name + "/set_app.php";

            using (var webClient = new WebClient())
            {
                // Создаём коллекцию параметров
                var pars = new NameValueCollection();
                // Добавляем необходимые параметры в виде пар ключ, значение
                pars.Add("id", GetHDDSerial());
                var response = webClient.UploadValues(url, pars);
                string str = System.Text.Encoding.UTF8.GetString(response);
                MessageBox.Show(str);
            }
        }

        private void button42_Click(object sender, EventArgs e)
        {
            var adr = cbDevAdr.Text;
            var dev = new adbDevice(adr);
            //dev.Shell("pm clear com.viber.voip");
            dev.PushFile(textBox_apk1.Text, "/data/local/tmp/com.BasicSample");
            Thread.Sleep(1000);
            Application.DoEvents();
            dev.PushFile(textBox_apk2.Text, "/data/local/tmp/com.BasicSample.test");
            Thread.Sleep(1000);
            Application.DoEvents();
            dev.InstallAPK2("/data/local/tmp/com.BasicSample");
            Application.DoEvents();
            dev.InstallAPK2("/data/local/tmp/com.BasicSample.test");
            Application.DoEvents();
            dev.Instrument("com.BasicSample", "INST", cbFunc.Items[cbFunc.SelectedIndex].ToString());
            Application.DoEvents();
        }

        private void bInstall_Click(object sender, EventArgs e)
        {
            var apks = apksname.ToList();
            var devs = listDevs.Items
                .OfType<object>()
                .Select(ob => ob.ToString())
                .Select(vmname => new adbDevice(vmname))
                .ToList();
            new Thread((ThreadStart)(() => devs.ForEach(dev => apks.ForEach(apk =>
            {
                try
                {
                    dev.InstallAPK1(apk);
                }
                catch (Exception ex1) { Program.OnLog(null, Thread.CurrentThread.ManagedThreadId + ":" + ex1.ToString()); }
            })))).Start();
        }       

        private void button3_Click(object sender, EventArgs e)
        {
            var adr = cbDevAdr.Text;

            new Thread((ThreadStart)(() =>
            {
                string export_ret = adb.ExportAccountViber(adr);
            })).Start();
        }

        private void listBox_accs_by_date_SelectedIndexChanged(object sender, EventArgs e)
        {
            System.IO.DirectoryInfo info = new System.IO.DirectoryInfo(Path.GetDirectoryName(Application.ExecutablePath) + "\\accounts\\viber\\" + listBox_accs_by_date.Items[listBox_accs_by_date.SelectedIndex].ToString());
            System.IO.DirectoryInfo[] dirs = info.GetDirectories();
            System.IO.FileInfo[] files = info.GetFiles();

            comboBox4.Items.Clear();
            for (int i = 0; i < dirs.Length; i++)
            {
                comboBox4.Items.Add(dirs[i].ToString());
            }

            comboBox4.SelectedIndex = 0;
        }

        private void button36_Click_1(object sender, EventArgs e)
        {
            for (int i = 0; i < comboBox4.Items.Count; i++)
            {
                string dbname = adb.MakeReport(listBox_accs_by_date.Items[listBox_accs_by_date.SelectedIndex].ToString(), comboBox4.Items[i].ToString());
                write_log("Отчет по базе" + dbname);
            }
        }

        private void button22_Click_2(object sender, EventArgs e)
        {
            adb.MakeReport(listBox_accs_by_date.Items[listBox_accs_by_date.SelectedIndex].ToString(), comboBox4.Items[comboBox4.SelectedIndex].ToString());
        }

        private void btn_getdata_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            var url = "http://tnt-nets.ru/get_data_json.php?id=" + GetHDDSerial();

            write_log("Get worker settings " + GetHDDSerial());
            write_log("URL config: " + url);
            
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                var data1 = reader.ReadToEnd();
                dynamic parsed = JsonConvert.DeserializeObject<dynamic>(data1);

                if (parsed == null)
                {
                    MessageBox.Show("Ваш HWID=" + GetHDDSerial() + " напишите его мне чтоб я прописал вас на сервере. Связаться со мной через телеграм: @tnt2018", "TNT-SENDER ошибка!",  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    //Application.Exit();
                    Process.GetCurrentProcess().Kill();
                }

                textBox_api_url.Text = parsed.api_url;
                textBox_api_key.Text = parsed.api_key;

                apikey_china.Text = parsed.apikey_china;
                apikey_smsactivate.Text = parsed.apikey_smsactivate;
                apikey_5sim.Text = parsed.apikey_5sim;
                apikey_smshub.Text = parsed.apikey_smshub;
                apikey_simsms.Text = parsed.apikey_simsms;



                string MethodSMS = parsed.sms_method;

                if (MethodSMS == "china")
                {
                    radioButton_method0.Checked = true;
                }
                else
                {
                    if (textBox_api_url.Text.IndexOf("sms-activate") > 0 && textBox_api_url.Text.IndexOf("5sim") == -1)
                        radioButton_method1.Checked = true;

                    if (textBox_api_url.Text.IndexOf("5sim") > 0)
                        radioButton_method2.Checked = true;

                    if (textBox_api_url.Text.IndexOf("smshub") > 0)
                        radioButton_method3.Checked = true;

                    if (textBox_api_url.Text.IndexOf("simsms") > 0)
                        radioButton_method4.Checked = true;
                }



                textBox_checknumber.Text = parsed.check_number;
                textBox_pic_description.Text = parsed.pic_description;

                numericUpDown_msgs_count.Value = Convert.ToInt32(parsed.msgs_count);
                textBox_message.Text = parsed.text;
                textBox_alfaname.Text = parsed.alfa_name;
                textBox_country.Text = parsed.country;

                for (int i = 0; i < comboBox_country.Items.Count; i++)
                {
                    if (comboBox_country.Items[i].ToString().IndexOf(textBox_country.Text) > -1)
                    {
                        comboBox_country.SelectedIndex = i;
                        break;
                    }
                }
                

                string funcs = parsed.funcs;
                string[] delim = { "\r\n" };
                string[] data_lines = funcs.Split(delim, StringSplitOptions.None);
                cbFunc.Items.AddRange(data_lines);
                cbFunc.SelectedIndex = 0;

                if (cbFunc.Items[cbFunc.SelectedIndex].ToString() == "Viber")
                    radioButton_viber.Checked = true;
                if (cbFunc.Items[cbFunc.SelectedIndex].ToString() == "WhatsApp")
                    radioButton_whatsapp.Checked = true;

                get_num_presents_Click_1(null, null);

                if (parsed.send_picture == "0")
                    checkBox_send_picture.Checked = false;
                else
                    checkBox_send_picture.Checked = true;

                if (parsed.set_avatar == "0")
                    checkBox_set_avatar.Checked = false;
                else
                    checkBox_set_avatar.Checked = true;

                this.Text = "TNT-SENDER " + version + " User=" + parsed.name;




            }

            /*   write_log("ОШИБКА !!! Не найдены настройки воркера");  */


            this.Enabled = true;
        }

        private void btn_setdata_Click(object sender, EventArgs e)
        {
            string url = "http://" + server_name + "/set_data.php";

            using (var webClient = new WebClient())
            {
                // Создаём коллекцию параметров
                var pars = new NameValueCollection();
                // Добавляем необходимые параметры в виде пар ключ, значение
                pars.Add("id", GetHDDSerial());
                pars.Add("api_url", textBox_api_url.Text);
                pars.Add("api_key", textBox_api_key.Text);
                pars.Add("check_number", textBox_checknumber.Text);
                pars.Add("msgs_count", numericUpDown_msgs_count.Value.ToString());
                pars.Add("txt", textBox_message.Text);
                pars.Add("alfa_name", textBox_alfaname.Text);
                pars.Add("country", textBox_country.Text);
                pars.Add("pic_description", textBox_pic_description.Text);

                pars.Add("apikey_china", apikey_china.Text);
                pars.Add("apikey_smsactivate", apikey_smsactivate.Text);
                pars.Add("apikey_5sim", apikey_5sim.Text);
                pars.Add("apikey_smshub", apikey_smshub.Text);
                pars.Add("apikey_simsms", apikey_simsms.Text);
                

                if (radioButton_method0.Checked)
                    pars.Add("sms_method", "china");
                else
                    pars.Add("sms_method", "sms-activate");

                if (checkBox_send_picture.Checked)
                    pars.Add("send_picture", "1");
                else
                    pars.Add("send_picture", "0");

                if (checkBox_set_avatar.Checked)
                    pars.Add("set_avatar", "1");
                else
                    pars.Add("set_avatar", "0");


                var response = webClient.UploadValues(url, pars);
                string str = System.Text.Encoding.UTF8.GetString(response);
                write_log(str);
                MessageBox.Show(str);
            }
        }

        private void listBox_sms_services_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        public string SendFile(string sWebAddress, string filePath, NameValueCollection nvc)
        {
            WebResponse response = null;
            try
            {
                string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
                byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
                HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(sWebAddress);
                wr.ContentType = "multipart/form-data; boundary=" + boundary;
                wr.Method = "POST";
                wr.KeepAlive = true;
                wr.Credentials = System.Net.CredentialCache.DefaultCredentials;
                Stream stream = wr.GetRequestStream();
                string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";

                foreach (string key in nvc.Keys)
                {
                    stream.Write(boundarybytes, 0, boundarybytes.Length);
                    string formitem = string.Format(formdataTemplate, key, nvc[key]);
                    byte[] formitembytes1 = System.Text.Encoding.UTF8.GetBytes(formitem);
                    stream.Write(formitembytes1, 0, formitembytes1.Length);
                }

                stream.Write(boundarybytes, 0, boundarybytes.Length);
                byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(filePath);
                stream.Write(formitembytes, 0, formitembytes.Length);
                stream.Write(boundarybytes, 0, boundarybytes.Length);
                string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
                string header = string.Format(headerTemplate, "file", Path.GetFileName(filePath), Path.GetExtension(filePath));
                byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
                stream.Write(headerbytes, 0, headerbytes.Length);

                FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                byte[] buffer = new byte[4096];
                int bytesRead = 0;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                    stream.Write(buffer, 0, bytesRead);
                fileStream.Close();

                byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
                stream.Write(trailer, 0, trailer.Length);
                stream.Close();

                response = wr.GetResponse();
                Stream responseStream = response.GetResponseStream();
                StreamReader streamReader = new StreamReader(responseStream);
                string responseData = streamReader.ReadToEnd();
                return responseData;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                if (response != null)
                    response.Close();
            }
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            File.WriteAllText(GetHDDSerial() + ".txt", textBox_phonebase.Text);
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("id", GetHDDSerial());
            nvc.Add("base", comboBox_tablename.Items[comboBox_tablename.SelectedIndex].ToString());
            SendFile("http://tnt-nets.ru/upload/index.php", GetHDDSerial() + ".txt", nvc);
        }

        private void button24_Click_1(object sender, EventArgs e)
        {
            var adr = cbDevAdr.Text;

            new Thread((ThreadStart)(() =>
            {
                string export_ret = adb.ExportAccountWhatsApp(adr);
            })).Start();
        }

        private void cbFunc_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbFunc.Items[cbFunc.SelectedIndex].ToString() == "WhatsApp")
            {
                checkedListBox1.SetItemChecked(0, false);
                checkedListBox1.SetItemChecked(1, true);
            }
            if (cbFunc.Items[cbFunc.SelectedIndex].ToString() == "Viber")
            {
                checkedListBox1.SetItemChecked(1, false);
                checkedListBox1.SetItemChecked(0, true);
            }

            write_log("Applying changes");
            ///////btn_setdata_Click(null, null); только после инициализации!!!!!!!!!!!!!
        }
        

        private void button6_Click_1(object sender, EventArgs e)
        {
            string s = GetDBState();
            textBox1.Text = s;
            toolStripStatusLabel5.Text = s;
            //MessageBox.Show(s);
        }

        private void bRunX_Click_1(object sender, EventArgs e)
        {
            int n1 = (int)numericUpDown_vmfirst.Value;
            int n2 = (int)numericUpDown_vmlast.Value;

            (new nox()).create_vms(n1, n2, 1 * 100, new string[0], vm_params.Text);
        }

        private void button8_Click_1(object sender, EventArgs e)
        {
            string android_device = listBox3.Items[_random.Next(listBox3.Items.Count)].ToString();

            var andr_params = android_device.Split('|');
            string brand = andr_params[0];
            string manufacturer = andr_params[1];
            string model = andr_params[2];

            vm_params.Text = "-resolution:700x650 -dpi:160 -screen:vertical -cpu:2 -memory:1024 -imei:864394100050568 -model:" + model + " -manufacturer:" + manufacturer + " -brand:" + brand;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            textBox8.Text = GET("http://tnt-nets.ru/view_base.php?id=" + GetHDDSerial());
        }

        private void button12_Click(object sender, EventArgs e)
        {
            if (openFileDialog2.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            textBox_phonebase.Text = File.ReadAllText(openFileDialog2.FileName);
        }
        
        private void comboBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            apiurl = ConfigurationManager.AppSettings["apiurl" + comboBox1.SelectedIndex.ToString()];
            apikey = ConfigurationManager.AppSettings["apikey" + comboBox1.SelectedIndex.ToString()];

            textBox_api_url.Text = apiurl;
            textBox_api_key.Text = apikey;
            
            get_balance(comboBox1.SelectedIndex+1);
            get_num_presents_Click(null, null);
        }

        private void button15_Click(object sender, EventArgs e)
        {
 
        }

        private void get_num_presents_Click_1(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == -1)
                return;

            write_log("Получаем номера в наличии (" + ConfigurationManager.AppSettings["service" + comboBox1.SelectedIndex.ToString()] + ")");
            string res = "";
            toolStripProgressBar1.Maximum = comboBox_country.Items.Count;

            for (int i = 0; i < comboBox_country.Items.Count; i++)
            {
                string country = comboBox_country.Items[i].ToString();
                country = country.Substring(0, country.IndexOf(" "));
                int cid = Int32.Parse(country);
                string Answer = GET(apiurl, "api_key=" + apikey + "&action=getNumbersStatus&country=" + (cid)); // comboBox_country.SelectedIndex

                if (Answer == "BAD_KEY")
                {
                    write_log(ConfigurationManager.AppSettings["service" + comboBox1.SelectedIndex.ToString()] + " :: BAD_KEY (укажите верный API-ключ в App.config)");
                    return;
                }
                if (Answer == "WRONG_OPERATOR")
                {
                    write_log(ConfigurationManager.AppSettings["service" + comboBox1.SelectedIndex.ToString()] + " :: WRONG_OPERATOR непонятная ошибка");
                    toolStripProgressBar1.Value = 0;
                    return;
                }
                if (Answer == "WRONG COUNTRY")
                {
                    write_log("WRONG COUNTRY " + country);
                    break;
                }
                if (Answer == "BANNED")
                {
                    write_log("BANNED " + country);
                    break;
                }


                string svc;

                if (radioButton_viber.Checked)
                    svc = "vi_0";
                else
                    svc = "wa_0";

                if (Answer.Length > 5)
                {
                    string wa = "";

                    switch (comboBox1.SelectedIndex)
                    {
                        case 0: wa = Answer.Substring(Answer.IndexOf(svc) + 7, 10); break;
                        case 1: wa = Answer.Substring(Answer.IndexOf(svc) + 7, 10); break;
                        case 2: wa = Answer.Substring(Answer.IndexOf(svc) + 8, 10); break;
                        case 3: wa = Answer.Substring(Answer.IndexOf(svc) + 7, 10); break;
                    }

                    wa = wa.Substring(0, wa.IndexOf("\""));
                    res += comboBox_country.Items[i].ToString() + " : " + wa + "\r\n";
                }
                toolStripProgressBar1.Value++;
                Application.DoEvents();
            }
            textBox5.Text = res;
            toolStripProgressBar1.Value = 0;
        }

        private void comboBox_country_SelectedIndexChanged(object sender, EventArgs e)
        {
            String temp = comboBox_country.Items[comboBox_country.SelectedIndex].ToString();
            var wds = temp.Split(' ');
            textBox_country.Text = wds[0];
        }
               
        private void radioButton_method0_CheckedChanged(object sender, EventArgs e)
        {
            groupBox2.Enabled = !radioButton_method0.Checked;
            if (radioButton_method0.Checked)
            {
                write_log("Логинимся в китае");
                sms_service.china_login();
                get_balance(0);
            }
        }

        private void radioButton_method1_CheckedChanged_1(object sender, EventArgs e)
        {
            groupBox2.Enabled = !radioButton_method0.Checked;
            textBox_api_url.Text = "http://sms-activate.ru/stubs/handler_api.php";
            textBox_api_key.Text = apikey_smsactivate.Text;
            comboBox1.SelectedIndex = 0;
        }

        private void radioButton_method2_CheckedChanged(object sender, EventArgs e)
        {
            groupBox2.Enabled = !radioButton_method0.Checked;
            textBox_api_url.Text = "http://sms-activate.api.5sim.net/stubs/handler_api.php";
            textBox_api_key.Text = apikey_5sim.Text;
            comboBox1.SelectedIndex = 1;
        }

        private void radioButton_method3_CheckedChanged(object sender, EventArgs e)
        {
            groupBox2.Enabled = !radioButton_method0.Checked;
            textBox_api_url.Text = "https://smshub.org/stubs/handler_api.php";
            textBox_api_key.Text = apikey_smshub.Text;
            comboBox1.SelectedIndex = 2;
        }

        private void radioButton_method4_CheckedChanged(object sender, EventArgs e)
        {
            groupBox2.Enabled = !radioButton_method0.Checked;
            textBox_api_url.Text = "http://simsms.org/stubs/handler_api.php";
            textBox_api_key.Text = apikey_simsms.Text;
            comboBox1.SelectedIndex = 3;
        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void comboBox_country_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            string s= comboBox_country.Items[comboBox_country.SelectedIndex].ToString();
            s = s.Substring(0, s.IndexOf(" "));
            textBox_country.Text = s;
        }

        private void perfom_cmd_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < listDevs.Items.Count; i++)
            {
                string prefix = "-s " + listDevs.Items[i].ToString() + " ";
                adb.PerformAdbCommand(prefix, textBox2.Text);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var adr = cbDevAdr.Text;
            var apk1 = textBox_apk1.Text;
            var dd = new[] {
                new { f1 = textBox_apk1.Text, f2 = "/data/local/tmp/com.BasicSample" } ,
                new { f1 = textBox_apk2.Text, f2 = "/data/local/tmp/com.BasicSample.test" }
            }.ToList();
            //new L

            var Pkg = tbPkg.Text;
            var Cl = cbClass.Text;
            var Func = cbFunc.Text;

            new Thread((ThreadStart)(() =>
            {
                var dev = new adbDevice(adr);
                dd.ForEach(a => dev.PushFile(a.f1, a.f2));
                dd.ForEach(a => dev.InstallAPK2(a.f2));
                dev.Instrument(Pkg, Cl, Func);
                MessageBox.Show("Закончили");
                string export_ret = adb.ExportAccountViber(adr);

            })).Start();
        }

        private void PushX_Click(object sender, EventArgs e)
        {
            openFileDialog1.Multiselect = true;
            if (openFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            var adr = cbDevAdr.Text;
            var fls = openFileDialog1.FileNames.ToList();

            new Thread((ThreadStart)(() =>
            {
                var dev = new adbDevice(adr);
                fls.ForEach(fl => dev.PushFile(fl, "/sdcard/DCIM/"));
            })).Start();
        }

        private void button13_Click(object sender, EventArgs e)
        {

        }

        private void button29_Click(object sender, EventArgs e)
        {
            memu.MemuConsole("create");

        }

        private void button33_Click(object sender, EventArgs e)
        {
            string vm = comboBox_memu.SelectedItem.ToString();
            memu.SetVMOption(vm, " imei 133524256790010");
        }

        private void bClear_Click(object sender, EventArgs e)
        {
            var adr = cbDevAdr.Text;
            var dev = new adbDevice(adr);
            dev.Shell("pm clear com.viber.voip");
        }

        private void button27_Click_1(object sender, EventArgs e)
        {
            List<IntPtr> result = new List<IntPtr>();

            IntPtr need_hwnd = (IntPtr)0;
            EnumWindows((hWnd, lParam) =>
            {
                if (GetWindowTextLength(hWnd) != 0) //IsWindowVisible(hWnd) && 
                {
                    string text = GetWindowText(hWnd);
                    if (text.IndexOf("Telegram") > -1)
                    {
                        //listBox2.Items.Add(hWnd + ":" + text + ":" + GetClassName(hWnd));
                        need_hwnd = hWnd;
                        result.Add(hWnd);
                    }

                }

                return true;
            }, IntPtr.Zero);

            MessageBox.Show(result.Count.ToString());

            for (int i = 0; i < result.Count; i++)
            {
                MessageBox.Show(result[i].ToString());
                my_graphics.MakeScreen(result[i], "screen_telega" + i.ToString() + ".png");
            }
        }

        private void button20_Click_1(object sender, EventArgs e)
        {
            string tzid = sms_service.sms_reg_getnum();
            textBox6.Text = tzid;
            string phone = sms_service.sms_reg_getstate(tzid);
            textBox7.Text = phone;
        }

        private void button21_Click_1(object sender, EventArgs e)
        {
            string tzid = "46097743";
            string answer1 = GET("http://api.sms-reg.com/setReady.php", "tzid=" + tzid + "&apikey=e748abvqgu1q3x5y3sae8a1obdhg5cll");
        }

        private void button37_Click_1(object sender, EventArgs e)
        {
            //DownloadAPK("Viber.apk");
            //DownloadAPK("WhatsApp.apk");
            DownloadAPK("xposed-271.apk");
            DownloadAPK("xposed-315.apk");
            DownloadAPK("changerpro_1.5.5_.apk");
            DownloadAPK("org.proxydroid-61.apk");
        }

        private void radioButton_whatsapp_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton_viber_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void groupBox6_Enter(object sender, EventArgs e)
        {

        }

        private void label34_Click(object sender, EventArgs e)
        {

        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void checkBox_set_avatar_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void tabPage4_Click(object sender, EventArgs e)
        {



        }
    }
}
