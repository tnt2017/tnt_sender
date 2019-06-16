using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.Configuration;
using System.IO;
using System.Data.SQLite;



namespace tnt_sender
{
    public class adbDevice
    {
        public enum EMUL
        {
            nox,
            memu,
            qemu
        };
        public static string nox_path, memu_path;
        public string emulator;
        public string device;
        public Thread MyTh;
        public string adbEml
        {
            get
            {
                if (emulator == "nox")
                {
                    return nox_path + "\\bin\\nox_adb.exe";
                }
                if (emulator == "memu")
                {
                    return memu_path + "\\adb.exe";
                }
                if (emulator == "qemu")
                {
                    return @"C:\Users\User\AppData\Local\Android\Sdk\platform-tools\" + "adb.exe";
                }
                return "";
            }
        }

        static adbDevice()
        {
            nox_path = ConfigurationManager.AppSettings["nox_path"];
            memu_path = ConfigurationManager.AppSettings["memu_path"];
        }

        public adbDevice(string device, EMUL emul = EMUL.nox)
        {
            emulator = emul.ToString();
            this.device = device;
        }

        void write_log(string s)
        {
            Program.OnLog(null, s + Environment.NewLine);
        }
        static string d = "adb shell am instrument -w -r -e debug true -e class 'com.example.android.testing.uiautomator.BasicSample.ChangeTextBehaviorTest#X' com.example.android.testing.uiautomator.BasicSample.test/androidx.test.runner.AndroidJUnitRunner";

        public void Instrument(string Package, string Class,string Fun, string test = "test")
        {
            var AndroidJUnitRunner = "androidx.test.runner.AndroidJUnitRunner";
            var f1 =
                new[] { "am", "instrument", "-w", "-r", "-e debug false" }
                .Concat(
                new[] { "-e", "class" }
                )
            .Concat(
            new[] { "'" + Package + "." + Class + "#" + Fun + "'", Package + "." + test + "/" + AndroidJUnitRunner }
            );
                       

            Shell(string.Join(" ", f1));
        }

        public void Shell(string cmd)
        {
            Start(new[] { "shell" }, new[] { cmd, "exit" }, s => Program.OnLog(this, s), e => Program.OnLog(this, e));
        }

        public void Start(IList<string> args, string[] input=null, Action<string> Out = null, Action<string> Err = null)
        {
            //  Out = Out ?? ((_) => { });
            // Err = Err ?? ((_) => { });
            //  Exit = Exit ?? (() => { });
            args = args.ToList();

            args.Insert(0,"-s");
            args.Insert(1, device);

            Program.OnLog(null, "Start:" + string.Join(" ", args) + ":" + (input != null ? string.Join("&", input) : ""));

            var p = new System.Diagnostics.Process()
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo()
                {
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = adbEml,
                    Arguments = string.Join(" ", args),
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.GetEncoding(866),
                    StandardErrorEncoding = Encoding.GetEncoding(866)

                }
            };
            p.EnableRaisingEvents = true;
            try
            {
                p.Start();
                if (Out != null) new Thread(() =>
                {
                    var buffer = new char[1024 * 64];
                    try
                    {
                        while (true)
                        {
                            int n1 = p.StandardOutput.Read(buffer, 0, buffer.Length);
                            if (n1 <= 0) break;
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
                            int n1 = p.StandardError.Read(buffer, 0, buffer.Length);
                            if (n1 <= 0) break;
                            var str = new string(buffer, 0, n1);
                            Err(str);
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.ToString());

                    }

                }).Start();
                if (input != null)
                {
                    p.StandardInput.WriteLine(string.Join("&", input));
                }
                p.WaitForExit();
            }
            catch (Exception ER) {
                try
                {
                    if (p != null) p.Kill();
                }
                catch (Exception ex_) { }
            
            }

            
            //if (Exit != null) p.Exited += (_1, _2) => Exit();

        }
       
        public static void CopyDir(string FromDir, string ToDir)
        {
            Directory.CreateDirectory(ToDir);
            foreach (string s1 in Directory.GetFiles(FromDir))
            {
                string s2 = ToDir + "\\" + Path.GetFileName(s1);
                File.Copy(s1, s2);
            }
            foreach (string s in Directory.GetDirectories(FromDir))
            {
                CopyDir(s, ToDir + "\\" + Path.GetFileName(s));
            }
        }



