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
            string directorySrcPath = @"\\192.168.1.34\刷卡資料\"; // 讀取檔案的資料夾
            string directoryDestPath = @"\\192.168.1.34\刷卡資料\processed\"; // 讀取完要移至的資料夾
            string line; // 檔案的每一列


            // 用來儲存文件切割完的字串
            DataTable dt = new DataTable();
            dt.Columns.Add("card_no"); // 卡號
            dt.Columns.Add("clock_date"); // 打卡日期
            dt.Columns.Add("clock_time"); // 打卡時間

            try
            {
                foreach (String filePath in Directory.GetFiles(directorySrcPath, "*.txt"))
                {
                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        string[] fields = new string[3];
                        while ((line = reader.ReadLine()) != null)
                        {
                            fields[0] = line.Substring(0, 10);
                            fields[1] = line.Substring(12, 8);
                            fields[2] = line.Substring(20, 4);
                            DataRow dr = dt.NewRow();
                            dr["card_no"] = fields[0];
                            dr["clock_date"] = fields[1];
                            dr["clock_time"] = fields[2];
                            dt.Rows.Add(dr);
                        }
                    }
                    File.Move(filePath, directoryDestPath + $"{Path.GetFileNameWithoutExtension(filePath)}-{DateTime.Now.ToString("HHmmss")}{Path.GetExtension(filePath)}"); // 將處理好的檔案移至資料夾 \\192.168.1.34\刷卡資料\processed\    
                }

                // 將進度條歸零
                pgBar.Value = 0;
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["mssql_OvertimeCalc"].ConnectionString))
                {
                    SqlCommand cmd;
                    SqlDataReader reader;
                    con.Open();

                    int i = 0;
                    foreach (DataRow dr in dt.Rows)
                    {
                        // 工號
                        string perno = "";
                        // 將yyyyMMdd格式字符串解析為DateTime對象
                        DateTime date = DateTime.ParseExact(dr["clock_date"].ToString(), "yyyyMMdd", null);
                        // 格式化DateTime對象為'2023-10-16 0:0:0.000'格式的字串
                        string formattedDateStr = date.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        cmd = new SqlCommand(CommandStrings.selectPerno, con);
                        cmd.Parameters.AddWithValue("@cardno", dr["card_no"]);
                        cmd.Parameters.Add("@startDate", SqlDbType.DateTime).Value = formattedDateStr;
                        reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            perno = reader["perno"].ToString();
                        }
                        reader.Close();

                        // paymark1、paymark4、paymark5、部門號
                        string paymark1 = "", paymark4 = "", paymark5 = "", deptno = "";
                        cmd = new SqlCommand(CommandStrings.selectPaymark1Paymark4Paymark5Deptno, con);
                        cmd.Parameters.AddWithValue("@perno", perno);
                        reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            paymark1 = reader["paymark1"].ToString();
                            paymark4 = reader["paymark4"].ToString();
                            paymark5 = reader["paymark5"].ToString();
                            deptno = reader["deptno"].ToString();
                        }
                        reader.Close();

                        // worno
                        string workno = "";
                        cmd = new SqlCommand(CommandStrings.selectWorkno, con);
                        cmd.Parameters.AddWithValue("@perno", perno);
                        reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            workno = reader["workno"].ToString();
                        }
                        reader.Close();

                        cmd = new SqlCommand(CommandStrings.InsertIntoPddat, con);

                        cmd.Parameters.AddWithValue("@facno", "001");
                        cmd.Parameters.AddWithValue("@cardno", dr["card_no"].ToString());
                        cmd.Parameters.AddWithValue("@perno", perno);
                        cmd.Parameters.AddWithValue("@attdate", dr["clock_date"].ToString());
                        cmd.Parameters.AddWithValue("@atttime", dr["clock_time"].ToString());
                        cmd.Parameters.AddWithValue("@attcode", "");
                        cmd.Parameters.AddWithValue("@attfunc", "");
                        cmd.Parameters.AddWithValue("@pcdcode", ""); // 時段碼- 待解決 
                        cmd.Parameters.AddWithValue("@pcddate", dr["clock_date"].ToString());
                        cmd.Parameters.AddWithValue("@workno", workno);
                        cmd.Parameters.AddWithValue("@iocode", ""); // 待解決 (若此欄位有值datasource、remark就有值，且cardiocode、prjno、gps、bodytemp皆為null)
                        cmd.Parameters.AddWithValue("@datasource", ""); // 待解決
                        cmd.Parameters.AddWithValue("@moduserno", null);
                        cmd.Parameters.AddWithValue("@moddate", null);
                        cmd.Parameters.AddWithValue("@paymark1", paymark1);
                        cmd.Parameters.AddWithValue("@paymark4", paymark4);
                        cmd.Parameters.AddWithValue("@paymark5", paymark5);
                        cmd.Parameters.AddWithValue("@deptno", deptno);
                        cmd.Parameters.AddWithValue("@userno", ""); // 待解決
                        cmd.Parameters.AddWithValue("@pcsfh001", null);
                        cmd.Parameters.AddWithValue("@pcsfh002", null);
                        cmd.Parameters.AddWithValue("@pcsfh003", null);
                        cmd.Parameters.AddWithValue("@remark", null); // 待解決
                        cmd.Parameters.AddWithValue("@cnvsta", null);
                        cmd.Parameters.AddWithValue("@cardiocode", ""); // 待解決
                        cmd.Parameters.AddWithValue("@prjno", "");
                        cmd.Parameters.AddWithValue("@gps", "");
                        cmd.Parameters.AddWithValue("@flowyn", null);
                        cmd.Parameters.AddWithValue("@bodytemp", 0.0); // 待解決
                        cmd.ExecuteNonQuery();

                        // 更新進度條
                        i++;
                        lib.Control.ShowPgbar(pgBar, dt.Rows.Count, i);
                    }
                    MessageBox.Show("檔案匯入成功!");
                    pgBar.Value = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("發生錯誤: " + ex.Message);
            }
        }
    }
}