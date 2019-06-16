using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.ComponentModel;


namespace tnt_sender
{
    class my_sms
    {
        public string apikey;
        public string apiurl;

        public my_sms(string aurl, string akey)
        {
            apiurl = aurl;
            apikey = akey;
        }

        public void output1(string text)
        {
            //Program.Form1.log.AppendText(text + Environment.NewLine);
        }

        public void write_log(string s)
        {
            //Invoke((MethodInvoker)(() => output1(DateTime.Now + " :: " + s)));
            //Application.DoEvents();
        }
        
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


        public string get_phone(string service, string country)
        {
            //string apiurl = ConfigurationManager.AppSettings["apiurl1"];
            //string apikey = ConfigurationManager.AppSettings["apikey1"];
            string Answer = GET(apiurl, "api_key=" + apikey + "&action=getNumber&service=" + service + "&forward=0&country=" + country); 
            return Answer;
        }

        public string get_code(string opid)
        {
            //string apiurl = ConfigurationManager.AppSettings["apiurl1"];
            //string apikey = ConfigurationManager.AppSettings["apikey1"];

            string code = "";
            for (; ; )
            {
                string Answer = GET(apiurl, "api_key=" + apikey + "&action=getStatus&id=" + opid);
                ///textBox_code.Text = (Answer);

                if (Answer.IndexOf(":") > 0)
                {
                    code = Answer.Substring(Answer.IndexOf(":") + 1, 6);
                    break;
                }

                Thread.Sleep(1000);
            }

            return code;
        }


        public void china_login()
        {
            write_log("Логинимся в Китае");
            string auth = GET("http://tnt-nets.ru/sms/", "action=loginIn&name=DimaMar333&password=DenisAlina2018");
            write_log(auth);
        }
        
        public string get_china_phone(string sid)
        {
            start:

            string host = "http://tnt-nets.ru/sms/";
            string apikey = "51c4fc3e2c923fa4d9aa3fd8d762e21e";
            string sparams = "action=getPhone&token=" + apikey + "&sid=" + sid + "&mylogin=" + Form1.GetHDDSerial();


            string answer = GET(host, sparams);
            var dataspt = answer.Split('|');
            string ret = dataspt[1];

            if (ret == "token过期或错误请重新登陆")
            {
                write_log("Надо залогиниться в Китае ");
                china_login();
                goto start;
            }

            if (ret.Substring(0, 3) == "170")
            {
                write_log("Плохой номер на 170, получаем новый");
                goto start;
            }

            return ret;
        }


       /* public string get_tnt_phone(int service_num)
        {
            string host = "http://tnt-nets.ru/china-test/";
            string answer = GET(host, "req=getphone" + service_num.ToString());
            MessageBox.Show(answer);
            return answer;
        }*/


        public string get_china_code(string sid, string phone)
        {
            string host = "http://tnt-nets.ru/sms/";
            string apikey = "51c4fc3e2c923fa4d9aa3fd8d762e21e";

            string code = "";
            int time_counter = 0;

            for (; ; )
            {
                time_counter++;
                string answer = GET(host, "action=getMessage&token=" + apikey + "&sid=" + sid + "&phone=" + phone);
                if (answer.IndexOf(":") > 0)
                {
                    code = answer.Substring(answer.IndexOf(":") + 2, 6);
                    break;
                }

                Thread.Sleep(1000);

                if (time_counter > 60)
                    return "timeout";
            }

            return code;
        }




        public string sms_reg_getnum()
        {
            string answer1 = GET("http://api.sms-reg.com/getNum.php", "country=ru&service=viber&apikey=e748abvqgu1q3x5y3sae8a1obdhg5cll");
            string tzid = answer1;
            tzid = tzid.Substring(tzid.IndexOf("tzid") + 7, 8);
            return tzid;
        }



        public string sms_reg_getstate(string tzid)
        {
            int counter = 0;
            MessageBox.Show(tzid);

            start:

            string answer = GET("http://api.sms-reg.com/getState.php", "tzid=" + tzid + "&apikey=e748abvqgu1q3x5y3sae8a1obdhg5cll");
            //textBox7.Text = answer;

            if (answer.IndexOf("number") > 0)
            {
                string number = answer.Substring(answer.IndexOf("number") + 9, 10);
                MessageBox.Show(number + " за " + counter.ToString() + "секунд");
                return number;
            }
            else
            {
                counter++;
                Thread.Sleep(1000);
                Application.DoEvents();
                goto start;
            }
        }






    }
}