        public string ExportAccountViber()
        {
            string phone = "temp";

            DateTime date1 = DateTime.Now;
            string dt = date1.ToShortDateString();

            Directory.CreateDirectory(Path.GetDirectoryName(Application.ExecutablePath) + "\\accounts\\");
            Directory.CreateDirectory(Path.GetDirectoryName(Application.ExecutablePath) + "\\accounts\\viber\\");
            Directory.CreateDirectory(Path.GetDirectoryName(Application.ExecutablePath) + "\\accounts\\viber\\" + dt + "\\");
            Directory.CreateDirectory(Path.GetDirectoryName(Application.ExecutablePath) + "\\accounts\\viber\\" + dt + "\\" + phone + "\\");
            Directory.CreateDirectory(Path.GetDirectoryName(Application.ExecutablePath) + "\\accounts\\viber\\" + dt + "\\" + phone + "\\com.viber.voip\\");

            string prefix = "\\accounts\\viber\\" + dt + "\\" + phone + "\\";

            ExtractFolder( "pull /data/data/com.viber.voip", prefix);
            ExtractFolder( "pull /data/data/com.viber.voip/cache/", prefix + "\\com.viber.voip\\");
            ExtractFolder( "pull /data/data/com.viber.voip/code_cache/", prefix + "\\com.viber.voip\\");
            ExtractFolder( "pull /data/data/com.viber.voip/databases/", prefix + "\\com.viber.voip\\");
            ExtractFolder( "pull /data/data/com.viber.voip/files/", prefix + "\\com.viber.voip\\");
            ExtractFolder( "pull /data/data/com.viber.voip/lib/", prefix + "\\com.viber.voip\\");
            ExtractFolder( "pull /data/data/com.viber.voip/no_backup/", prefix + "\\com.viber.voip\\");
            ExtractFolder( "pull /data/data/com.viber.voip/shared_prefs/", prefix + "\\com.viber.voip\\");
            ExtractFolder( "pull /storage/emulated/legacy/viber", prefix);

            string dbname = Path.GetDirectoryName(Application.ExecutablePath) + prefix + "\\com.viber.voip\\databases\\viber_messages";

            string req = "SELECT number from participants_info WHERE _id=1"; 
            List<string> fieldsList = new List<string> { "number" };
            phone = SqliteRequestTable(dbname, req, fieldsList);
            phone = phone.Replace("\r\n", "");
            write_log("Получили номер телефона аккаунта " + phone);
            
            try
            {
                CopyDir(Path.GetDirectoryName(Application.ExecutablePath) + "\\accounts\\viber\\" + dt + "\\temp",
                        Path.GetDirectoryName(Application.ExecutablePath) + "\\accounts\\viber\\" + dt + "\\" + phone);
                MakeReport(dt, phone);
            }
            catch (Exception ex)
            {
                write_log(ex.Message);
            }

            Console.ReadLine();
            return dbname;
        }


        public string ExportAccountWhatsApp()
        {
            Random rnd = new Random();
            string rand_str ="";
            for (int i = 0; i < 5; i++)
            {
                rand_str += rnd.Next(0, 9).ToString();
            }

            string temp_phone = "temp_" + rand_str;

            DateTime date1 = DateTime.Now;
            string dt = date1.ToShortDateString();

            Directory.CreateDirectory(Path.GetDirectoryName(Application.ExecutablePath) + "\\accounts\\");
            Directory.CreateDirectory(Path.GetDirectoryName(Application.ExecutablePath) + "\\accounts\\whatsapp\\");
            Directory.CreateDirectory(Path.GetDirectoryName(Application.ExecutablePath) + "\\accounts\\whatsapp\\" + dt + "\\");
            Directory.CreateDirectory(Path.GetDirectoryName(Application.ExecutablePath) + "\\accounts\\whatsapp\\" + dt + "\\" + temp_phone + "\\");
            Directory.CreateDirectory(Path.GetDirectoryName(Application.ExecutablePath) + "\\accounts\\whatsapp\\" + dt + "\\" + temp_phone + "\\com.whatsapp\\");

            string prefix = "\\accounts\\whatsapp\\" + dt + "\\" + temp_phone + "\\";

            ExtractFolder("pull /data/data/com.whatsapp", prefix);
            ExtractFolder("pull /data/data/com.whatsapp/cache/", prefix + "\\com.whatsapp\\");
            ExtractFolder("pull /data/data/com.whatsapp/code_cache/", prefix + "\\com.whatsapp\\");
            ExtractFolder("pull /data/data/com.whatsapp/databases/", prefix + "\\com.whatsapp\\");
            ExtractFolder("pull /data/data/com.whatsapp/files/", prefix + "\\com.whatsapp\\");
            ExtractFolder("pull /data/data/com.whatsapp/lib/", prefix + "\\com.whatsapp\\");
            ExtractFolder("pull /data/data/com.whatsapp/no_backup/", prefix + "\\com.whatsapp\\");
            ExtractFolder("pull /data/data/com.whatsapp/shared_prefs/", prefix + "\\com.whatsapp\\");
            ExtractFolder("pull /storage/emulated/legacy/WhatsApp", prefix);


            string fname = Path.GetDirectoryName(Application.ExecutablePath)  + prefix + "\\com.whatsapp\\files\\me";
            write_log("Файл с номером телефона: " + fname);

            if (File.Exists(fname))
            {

                string s = File.ReadAllText(fname);
                string[] words = s.Split(new char[] { '\0' });
                string phone = words[words.Length - 1];
                phone = phone.Replace("\n", "");

                write_log("Получили номер телефона аккаунта " + phone);

                try
                {
                    CopyDir(Path.GetDirectoryName(Application.ExecutablePath) + "\\accounts\\whatsapp\\" + dt + "\\" + temp_phone,
                            Path.GetDirectoryName(Application.ExecutablePath) + "\\accounts\\whatsapp\\" + dt + "\\" + phone);
                    //MakeReport(dt, phone);
                }
                catch (Exception ex)
                {
                    write_log(ex.Message);
                }
                return phone;
            }
            else
            {
                write_log("Файл с номером телефона не найден. Экспорт не делаем Выход.");
                return "error";
            }
        }
        

