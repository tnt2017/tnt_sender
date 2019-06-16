using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Configuration;
using System.IO;
using System.Windows.Forms;

//using System.Windows.Threading;

namespace tnt_sender
{
    public class nox
    {
        private string nox_path;
        delegate void write_log(string fname, string s);


        public nox() //object param
        {
            nox_path = ConfigurationManager.AppSettings["nox_path"];
            //write_log write_log1 = (write_log)param;
          /// Form1.LastInstance.SetTextLog("123");
        }

 

        public class Thread_Params2
        {
            public string vmname;
            public IList<string> apknames;
            public string resolution;

            public string brand;
            public string model;
            public string manufacturer;

            public int cpu_count;
            public int memory;
        }

        static void vmcreate_thread(object obj)
        {
            Thread_Params2 c = (Thread_Params2) obj;
            string vm_name = c.vmname;

            Process _process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            string nox_path = ConfigurationManager.AppSettings["nox_path"];

            startInfo.FileName = nox_path + "\\bin\\Nox.exe";
            string apk_path = Path.GetDirectoryName(Application.ExecutablePath) + "\\apk";

            var apk_paths = c.apknames.Select(name => apk_path + "\\" + name).ToList();
            startInfo.Arguments = "-clone:" + vm_name + " " + c.resolution  + " " + 
                                  " -root:true \"-title:" +
                                  vm_name + "\" " + string.Join(" ", apk_paths.Select(apk => "-apk:\"" + apk + "\""));

            //MessageBox.Show(apk_path);
            _process.StartInfo = startInfo;
            _process.Start();
        }


        public void create_vms(int n1, int n2, int interval, IList<string> apksname, string vm_params)
        {
            for (int i = n1; i < n2 + 1; i++)
            {
                Thread_Params2 tp = new Thread_Params2();
                tp.vmname = "Nox_" + i.ToString();
                tp.apknames = apksname;
                tp.resolution = vm_params;

                Thread mythread = new Thread(new ParameterizedThreadStart(vmcreate_thread));
                mythread.Start(tp);
                Thread.Sleep(interval);
            }
        }


        public void PerformAdbCommand(string s, bool output)
        {
            Process _process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.FileName = nox_path + "\\bin\\nox_adb.exe";
            //MessageBox.Show(startInfo.FileName);
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            s = s.Substring(0, s.IndexOf("#"));
            startInfo.Arguments = s;

            if (output)
            {
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;
               // _process.OutputDataReceived += new DataReceivedEventHandler(sortOutputHandler);
                _process.EnableRaisingEvents = true;
               // _process.Exited += new EventHandler(whenExitProcess);
            }


            _process.StartInfo = startInfo;
            _process.Start();

            if (output)
                _process.BeginOutputReadLine();
        }


      /*  
        }*/






    }
}
