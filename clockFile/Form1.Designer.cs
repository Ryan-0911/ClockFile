namespace clockFile
{
    partial class Form1
    {
        /// <summary>
        /// 設計工具所需的變數。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清除任何使用中的資源。
        /// </summary>
        /// <param name="disposing">如果應該處置受控資源則為 true，否則為 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 設計工具產生的程式碼

        /// <summary>
        /// 此為設計工具支援所需的方法 - 請勿使用程式碼編輯器修改
        /// 這個方法的內容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.dtpFileDate = new System.Windows.Forms.DateTimePicker();
            this.btnCfmDate = new System.Windows.Forms.Button();
            this.timerAuto = new System.Windows.Forms.Timer(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // dtpFileDate
            // 
            this.dtpFileDate.CalendarFont = new System.Drawing.Font("微軟正黑體", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.dtpFileDate.Font = new System.Drawing.Font("微軟正黑體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.dtpFileDate.Location = new System.Drawing.Point(50, 90);
            this.dtpFileDate.Name = "dtpFileDate";
            this.dtpFileDate.Size = new System.Drawing.Size(299, 29);
            this.dtpFileDate.TabIndex = 0;
            // 
            // btnCfmDate
            // 
            this.btnCfmDate.Font = new System.Drawing.Font("微軟正黑體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.btnCfmDate.Location = new System.Drawing.Point(371, 90);
            this.btnCfmDate.Name = "btnCfmDate";
            this.btnCfmDate.Size = new System.Drawing.Size(99, 33);
            this.btnCfmDate.TabIndex = 1;
            this.btnCfmDate.Text = "確認日期";
            this.btnCfmDate.UseVisualStyleBackColor = true;
            this.btnCfmDate.Click += new System.EventHandler(this.btnCfmDate_Click);
            // 
            // timerAuto
            // 
            this.timerAuto.Enabled = true;
            this.timerAuto.Tick += new System.EventHandler(this.timerAuto_Tick);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("微軟正黑體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label1.ForeColor = System.Drawing.Color.SteelBlue;
            this.label1.Location = new System.Drawing.Point(48, 38);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(187, 20);
            this.label1.TabIndex = 2;
            this.label1.Text = "30秒後自動匯入昨日資料";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(657, 203);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnCfmDate);
            this.Controls.Add(this.dtpFileDate);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DateTimePicker dtpFileDate;
        private System.Windows.Forms.Button btnCfmDate;
        private System.Windows.Forms.Timer timerAuto;
        private System.Windows.Forms.Label label1;
    }
}

