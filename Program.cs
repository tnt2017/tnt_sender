using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace tnt_sender
{
    public static class Program   ///// добавил public (может нужно будет убрать)
    {
        public static void OnLog(object sender,string text ) {
            if (Logger != null) Logger(sender, text);
        }

        public delegate void LoggerEventHandler(object sender, string text);

        public static event LoggerEventHandler Logger;
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
