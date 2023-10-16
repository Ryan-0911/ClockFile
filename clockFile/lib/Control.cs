using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace clockFile.lib
{
    internal class Control
    {
        delegate void logHandler(TextBox tb, string text);
        delegate void pgbarHandler(ProgressBar pgbar, int total, int current);

        /// <summary>
        /// 顯示在訊息視窗上
        /// </summary>
        /// <param name="tb"></param>
        /// <param name="text"></param>
        public static void ShowLog(TextBox tb, string text)
        {
            //判斷這個TextBox的物件是否在同一個執行緒上
            if (tb.InvokeRequired)
            {
                //當InvokeRequired為true時，表示在不同的執行緒上，所以進行委派的動作
                logHandler ph = new logHandler(ShowLog);
                tb.Invoke(ph, tb, text);
            }
            else
            {
                //表示在同一個執行緒上了，所以可以正常的呼叫到這個TextBox物件
                //tb.Text = text + tb.Text;
                tb.AppendText(text);
            }
        }

        /// <summary>
        /// 顯示進度條
        /// </summary>
        /// <param name="pgbar"></param>
        /// <param name="total"></param>
        /// <param name="current"></param>
        public static void ShowPgbar(ProgressBar pgbar, int total, int current)
        {
            if (pgbar.InvokeRequired)
            {
                pgbarHandler ph = new pgbarHandler(ShowPgbar);
                pgbar.Invoke(ph, pgbar, total, current);
            }
            else
            {
                pgbar.Maximum = total;
                pgbar.Value = current;
            }
        }
    }
}