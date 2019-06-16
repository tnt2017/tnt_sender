using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Configuration;

namespace tnt_sender
{
    public class memu
    {
        private string memu_path;

        public memu()
        {
            memu_path = ConfigurationManager.AppSettings["memu_path"];
        }

        public void MemuConsole(string s)
        {
            Process _process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.FileName = memu_path + "MEmu\\memuconsole.exe";
            startInfo.Arguments = s;

            _process.StartInfo = startInfo;
            _process.Start();
        }

        public void SetVMOption(string vm, string s)
        {
            Process _process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.FileName = memu_path + "MEmuHyperv\\MEmuManage.exe";
            startInfo.Arguments = "guestproperty set " + vm + s;

            _process.StartInfo = startInfo;
            _process.Start();
        }

    }
}
