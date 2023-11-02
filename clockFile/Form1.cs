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

        //// 成功所寄發的email內文
        //string messageBodySuccess = "";
        //string firstLineSuccess = "<font>成功匯入打卡資料</font><br><br>";

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

            // 設定email內文
            messageBodyFailure += firstLineFail;
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

            // 移動後的檔案路徑
            string finalFilePath;

            // 處理狀態是否有異常
            bool fail;

            try
            {
                foreach (String f in Directory.GetFiles(directorySrcPath, "*.txt"))
                {
                    // 設為空字串
                    finalFilePath = "";

                    // 移動後的檔案路徑
                    finalFilePath = directoryDestPath + $"{Path.GetFileNameWithoutExtension(f)}-{DateTime.Now.ToString("HHmmss")}{Path.GetExtension(f)}";

                    lib.Control.ShowLog(tbLog, $"將【{f}】讀入DataTable \r\n");

                    // 清空 dataTable
                    dt.Clear();

                    // 處理狀態是否異常
                    fail = false;

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

                    // 因為同一卡號可能連續打卡兩次，相同兩筆只保留第一筆
                    var distinctDt = dt.AsEnumerable()
                                       .Where((row, index) => index == 0 || 
                                            ! dt.AsEnumerable()
                                                .Take(index)
                                                .Any(prevRow =>
                                                        row.Field<string>("card_no") == prevRow.Field<string>("card_no") &&
                                                        row.Field<string>("clock_date") == prevRow.Field<string>("clock_date") &&
                                                        row.Field<string>("clock_time") == prevRow.Field<string>("clock_time")))
                                                .CopyToDataTable();

                    // 將進度條歸零
                    pgBar.Value = 0;
                    // 進度條累加變數
                    int i = 0;

                    // 將txt檔資料寫入資料庫
                    lib.Control.ShowLog(tbLog, $"開始將【{f}】寫入資料庫 \r\n");
                    using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["mssql_OvertimeCalc"].ConnectionString))
                    {
                        SqlCommand cmd;
                        SqlDataReader reader;
                        con.Open();

                        foreach (DataRow dr in distinctDt.Rows)
                        {
                            try
                            {
                                lib.Control.ShowLog(tbLog, $"{dr["card_no"]}/{dr["clock_date"]}/{dr["clock_time"]} \r\n");

                                // perno (工號)-------------------------------------------------------------------------------------------------
                                string perno = "";
                                DateTime date = DateTime.ParseExact(dr["clock_date"].ToString(), "yyyyMMdd", null);
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

                                // paymark1、paymark4、paymark5、deptno (部門號)-------------------------------------------------------------------------------------------------
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

                                // worno (班別)-------------------------------------------------------------------------------------------------
                                string workno = "";
                                cmd = new SqlCommand(CommandStrings.selectWorkno, con);
                                cmd.Parameters.AddWithValue("@perno", perno);
                                reader = cmd.ExecuteReader();
                                if (reader.Read())
                                {
                                    workno = reader["workno"].ToString();
                                }
                                reader.Close();

                                // 若查無班別
                                if (workno == "")
                                {
                                    cmd = new SqlCommand(CommandStrings.selectWorknoAlt, con);
                                    cmd.Parameters.AddWithValue("@deptno", deptno);
                                    reader = cmd.ExecuteReader();
                                    if (reader.Read())
                                    {
                                        workno = reader["workno"].ToString();
                                    }
                                    reader.Close();
                                }

                                // pcdcode (時段碼)-------------------------------------------------------------------------------------------------
                                string pcdcode = get_pcdcode(dr, workno);

                                // 寫入 pcddat 
                                cmd = new SqlCommand(CommandStrings.InsertIntoPddat, con);
                                cmd.Parameters.AddWithValue("@facno", "001");
                                cmd.Parameters.AddWithValue("@cardno", dr["card_no"].ToString());
                                cmd.Parameters.AddWithValue("@perno", perno);
                                cmd.Parameters.AddWithValue("@attdate", dr["clock_date"].ToString());
                                cmd.Parameters.AddWithValue("@atttime", dr["clock_time"].ToString());
                                cmd.Parameters.AddWithValue("@attcode", "");
                                cmd.Parameters.AddWithValue("@attfunc", "");
                                cmd.Parameters.AddWithValue("@pcdcode", pcdcode);
                                cmd.Parameters.AddWithValue("@pcddate", dr["clock_date"].ToString());
                                cmd.Parameters.AddWithValue("@workno", workno);
                                cmd.Parameters.AddWithValue("@iocode", "");
                                cmd.Parameters.AddWithValue("@datasource", "0");
                                cmd.Parameters.AddWithValue("@moduserno", DBNull.Value);
                                cmd.Parameters.AddWithValue("@moddate", DBNull.Value);
                                cmd.Parameters.AddWithValue("@paymark1", paymark1);
                                cmd.Parameters.AddWithValue("@paymark4", paymark4);
                                cmd.Parameters.AddWithValue("@paymark5", paymark5);
                                cmd.Parameters.AddWithValue("@deptno", deptno);
                                cmd.Parameters.AddWithValue("@userno", $"BT{dr["clock_date"].ToString().Substring(4)}");
                                cmd.Parameters.AddWithValue("@pcsfh001", DBNull.Value);
                                cmd.Parameters.AddWithValue("@pcsfh002", DBNull.Value);
                                cmd.Parameters.AddWithValue("@pcsfh003", DBNull.Value);
                                cmd.Parameters.AddWithValue("@remark", DBNull.Value);
                                cmd.Parameters.AddWithValue("@cnvsta", DBNull.Value);
                                cmd.Parameters.AddWithValue("@cardiocode", "");
                                cmd.Parameters.AddWithValue("@prjno", "");
                                cmd.Parameters.AddWithValue("@gps", "");
                                cmd.Parameters.AddWithValue("@flowyn", DBNull.Value);
                                cmd.Parameters.AddWithValue("@bodytemp", 0.0);
                                cmd.ExecuteNonQuery();

                                // 寫入 auto_ClockFile 資料表
                                cmd = new SqlCommand(CommandStrings.InsertIntoClockFile, con);
                                cmd.Parameters.AddWithValue("@card_no", dr["card_no"].ToString());
                                cmd.Parameters.AddWithValue("@staff_no", perno);
                                cmd.Parameters.AddWithValue("@clock_date", dr["clock_date"].ToString());
                                cmd.Parameters.AddWithValue("@clock_time", dr["clock_time"].ToString());
                                cmd.Parameters.AddWithValue("@clock_type", pcdcode);
                                cmd.Parameters.AddWithValue("@work_no", workno);
                                cmd.Parameters.AddWithValue("@dept_no", deptno);
                                cmd.Parameters.AddWithValue("@file_path", f);
                                cmd.ExecuteNonQuery();

                                // 更新進度條
                                i++;
                                lib.Control.ShowPgbar(pgBar, dt.Rows.Count, i);
                            }
                            catch (Exception ex)
                            {
                                lib.Control.ShowLog(tbLog, $"發生錯誤: {ex.Message.ToString()} \r\n");
                                // 寫入 auto_ClockFileErrorLog
                                using (SqlConnection conError = new SqlConnection(ConfigurationManager.ConnectionStrings["mssql_OvertimeCalc"].ConnectionString))
                                {
                                    SqlCommand cmdError = new SqlCommand(CommandStrings.InsertIntoClockFileErrorLog, conError);
                                    cmdError.Parameters.AddWithValue("@file_path", f);
                                    cmdError.Parameters.AddWithValue("@error", ex.Message.ToString());
                                    cmdError.Parameters.AddWithValue("@card_no", dr["card_no"]);
                                    cmdError.Parameters.AddWithValue("@clock_date", dr["clock_date"]);
                                    cmdError.Parameters.AddWithValue("@clock_time", dr["clock_time"]);

                                    conError.Open();
                                    cmdError.ExecuteNonQuery();
                                }
                                fail = true;
                                messageBodyFailure += htmlTrStart;
                                messageBodyFailure += htmlTdStart + dr["card_no"].ToString() + htmlTdEnd;
                                messageBodyFailure += htmlTdStart + dr["clock_date"].ToString() + htmlTdEnd;
                                messageBodyFailure += htmlTdStart + dr["clock_time"].ToString() + htmlTdEnd;
                                messageBodyFailure += htmlTdStart + ex.Message.ToString() + htmlTdEnd;
                                messageBodyFailure += htmlTrEnd;
                            }
                        }
                        // 最後一列
                        lib.Control.ShowPgbar(pgBar, dt.Rows.Count, i);
                        pgBar.Value = 0;
                        // 將處理好的txt檔案移至\\192.168.1.34\刷卡資料\processed\    
                        // File.Move(f, finalFilePath);
                        // lib.Control.ShowLog(tbLog, $"已將【{f}】移至【{finalFilePath}】 \r\n");
                    }
                    if (fail)
                    {
                        messageBodyFailure += htmlTableEnd;
                        List<string> f_recievers = new List<string>();
                        f_recievers.Add("yak30257987@gmail.com");
                        f_recievers.Add("karl.hsu@sumeeko.com");
                        EmailSender f_emailSender = new FailureEmailSender();
                        f_emailSender.SendEmail(f_recievers, $"打卡資料匯入異常通知【{f}】", messageBodyFailure);
                    }

                    List<string> i_recievers = new List<string>();
                    i_recievers.Add("allmis@sumeeko.com");
                    EmailSender i_emailSender = new InformEmailSender();
                    if (fail)
                    {
                        i_emailSender.SendEmail(i_recievers, $"打卡資料已匯入完成【{f}】", "有錯誤發生");

                    }
                    else
                    {
                        i_emailSender.SendEmail(i_recievers, $"打卡資料已匯入完成【{f}】", "全部成功");

                    }
                }
                timerAuto.Stop();
            }
            catch (Exception ex)
            {
                lib.Control.ShowLog(tbLog, $"發生錯誤: {ex.Message} \r\n");
            }
        }

        private static string get_pcdcode(DataRow dr, string workno)
        {
            //pcdcode (時段碼)-------------------------------------------------------------------------------------------------
            string pcdcode = "";
            // 打卡時間 Datetime
            DateTime DateTimeClock, DateTime0000, DateTime0030, DateTime0100, DateTime0130, DateTime0200, DateTime0300, DateTime0330,
                DateTime0400, DateTime0430, DateTime0500, DateTime0600, DateTime0630, DateTime0700, DateTime0730, DateTime0800, DateTime0830,
                DateTime0900, DateTime0930, DateTime1200, DateTime1230, DateTime1300, DateTime1530, DateTime1600, DateTime1630, DateTime1700,
                DateTime1730, DateTime1800, DateTime1830, DateTime1900, DateTime1930, DateTime2000, DateTime2030, DateTime2100, DateTime2130,
                DateTime2200, DateTime2300;
            string clockTimeStr = dr["clock_time"].ToString();
            string formattedTime = clockTimeStr.Insert(2, ":");
            DateTime.TryParseExact(formattedTime, "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTimeClock);

            // 00:00 Datetime
            DateTime.TryParseExact("00:00", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime0000);
            // 00:30 Datetime
            DateTime.TryParseExact("00:30", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime0030);
            // 01:00 DateTime
            DateTime.TryParseExact("01:00", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime0100);
            // 01:30 DateTime
            DateTime.TryParseExact("01:30", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime0130);
            // 02:00 DateTime
            DateTime.TryParseExact("02:00", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime0200);
            // 03:00 DateTime
            DateTime.TryParseExact("03:00", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime0300);
            // 03:30 DateTime
            DateTime.TryParseExact("03:30", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime0330);
            // 04:00 DateTime
            DateTime.TryParseExact("04:00", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime0400);
            // 04:30 Datetime
            DateTime.TryParseExact("04:30", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime0430);
            // 05:00 DateTime
            DateTime.TryParseExact("05:00", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime0500);
            // 06:00 DateTime
            DateTime.TryParseExact("06:00", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime0600);
            // 06:30 DateTime
            DateTime.TryParseExact("06:30", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime0630);
            // 07:00 Datetime
            DateTime.TryParseExact("07:00", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime0700);
            // 07:30 Datetime
            DateTime.TryParseExact("07:30", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime0730);
            // 08:00 Datetime
            DateTime.TryParseExact("08:00", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime0800);
            // 08:30 Datetime
            DateTime.TryParseExact("08:30", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime0830);
            // 09:00 DateTime
            DateTime.TryParseExact("09:00", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime0900);
            // 09:30 Datetime
            DateTime.TryParseExact("09:30", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime0930);
            // 12:00 DateTime
            DateTime.TryParseExact("12:00", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime1200);
            // 12:30 DateTime
            DateTime.TryParseExact("12:30", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime1230);
            // 13:00 Datetime
            DateTime.TryParseExact("13:00", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime1300);
            // 15:30 DateTime
            DateTime.TryParseExact("15:30", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime1530);
            // 16:00 Datetime
            DateTime.TryParseExact("16:00", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime1600);
            // 16:30 Datetime
            DateTime.TryParseExact("16:30", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime1630);
            // 17:00 Datetime
            DateTime.TryParseExact("17:00", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime1700);
            // 17:30 Datetime
            DateTime.TryParseExact("17:30", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime1730);
            // 18:00  Datetime
            DateTime.TryParseExact("18:00", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime1800);
            // 18:30 DateTime
            DateTime.TryParseExact("18:30", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime1830);
            // 19:00 DateTime
            DateTime.TryParseExact("19:00", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime1900);
            // 19:30 DateTime
            DateTime.TryParseExact("19:30", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime1930);
            // 20:00 Datetime
            DateTime.TryParseExact("20:00", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime2000);
            // 20:30 DateTime
            DateTime.TryParseExact("20:30", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime2030);
            // 21:00 DateTime
            DateTime.TryParseExact("21:00", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime2100);
            // 21:30 Datetime
            DateTime.TryParseExact("21:30", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime2130);
            // 22:00 DateTime
            DateTime.TryParseExact("22:00", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime2200);
            // 23:00 Datetime
            DateTime.TryParseExact("23:00", "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime2300);

            switch (workno)
            {
                // B0 早班0800-1700(平日)
                // BA 休假0800-1700(日)
                // BT 休假0800-1700(六)
                case "B0":
                case "BA":
                case "BT":
                    if (DateTimeClock <= DateTime0800) { pcdcode = "A"; }
                    else if (DateTimeClock >= DateTime1200 && DateTimeClock <= DateTime1300) { pcdcode = "C4"; }
                    else if (DateTimeClock >= DateTime1700 && DateTimeClock <= DateTime1730) { pcdcode = "F"; }
                    else if (DateTimeClock > DateTime1730) { pcdcode = "G"; }
                    else { pcdcode = "B"; }
                    break;
                // C0 辦0830-1730(平日)
                // CA 休假0830-1730(日)
                case "C0":
                case "CA":
                    if (DateTimeClock <= DateTime0830) { pcdcode = "A"; }
                    else if (DateTimeClock >= DateTime1200 && DateTimeClock <= DateTime1300) { pcdcode = "C4"; }
                    else if (DateTimeClock >= DateTime1730 && DateTimeClock <= DateTime1800) { pcdcode = "F"; }
                    else if (DateTimeClock > DateTime1800) { pcdcode = "G"; }
                    else { pcdcode = "B"; }
                    break;
                // D0 晚班1600-0030(平日)
                // DA 休假1600-0030(日)
                // DT 休假1600-0030(六)
                case "D0":
                case "DA":
                case "DT":
                    if (DateTimeClock <= DateTime1600) { pcdcode = "A"; }
                    else if (DateTimeClock >= DateTime2000 && DateTimeClock <= DateTime2030) { pcdcode = "C4"; }
                    else if (DateTimeClock >= DateTime0030 && DateTimeClock <= DateTime0100) { pcdcode = "F"; }
                    else if (DateTimeClock > DateTime0100) { pcdcode = "G"; }
                    else { pcdcode = "B"; }
                    break;
                // E0 1300-2130(平日)
                // EA 休假1300-2130(日)
                // ET 休假1300-2130(六)
                case "E0":
                case "EA":
                case "ET":
                    if (DateTimeClock <= DateTime1300) { pcdcode = "A"; }
                    else if (DateTimeClock >= DateTime1700 && DateTimeClock <= DateTime1730) { pcdcode = "C4"; }
                    else if (DateTimeClock >= DateTime2130 && DateTimeClock <= DateTime2200) { pcdcode = "F"; }
                    else if (DateTimeClock > DateTime2200) { pcdcode = "G"; }
                    else { pcdcode = "B"; }
                    break;
                // FA 休假2130-0600(日)
                // FT 休假2130-0600(六)
                case "FA":
                case "FT":
                    if (DateTimeClock <= DateTime2130) { pcdcode = "A"; }
                    else if (DateTimeClock >= DateTime0130 && DateTimeClock <= DateTime0200) { pcdcode = "C4"; }
                    else if (DateTimeClock >= DateTime0600 && DateTimeClock <= DateTime0630) { pcdcode = "F"; }
                    else if (DateTimeClock > DateTime0630) { pcdcode = "G"; }
                    else { pcdcode = "B"; }
                    break;
                // G0 現0800-1630(平日)
                // GA 休假0800-1630(日)
                // GT 休假0800-1630(六)
                case "G0":
                case "GA":
                case "GT":
                    if (DateTimeClock <= DateTime0800) { pcdcode = "A"; }
                    else if (DateTimeClock >= DateTime1200 && DateTimeClock <= DateTime1230) { pcdcode = "C4"; }
                    else if (DateTimeClock >= DateTime1630 && DateTimeClock <= DateTime1700) { pcdcode = "F"; }
                    else if (DateTimeClock > DateTime1700) { pcdcode = "G"; }
                    else { pcdcode = "B"; }
                    break;
                // H0 0700-1530(平日)
                // HA 休假0700-1530(日)
                // HT 休假0700-1530(六)
                case "H0":
                case "HA":
                case "HT":
                    if (DateTimeClock <= DateTime0700) { pcdcode = "A"; }
                    else if (DateTimeClock >= DateTime1230 && DateTimeClock <= DateTime1300) { pcdcode = "C4"; }
                    else if (DateTimeClock >= DateTime1530 && DateTimeClock <= DateTime1600) { pcdcode = "F"; }
                    else if (DateTimeClock > DateTime1600) { pcdcode = "G"; }
                    else { pcdcode = "B"; }
                    break;
                // I0 現0930-1800
                // IA 休假0930-1800(日)
                // IT 休假0930-1800(六)
                case "I0":
                case "IA":
                case "IT":
                    if (DateTimeClock <= DateTime0930) { pcdcode = "A"; }
                    else if (DateTimeClock >= DateTime1230 && DateTimeClock <= DateTime1300) { pcdcode = "C4"; }
                    else if (DateTimeClock >= DateTime1800 && DateTimeClock <= DateTime1830) { pcdcode = "F"; }
                    else if (DateTimeClock > DateTime1830) { pcdcode = "G"; }
                    else { pcdcode = "B"; }
                    break;
                // J0 早班0700-1600(平日)
                // JT 休假0700-1600(六)
                case "J0":
                case "JT":
                    if (DateTimeClock <= DateTime0700) { pcdcode = "A"; }
                    else if (DateTimeClock >= DateTime1200 && DateTimeClock <= DateTime1300) { pcdcode = "C4"; }
                    else if (DateTimeClock >= DateTime1600 && DateTimeClock <= DateTime1630) { pcdcode = "F"; }
                    else if (DateTimeClock > DateTime1630) { pcdcode = "G"; }
                    else { pcdcode = "B"; }
                    break;
                // K0 0730-1730(平日)
                // KA 休假0730-1730(六 日)
                case "K0":
                case "KA":
                    if (DateTimeClock <= DateTime0730) { pcdcode = "A"; }
                    else if (DateTimeClock >= DateTime1200 && DateTimeClock <= DateTime1300) { pcdcode = "C4"; }
                    else if (DateTimeClock >= DateTime1730 && DateTimeClock <= DateTime1800) { pcdcode = "F"; }
                    else if (DateTimeClock > DateTime1800) { pcdcode = "G"; }
                    else { pcdcode = "B"; }
                    break;

                // L0 08:30-12:00(平日)
                // LA 08:30-12:00(日)
                // LT 08:30-12:00(六)
                //case "L0":
                //case "LA":
                //case "LT":
                //    break;

                // M0 中班1300-2130
                case "M0":
                    if (DateTimeClock <= DateTime1300) { pcdcode = "A"; }
                    else if (DateTimeClock >= DateTime1900 && DateTimeClock <= DateTime1930) { pcdcode = "C4"; }
                    else if (DateTimeClock >= DateTime2130 && DateTimeClock <= DateTime2200) { pcdcode = "F"; }
                    else if (DateTimeClock > DateTime2200) { pcdcode = "G"; }
                    else { pcdcode = "B"; }
                    break;
                // M1 中班1200-2030(平日)
                // M2 休假1200-2030(六)
                case "M1":
                case "M2":
                    if (DateTimeClock <= DateTime1200) { pcdcode = "A"; }
                    else if (DateTimeClock >= DateTime1600 && DateTimeClock <= DateTime1630) { pcdcode = "C4"; }
                    else if (DateTimeClock >= DateTime2030 && DateTimeClock <= DateTime2100) { pcdcode = "F"; }
                    else if (DateTimeClock > DateTime2100) { pcdcode = "G"; }
                    else { pcdcode = "B"; }
                    break;
                // M3 中班1700-2030(平日)
                case "M3":
                    if (DateTimeClock <= DateTime1700) { pcdcode = "A"; }
                    else if (DateTimeClock >= DateTime1900 && DateTimeClock <= DateTime1930) { pcdcode = "C4"; }
                    else if (DateTimeClock >= DateTime2030 && DateTimeClock <= DateTime2100) { pcdcode = "F"; }
                    else if (DateTimeClock > DateTime2100) { pcdcode = "G"; }
                    else { pcdcode = "B"; }
                    break;
                // MA 休假1200-2030(日)
                // MT 休假1200-2030(六)
                case "MA":
                case "MT":
                    if (DateTimeClock <= DateTime1200) { pcdcode = "A"; }
                    else if (DateTimeClock >= DateTime1600 && DateTimeClock <= DateTime1630) { pcdcode = "C4"; }
                    else if (DateTimeClock >= DateTime2030 && DateTimeClock <= DateTime2100) { pcdcode = "F"; }
                    else if (DateTimeClock > DateTime2100) { pcdcode = "G"; }
                    else { pcdcode = "B"; }
                    break;
                // N0 現夜00:00-08:30(平日)
                // NA 休假00:00-08:30(日)
                // NT 休假00:00-08:30(六)
                case "N0":
                case "NA":
                case "NT":
                    if (DateTimeClock <= DateTime0000) { pcdcode = "A"; }
                    else if (DateTimeClock >= DateTime0400 && DateTimeClock <= DateTime0430) { pcdcode = "C4"; }
                    else if (DateTimeClock >= DateTime0830 && DateTimeClock <= DateTime0900) { pcdcode = "F"; }
                    else if (DateTimeClock > DateTime0900) { pcdcode = "G"; }
                    else { pcdcode = "B"; }
                    break;
                // O0 早班0900-1700
                // OA 假日0900-1700(日)
                // OT 假日0900-1700(六)
                case "O0":
                case "OA":
                case "OT":
                    if (DateTimeClock <= DateTime0900) { pcdcode = "A"; }
                    else if (DateTimeClock >= DateTime1200 && DateTimeClock <= DateTime1300) { pcdcode = "C4"; }
                    else if (DateTimeClock >= DateTime1700 && DateTimeClock <= DateTime1730) { pcdcode = "F"; }
                    else if (DateTimeClock > DateTime1730) { pcdcode = "G"; }
                    else { pcdcode = "B"; }
                    break;
                // Q0 早班2300-0730
                // QA 休假2300-0730(日)
                // QT 假日2300-0700(六)
                case "Q0":
                case "QA":
                case "QT":
                    if (DateTimeClock <= DateTime2300) { pcdcode = "A"; }
                    else if (DateTimeClock >= DateTime0300 && DateTimeClock <= DateTime0330) { pcdcode = "C4"; }
                    else if (DateTimeClock >= DateTime0730 && DateTimeClock <= DateTime0800) { pcdcode = "F"; }
                    else if (DateTimeClock > DateTime0800) { pcdcode = "G"; }
                    else { pcdcode = "B"; }
                    break;
                // R0 現夜2000-0430(平日)
                // R1 現夜2000-0430(平日)
                // RA 休假2000-0430(日)
                // RT 休假2000-0430(六)
                case "R0":
                case "R1":
                case "RA":
                case "RT":
                    if (DateTimeClock <= DateTime2000) { pcdcode = "A"; }
                    else if (DateTimeClock >= DateTime0000 && DateTimeClock <= DateTime0030) { pcdcode = "C4"; }
                    else if (DateTimeClock >= DateTime0430 && DateTimeClock <= DateTime0500) { pcdcode = "F"; }
                    else if (DateTimeClock > DateTime0500) { pcdcode = "G"; }
                    else { pcdcode = "B"; }
                    break;
                // S0 產學0800-1900(平日)
                // SA 產學0800-1900(日)
                // ST 產學0800-1900(五六)
                case "S0":
                case "SA":
                case "ST":
                    if (DateTimeClock <= DateTime0800) { pcdcode = "A"; }
                    else if (DateTimeClock >= DateTime1230 && DateTimeClock <= DateTime1300) { pcdcode = "C4"; }
                    else if (DateTimeClock >= DateTime1900 && DateTimeClock <= DateTime1930) { pcdcode = "F"; }
                    else if (DateTimeClock > DateTime1930) { pcdcode = "G"; }
                    else { pcdcode = "B"; }
                    break;
                // T0 熱處理0800-2000(平日)
                // TA 熱處理0800-2000(日)
                // TT 熱處理0800-2000(六)
                case "T0":
                case "TA":
                case "TT":
                    if (DateTimeClock <= DateTime0800) { pcdcode = "A"; }
                    else if (DateTimeClock >= DateTime1200 && DateTimeClock <= DateTime1230) { pcdcode = "C4"; }
                    else if (DateTimeClock >= DateTime2000 && DateTimeClock <= DateTime2030) { pcdcode = "F"; }
                    else if (DateTimeClock > DateTime2030) { pcdcode = "G"; }
                    else { pcdcode = "B"; }
                    break;
                // U0 熱處理2000-0800(平日)
                // UA 熱處理2000-0800(日)
                // UT 熱處理2000-0800(六)
                case "U0":
                case "UA":
                case "UT":
                    if (DateTimeClock <= DateTime2000) { pcdcode = "A"; }
                    else if (DateTimeClock >= DateTime0000 && DateTimeClock <= DateTime0030) { pcdcode = "C4"; }
                    else if (DateTimeClock >= DateTime0800 && DateTimeClock <= DateTime0830) { pcdcode = "F"; }
                    else if (DateTimeClock > DateTime0830) { pcdcode = "G"; }
                    else { pcdcode = "B"; }
                    break;
            }
            return pcdcode;
        }
    }
}