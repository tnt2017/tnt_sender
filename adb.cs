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
    public class adb
    {
        private static string nox_path, memu_path, emulator;


        static adb()
        {
            nox_path = ConfigurationManager.AppSettings["nox_path"];
            memu_path = ConfigurationManager.AppSettings["memu_path"];
        }

        public adb(string emul)
        {
            emulator = emul;           
        }

        void write_log(string s)
        {

        }

        public static void PerformAdbCommand(string device, string s)
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

        public void InstallAPK(string device, string apk)
        {
            write_log("InstallAPK device=" + device + " apk=" + apk);
            string path = Path.GetDirectoryName(Application.ExecutablePath) + "\\apk\\";
            string cmd = "install \"" + path + apk + "\" # установка exposed";
            //MessageBox.Show(cmd);
            PerformAdbCommand(device, cmd);
        }

        public void ExposedConfigure(string device)
        {
            string prefix = "-s " + device + " ";
            PerformAdbCommand(prefix, "shell monkey -p de.robv.android.xposed.installer 1 #запуск xposeda");
            Thread.Sleep(3000);
            PerformAdbCommand(prefix, "shell input tap 540 500 # нажать ок");
            Thread.Sleep(1000);
            PerformAdbCommand(prefix, "shell input tap 310 300 # вставить # установить xposed");
            Thread.Sleep(1000);
            PerformAdbCommand(prefix, "shell input tap 200 350 # нажать Install");


            //PerformAdbCommand(prefix + "shell input tap 100 200 # фреймворк");
            //Thread.Sleep(1000);
            //PerformAdbCommand(prefix + "shell input tap 280 500 # нажать ок");
        }

        public void ChangerProRandomApply(string device, string vmname) // for ex. vm_name=Nox_1
        {
            string prefix = "-s " + device + " ";
            PerformAdbCommand(prefix, "shell monkey -p com.phoneinfo.changerpro 1 #запуск changerpro");
            Thread.Sleep(1000);
            PerformAdbCommand(prefix, "shell input tap 100 500 # random all");
            Thread.Sleep(1000);
            PerformAdbCommand(prefix, "shell input tap 500 500 # apply");
            Thread.Sleep(3000);
            //PerformAdbCommand(prefix + "reboot # перегружаем командой");
            RebootVM(vmname);
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
        
        public void ExtractFolder(string device, string s, string path)
        {
            string prefix = "-s " + device + " ";
            s = prefix + s;

            Process _process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.FileName = nox_path + "\\bin\\nox_adb.exe";
            startInfo.WorkingDirectory = Path.GetDirectoryName(Application.ExecutablePath) + path;

            //startInfo.CreateNoWindow = true;
            //startInfo.WindowStyle = ProcessWindowStyle.Hidden;

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
            string sout="";

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
                        //MessageBox.Show(r.FieldCount.ToString());

                        /*for (int i = 0; i < r.FieldCount; i++)
                        {
                            if(i==0 || i==1 || i==2 || i==5 || i==7)
                            line += r[i].ToString() + "|";
                        }*/

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
                conn.Close();
            }
            

            return sout;
        }
        
        public string MakeReport(string dt, string phone)
        {
            string prefix = "\\accounts\\viber\\" + dt + "\\" + phone + "\\";
            string dbname = Path.GetDirectoryName(Application.ExecutablePath) + prefix + "\\com.viber.voip\\databases\\viber_messages";

            string reports_path = Path.GetDirectoryName(Application.ExecutablePath) + "\\reports\\";
            Directory.CreateDirectory(reports_path);
            
            string req = "SELECT address, status, body FROM messages";
            List<string> fieldsList = new List<string> { "address", "status", "body"}; 
            string my_report1 = SqliteRequestTable(dbname, req, fieldsList);

            File.WriteAllText(reports_path + phone + "_viber1.txt", my_report1);           
            File.AppendAllText(reports_path + dt + "_report.txt","###################" + DateTime.Now + "###################\r\n" + my_report1);


            req = "SELECT address FROM messages WHERE LENGTH(body) > 2"; /// 
            List<string> fieldsList2 = new List<string> { "address" };
            string my_report2 = SqliteRequestTable(dbname, req, fieldsList2);
            File.AppendAllText(reports_path + dt + "_phones.txt",  my_report2); //###################" + DateTime.Now + "###################
            

            List<string> fieldsList3 = new List<string> { "id", "number", "display_name"}; 

            req = "SELECT id, number, display_name FROM participants_info"; //address, status, body
            string my_report3 = SqliteRequestTable(dbname, req, fieldsList3);
            File.WriteAllText(reports_path + phone + "_viber2.txt", my_report3);         //MessageBox.Show(dbname);
            return dbname; 
        }


        void CopyDir(string FromDir, string ToDir)
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

        public string ExportAccountViber(string device)
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

            ExtractFolder(device, "pull /data/data/com.viber.voip", prefix);
            ExtractFolder(device, "pull /data/data/com.viber.voip/cache/", prefix + "\\com.viber.voip\\");
            ExtractFolder(device, "pull /data/data/com.viber.voip/code_cache/", prefix + "\\com.viber.voip\\");
            ExtractFolder(device, "pull /data/data/com.viber.voip/databases/", prefix + "\\com.viber.voip\\");
            ExtractFolder(device, "pull /data/data/com.viber.voip/files/", prefix + "\\com.viber.voip\\");
            ExtractFolder(device, "pull /data/data/com.viber.voip/lib/", prefix + "\\com.viber.voip\\");
            ExtractFolder(device, "pull /data/data/com.viber.voip/no_backup/", prefix + "\\com.viber.voip\\");
            ExtractFolder(device, "pull /data/data/com.viber.voip/shared_prefs/", prefix + "\\com.viber.voip\\");
            ExtractFolder(device, "pull /storage/emulated/legacy/viber", prefix);


            string dbname = Path.GetDirectoryName(Application.ExecutablePath) + prefix + "\\com.viber.voip\\databases\\viber_messages";

            string req = "SELECT number from participants_info WHERE _id=1"; //display_name 
            List<string> fieldsList = new List<string> { "number" }; //"display_name"
            phone = SqliteRequestTable(dbname, req, fieldsList);
            phone = phone.Replace("\r\n", "");
            write_log("Получили номер телефона аккаунта " + phone);


            try
            {
                //MessageBox.Show(phone);
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

        public string ExportAccountWhatsApp(string device)
        {
            string phone = "temp";

            DateTime date1 = DateTime.Now;
            string dt = date1.ToShortDateString();

            Directory.CreateDirectory(Path.GetDirectoryName(Application.ExecutablePath) + "\\accounts\\");
            Directory.CreateDirectory(Path.GetDirectoryName(Application.ExecutablePath) + "\\accounts\\whatsapp\\");
            Directory.CreateDirectory(Path.GetDirectoryName(Application.ExecutablePath) + "\\accounts\\whatsapp\\" + dt + "\\");
            Directory.CreateDirectory(Path.GetDirectoryName(Application.ExecutablePath) + "\\accounts\\whatsapp\\" + dt + "\\" + phone + "\\");
            Directory.CreateDirectory(Path.GetDirectoryName(Application.ExecutablePath) + "\\accounts\\whatsapp\\" + dt + "\\" + phone + "\\com.whatsapp\\");

            string prefix = "\\accounts\\viber\\" + dt + "\\" + phone + "\\";

            ExtractFolder(device, "pull /data/data/com.whatsapp", prefix);
            ExtractFolder(device, "pull /data/data/com.whatsapp/cache/", prefix + "\\com.whatsapp\\");
            ExtractFolder(device, "pull /data/data/com.whatsapp/code_cache/", prefix + "\\com.whatsapp\\");
            ExtractFolder(device, "pull /data/data/com.whatsapp/databases/", prefix + "\\com.whatsapp\\");
            ExtractFolder(device, "pull /data/data/com.whatsapp/files/", prefix + "\\com.whatsapp\\");
            ExtractFolder(device, "pull /data/data/com.whatsapp/lib/", prefix + "\\com.whatsapp\\");
            ExtractFolder(device, "pull /data/data/com.whatsapp/no_backup/", prefix + "\\com.whatsapp\\");
            ExtractFolder(device, "pull /data/data/com.whatsapp/shared_prefs/", prefix + "\\com.whatsapp\\");
            ExtractFolder(device, "pull /storage/emulated/legacy/WhatsApp", prefix);


            //string dbname = Path.GetDirectoryName(Application.ExecutablePath) + prefix + "\\com.whatsapp\\databases\\viber_messages";

            //string req = "SELECT number from participants_info WHERE _id=1"; //display_name 
            //List<string> fieldsList = new List<string> { "number" }; //"display_name"
            //phone = SqliteRequestTable(dbname, req, fieldsList);
            //phone = phone.Replace("\r\n", "");


            Random rnd = new Random();

            phone = "";

            for (int i = 0; i < 10; i++)
            {
                int value = rnd.Next(0, 10);
                phone += value.ToString();
            }

            MessageBox.Show(phone);                 

            write_log("Получили номер телефона аккаунта " + phone);
            
            try
            {
                //MessageBox.Show(phone);
                CopyDir(Path.GetDirectoryName(Application.ExecutablePath) + "\\accounts\\whatsapp\\" + dt + "\\temp",
                        Path.GetDirectoryName(Application.ExecutablePath) + "\\accounts\\whatsapp\\" + dt + "\\" + phone);
                //MakeReport(dt, phone);
            }
            catch (Exception ex)
            {
                write_log(ex.Message);
            }

            Console.ReadLine();
            return phone;
        }


 
    }
}