        public void PushFile(string fname, string path)
        {
            Start(new[] { "push", "\"" + fname + "\"", "\"" + path + "\"" }, null, s => Program.OnLog(this, s), e => Program.OnLog(this, e));            
        }
        public void PerformAdbCommand(string s)
        {
            s = "-s " + device + " " + s;
            if (s.IndexOf("sleep") > 0)
            {
                s = s.Substring(s.IndexOf("sleep") + 6, s.Length - s.IndexOf("sleep") - 6);
                int interval = Convert.ToInt32(s);
                Thread.Sleep(interval);
            }
            else
            {
                if (s.IndexOf("$") == -1 && s.IndexOf("!") == -1)
                {
                    Process _process = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo();

                    if (emulator == "nox")
                    {
                        startInfo.FileName = nox_path + "\\bin\\nox_adb.exe";
                    }
                    if (emulator == "memu")
                    {
                        startInfo.FileName = memu_path + "\\adb.exe";
                    }

                    startInfo.CreateNoWindow = true;
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    s = s.Substring(0, s.IndexOf("#"));
                    startInfo.Arguments = s;

                    _process.StartInfo = startInfo;
                    _process.Start();
                }
            }
        }

        public void InstallAPK(string apk)
        {
            write_log("InstallAPK device=" + device + " apk=" + apk);
            string path = Path.GetDirectoryName(Application.ExecutablePath) + "\\apk\\";
            string cmd = "install \"" + path + apk + "\" # установка exposed";
            //MessageBox.Show(cmd);
            PerformAdbCommand(cmd);
        }

        public void InstallAPK1(string apk, string[] arg = null)
        {
            if (!File.Exists(apk))
            {
                string path = Path.GetDirectoryName(Application.ExecutablePath) + "\\apk\\";
                apk = path + apk;
            }
            if (!File.Exists(apk)) { Program.OnLog(null, "File.Exists:" + apk); return; }
            arg = arg ?? new string[] { "-r" };
            //MessageBox.Show(cmd);
            Start(new[] { "install" }.Concat(arg).Concat(new[] { "\"" + apk + "\"" }).ToArray(), null, s => Program.OnLog(this, s), e => Program.OnLog(this, e));
        }
        //AFTER PUSH!!!
        public void InstallAPK2(string apk)
        {
            write_log("InstallAPK device=" + device + " apk=" + apk);
            //MessageBox.Show(cmd);
            Start(new[] { "shell", "pm", "install", "-t", "-r", "\"" + apk + "\"" }, null, s => Program.OnLog(this, s), e => Program.OnLog(this, e));
        }
               

        public void ActionVM(string vm, string action)
        {
            write_log("ActionVM" + vm);
            Process _process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            string nox_path = ConfigurationManager.AppSettings["nox_path"];

            startInfo.FileName = nox_path + "\\bin\\Nox.exe";
            string apk_path = Path.GetDirectoryName(Application.ExecutablePath) + "\\apk";

            string args = "-clone:" + vm + " -title:" + vm;

            if (action != "")
                args += " " + action;

            startInfo.Arguments = args;

            _process.StartInfo = startInfo;
            _process.Start();
        }

        public void RebootVM(string vm_name)
        {
            ActionVM(vm_name, " -quit");
            Thread.Sleep(3000);
            ActionVM(vm_name, "");
        }

        public void ExtractFolder(string s, string path)
        {
            string prefix = "-s " + device + " ";
            s = prefix + s;

            Process _process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.FileName = nox_path + "\\bin\\nox_adb.exe";
            startInfo.WorkingDirectory = Path.GetDirectoryName(Application.ExecutablePath) + path;

            //startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            if (s.IndexOf("#") > 0)
                s = s.Substring(0, s.IndexOf("#"));
            startInfo.Arguments = s;

            _process.StartInfo = startInfo;
            _process.Start();
            _process.WaitForExit();
        }

