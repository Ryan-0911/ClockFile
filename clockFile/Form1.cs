using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace clockFile
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            timerAuto.Start();
            DateTime today = DateTime.Now;
            DateTime yesterday = today.AddDays(-1);
            dtpFileDate.Value = yesterday;
        }

        private void timerAuto_Tick(object sender, EventArgs e)
        {

        }

        private void btnCfmDate_Click(object sender, EventArgs e)
        {
            readClockFile();
        }

        private void readClockFile()
        {
            string line;
            string directoryPath = @"\\192.168.1.34\刷卡資料"; // 要搜索的目錄路徑
            string searchPattern = $"{dtpFileDate.Value.ToString("yyyyMMdd")}*"; // 模糊搜索模式
            DataTable dt = new DataTable();

            try
            {
                // 使用 Directory 来搜索匹配模糊搜索模式的文件
                string[] matchingFiles = Directory.GetFiles(directoryPath, searchPattern);

                // 遍歷匹配到的文件
                foreach (string filePath in matchingFiles)
                {
                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        string[] fields;
                        while ((line = reader.ReadLine()) != null)
                        {
                            fields[0] = line.Substring(0, 10);
                            line.Substring(12, 8), line.Substring(20, 4) };
                        dt.Rows.Add(fields);
                    }

                    using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["mssql_OvertimeCalc"].ConnectionString))
                    {
                        con.Open();

                        SqlCommand cmd = new SqlCommand("INSERT INTO (ID, HoTen, DiaChi) VALUES (@id, @hoten, @diachi)", con);
                        cmd.Parameters.AddWithValue("@id", fields[0].ToString());
                        cmd.Parameters.AddWithValue("@hoten", fields[1].ToString());
                        cmd.Parameters.AddWithValue("@diachi", fields[2].ToString());
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("發生錯誤: " + ex.Message);
            }
        }
    }
}
