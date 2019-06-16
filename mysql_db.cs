using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;
using System.Configuration;


namespace tnt_sender
{
    public class mysql_db
    {
        MySqlConnection con;
        private string ConnectionString;


        public mysql_db()
        {
            ConnectionString = "server=127.0.0.1;user=root;database=tnt_sender;password=++++++;charset=utf8; Allow Zero Datetime=true;";
            Connect();
        }

        public void Connect()
        {
            try
            {
                con = new MySqlConnection();

                ConnectionString = "server=\"" + ConfigurationManager.AppSettings["mysql_host"] + "\";user=" +
                                   ConfigurationManager.AppSettings["mysql_login"] + ";database=" + ConfigurationManager.AppSettings["mysql_db"] + ";password=" + ConfigurationManager.AppSettings["mysql_pass"] + ";charset=utf8; Allow Zero Datetime=true;";

                con.ConnectionString = ConnectionString;
                MySqlConnectionStringBuilder mysqlCSB = new MySqlConnectionStringBuilder();
                mysqlCSB.ConnectionString = ConnectionString;
                con.Open();
            }
            catch (Exception exception)
            {
                write_log("Не удалось подключиться к mysql серверу " + exception.Message);
            }
        }

        void write_log(string s)
        {

        }

        public void SqlQuery(string sql, string message)
        {
            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    MySqlCommand cmd = new MySqlCommand(sql, con);
                    con.Open();
                    cmd.ExecuteNonQuery();

                    //MessageBox.Show(message);
                }

                catch (Exception ex)
                {
                    write_log(ex.Message);
                }
            }
        }


        public string SqlQueryWithResult(string sql)
        {
            DataTable dt = Get_DataTable(sql);
            if (dt == null)
                return "0";
            else
            {
                if (dt.Rows.Count != 0)
                    return dt.Rows[0][0].ToString();
                else
                    return "0";
            }
        }

        public DataTable Get_DataTable(string queryString)
        {
            DataTable dt = new DataTable();
            MySqlCommand com = new MySqlCommand(queryString, con);

            try
            {
                using (MySqlDataReader dr = com.ExecuteReader())
                {
                    if (dr.HasRows)
                    {
                        dt.Load(dr);
                    }
                }
            }

            catch (Exception ex)
            {
                write_log(ex.Message);
            }
            return dt;
        }

        public DataTable GetPhones(string id, string num, string base_name)
        {
            string q = @"SELECT phone from " + base_name + " WHERE state='0' LIMIT 0, " + num.ToString();
            return Get_DataTable(q);
        }


        public string[] GetPhones(int num, string base_name)
        {
            DataTable dt = GetPhones("", num.ToString(), base_name);

            string[] readText = new string[dt.Rows.Count];

            int i = 0;
            foreach (DataRow row in dt.Rows)
            {
                string phone = Convert.ToString(row["phone"]);
                readText[i] = phone;
                //MessageBox.Show(phone);
                SqlQuery("UPDATE " + base_name + " SET state=1 WHERE phone='" + phone + "'", "");
                i++;
            }

            return readText;
        }


    }
}