        public void deleteFolder(string folder)
        {
            try
            {
                //Класс DirectoryInfo как раз позволяет работать с папками. Создаём объект этого
                //класса, в качестве параметра передав путь до папки.
                DirectoryInfo di = new DirectoryInfo(folder);
                //Создаём массив дочерних вложенных директорий директории di
                DirectoryInfo[] diA = di.GetDirectories();
                //Создаём массив дочерних файлов директории di
                FileInfo[] fi = di.GetFiles();
                //В цикле пробегаемся по всем файлам директории di и удаляем их
                foreach (FileInfo f in fi)
                {
                    f.Delete();
                }

                //В цикле пробегаемся по всем вложенным директориям директории di 
                foreach (DirectoryInfo df in diA)
                {
                    //Как раз пошла рекурсия
                    deleteFolder(df.FullName);
                    //Если в папке нет больше вложенных папок и файлов - удаляем её,
                    if (df.GetDirectories().Length == 0 && df.GetFiles().Length == 0) df.Delete();
                }
            }
            //Начинаем перехватывать ошибки
            //DirectoryNotFoundException - директория не найдена
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine("Директория не найдена. Ошибка: " + ex.Message);
            }
            //UnauthorizedAccessException - отсутствует доступ к файлу или папке
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine("Отсутствует доступ. Ошибка: " + ex.Message);
            }
            //Во всех остальных случаях
            catch (Exception ex)
            {
                Console.WriteLine("Произошла ошибка. Обратитесь к администратору. Ошибка: " + ex.Message);
            }
        }
        

        public string SqliteRequestValue(string dbname, string scmd)
        {
            using (SQLiteConnection conn = new SQLiteConnection("Data Source=" + dbname))
            {
                conn.Open();
                SQLiteCommand cmd = new SQLiteCommand(conn);

                cmd.CommandText = scmd;
                object amount = 0;
                try
                {
                    amount = cmd.ExecuteScalar();
                }
                catch (SQLiteException ex)
                {
                    Console.WriteLine(ex.Message);
                }

                return amount.ToString();
            }
        }

        public string SqliteRequestTable(string dbname, string scmd, List<string> slist)
        {
            string sout = "";

            using (SQLiteConnection conn = new SQLiteConnection("Data Source=" + dbname))
            {
                try
                {
                    conn.Open();

                }
                catch (Exception e)
                {
                    return "ERROR" + e.Message;
                }
                SQLiteCommand cmd = new SQLiteCommand(conn);

                cmd.CommandText = scmd;
                try
                {
                    SQLiteDataReader r = cmd.ExecuteReader();
                    string line = String.Empty;
                    while (r.Read())
                    {
                        line = "";

                        for (int i = 0; i < slist.Count; i++)
                        {
                            string field = slist[i];
                            line += r[field].ToString();
                        }

                        //line = r["address"].ToString() + " status=" + r["status"].ToString() + " " + r["body"].ToString(); ///"display_name"
                        sout += line + "\r\n";
                    }

                    r.Close();
                }
                catch (SQLiteException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            return sout;
        }

        public string MakeReport(string dt, string phone)
        {
            string prefix = "\\accounts\\" + dt + "\\" + phone + "\\";
            string dbname = Path.GetDirectoryName(Application.ExecutablePath) + prefix + "\\com.viber.voip\\databases\\viber_messages";

            string reports_path = Path.GetDirectoryName(Application.ExecutablePath) + "\\reports\\";
            Directory.CreateDirectory(reports_path);

            string req = "SELECT address, status, body FROM messages";
            List<string> fieldsList = new List<string> { "address", "status", "body" };
            string my_report1 = SqliteRequestTable(dbname, req, fieldsList);

            File.WriteAllText(reports_path + phone + "_viber1.txt", my_report1);
            File.AppendAllText(reports_path + dt + "_report.txt", "###################" + DateTime.Now + "###################\r\n" + my_report1);


            req = "SELECT address FROM messages WHERE LENGTH(body) > 2"; /// 
            List<string> fieldsList2 = new List<string> { "address" };
            string my_report2 = SqliteRequestTable(dbname, req, fieldsList2);
            File.AppendAllText(reports_path + dt + "_phones.txt", my_report2); //###################" + DateTime.Now + "###################


            List<string> fieldsList3 = new List<string> { "id", "number", "display_name" };

            req = "SELECT id, number, display_name FROM participants_info"; //address, status, body
            string my_report3 = SqliteRequestTable(dbname, req, fieldsList3);
            File.WriteAllText(reports_path + phone + "_viber2.txt", my_report3);         //MessageBox.Show(dbname);
            return dbname;
        }
              
    }
}
