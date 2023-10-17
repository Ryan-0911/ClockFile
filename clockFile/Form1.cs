using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.CompilerServices;

namespace clockFile
{
    public partial class Form1 : Form
    {
        string directorySrcPath = @"\\192.168.1.34\刷卡資料\"; // 讀取檔案的資料夾
        string directoryDestPath = @"\\192.168.1.34\刷卡資料\processed\"; // 讀取完要移至的資料夾
        string line; // 檔案的每一列

        // 失敗所寄發的email內文
        string messageBodyFailure = "";
        string firstLineFail = "<font>以下是處理失敗的打卡資料</font><br><br>";
        string htmlTableStart = "<table style=\"border-collapse:collapse; text-align:center;\"></table>";
        string htmlTableEnd = "</table>";
        string htmlHeaderRowStart = "<tr style=\"background-color:#6FA1D2; color:#ffffff;\">";
        string htmlHeaderRowEnd = "</tr>";
        string htmlTrStart = "<tr style=\"color:#555555\">";
        string htmlTrEnd = "</tr>";
        string htmlTdStart = "<td style=\"border-color:#5c87b2; border-style:solid; border-width:thin; padding: 5px;\">";
        string htmlTdEnd = "</td>";

        // 成功所寄發的email內文
        string messageBodySuccess = "";
        string firstLineSuccess = "<font>成功匯入打卡資料</font><br><br>";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // 啟用定時器
            timerAuto.Start();
        }

        private void timerAuto_Tick(object sender, EventArgs e)
        {
            // 讀取檔案的資料夾中有檔案才執行
            String[] filePath = Directory.GetFiles(directorySrcPath, "*.txt");
            if (filePath.Length >= 0)
            {
                lblMode.Text = "自動模式";
                readClockFile();
            }
        }

        private void EmailReset()
        {
            // 清空郵件訊息
            messageBodyFailure = "";
            messageBodySuccess = "";

            // 設定email內文
            messageBodyFailure += htmlTableStart;
            messageBodyFailure += htmlHeaderRowStart;
            messageBodyFailure += htmlTdStart + "卡號" + htmlTdEnd;
            messageBodyFailure += htmlTdStart + "打卡日期" + htmlTdEnd;
            messageBodyFailure += htmlTdStart + "打卡時間" + htmlTdEnd;
            messageBodyFailure += htmlTdStart + "錯誤原因" + htmlTdEnd;
            messageBodyFailure += htmlHeaderRowEnd;
        }

        private void btnCfmDate_Click(object sender, EventArgs e)
        {
            // 若使用者手動處理的話，要先確認讀取檔案的資料夾中有無檔案，沒有就直接退出函式
            String[] filePath = Directory.GetFiles(directorySrcPath, "*.txt");
            if (filePath.Length >= 0)
            {
                lblMode.Text = "手動模式";
                readClockFile();
            }
            else
            {
                MessageBox.Show("目前尚無打卡資料", "操作說明", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
        }

        private void readClockFile()
        {

            // 用來儲存文件切割完的字串
            DataTable dt = new DataTable();
            dt.Columns.Add("card_no"); // 卡號
            dt.Columns.Add("clock_date"); // 打卡日期
            dt.Columns.Add("clock_time"); // 打卡時間

            try
            {
                foreach (String f in Directory.GetFiles(directorySrcPath, "*.txt"))
                {
                    // 清空 dataTable
                    dt.Clear();

                    // 重設郵件內文
                    EmailReset();

                    // 將txt檔案寫入dataTable
                    using (StreamReader reader = new StreamReader(f))
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

                    // 將處理好的txt檔案移至\\192.168.1.34\刷卡資料\processed\    
                    File.Move(f, directoryDestPath + $"{Path.GetFileNameWithoutExtension(f)}-{DateTime.Now.ToString("HHmmss")}{Path.GetExtension(f)}");

                    // 將進度條歸零
                    pgBar.Value = 0;
                    // 進度條累加變數
                    int i = 0;

                    // 將txt檔資料寫入資料庫
                    using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["mssql_OvertimeCalc"].ConnectionString))
                    {
                        SqlCommand cmd;
                        SqlDataReader reader;
                        con.Open();

                        foreach (DataRow dr in dt.Rows)
                        {
                            try
                            {
                                lib.Control.ShowLog(tbLog, $"{i + 1}、讀取{dr["card_no"]}/{dr["clock_date"]}/{dr["clock_time"]} \r\n");

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

                                // 寫入 pddat 打卡資料表
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
                                cmd.Parameters.AddWithValue("@moduserno", DBNull.Value);
                                cmd.Parameters.AddWithValue("@moddate", DBNull.Value);
                                cmd.Parameters.AddWithValue("@paymark1", paymark1);
                                cmd.Parameters.AddWithValue("@paymark4", paymark4);
                                cmd.Parameters.AddWithValue("@paymark5", paymark5);
                                cmd.Parameters.AddWithValue("@deptno", deptno);
                                cmd.Parameters.AddWithValue("@userno", ""); // 待解決
                                cmd.Parameters.AddWithValue("@pcsfh001", DBNull.Value);
                                cmd.Parameters.AddWithValue("@pcsfh002", DBNull.Value);
                                cmd.Parameters.AddWithValue("@pcsfh003", DBNull.Value);
                                cmd.Parameters.AddWithValue("@remark", DBNull.Value); // 待解決
                                cmd.Parameters.AddWithValue("@cnvsta", DBNull.Value);
                                cmd.Parameters.AddWithValue("@cardiocode", ""); // 待解決
                                cmd.Parameters.AddWithValue("@prjno", "");
                                cmd.Parameters.AddWithValue("@gps", "");
                                cmd.Parameters.AddWithValue("@flowyn", DBNull.Value);
                                cmd.Parameters.AddWithValue("@bodytemp", 0.0); // 待解決
                                cmd.ExecuteNonQuery();

                                // 更新進度條
                                i++;
                                lib.Control.ShowPgbar(pgBar, dt.Rows.Count, i);
                            }
                            catch (Exception ex)
                            {
                                lib.Control.ShowLog(tbLog, $"發生錯誤: {ex.Message.ToString()} \r\n");
                                messageBodyFailure += htmlTrStart;
                                messageBodyFailure += htmlTdEnd + dr["card_no"].ToString() + htmlTdEnd;
                                messageBodyFailure += htmlTdStart + dr["clock_date"].ToString() + htmlTdEnd;
                                messageBodyFailure += htmlTdStart + dr["clock_time"].ToString() + htmlTdEnd;
                                messageBodyFailure += htmlTdStart + ex.Message.ToString() + htmlTdEnd;
                            }
                        }
                    }
                    lib.Control.ShowPgbar(pgBar, dt.Rows.Count, i);
                    pgBar.Value = 0;
                }

                List<string> recievers = new List<string>();
                recievers.Add("yak30257987@gmail.com");

                // 發送成功郵件
                if (messageBodyFailure != "")
                {
                    EmailSender emailSender = new SuccessEmailSender();
                    emailSender.SendEmail(recievers, "打卡資料處理成功通知", messageBodySuccess);
                }
                // 發送失敗郵件
                else
                {
                    EmailSender emailSender = new FailureEmailSender();
                    emailSender.SendEmail(recievers, "打卡資料處理失敗通知", messageBodyFailure);
                }
            }
            catch (Exception ex)
            {
                lib.Control.ShowLog(tbLog, $"發生錯誤: {ex.Message} \r\n");
            }
        }
    }
}