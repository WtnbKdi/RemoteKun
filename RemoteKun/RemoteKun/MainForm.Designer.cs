namespace RemoteKun
{
    partial class MainForm
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.startButton = new System.Windows.Forms.Button();
            this.ipAddrTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.PortNumlabel = new System.Windows.Forms.Label();
            this.portTextBox = new System.Windows.Forms.TextBox();
            this.msgTextBox = new System.Windows.Forms.TextBox();
            this.sendMsgButton = new System.Windows.Forms.Button();
            this.msgListBox = new System.Windows.Forms.ListBox();
            this.desktopReqButton = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox
            // 
            this.pictureBox.BackColor = System.Drawing.SystemColors.ButtonShadow;
            this.pictureBox.Location = new System.Drawing.Point(12, 59);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(992, 516);
            this.pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox.TabIndex = 0;
            this.pictureBox.TabStop = false;
            this.pictureBox.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseDoubleClick);
            this.pictureBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseDown);
            this.pictureBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseUp);
            // 
            // startButton
            // 
            this.startButton.Location = new System.Drawing.Point(477, 4);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(101, 23);
            this.startButton.TabIndex = 1;
            this.startButton.Text = "開始";
            this.startButton.UseVisualStyleBackColor = true;
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            // 
            // ipAddrTextBox
            // 
            this.ipAddrTextBox.Location = new System.Drawing.Point(69, 6);
            this.ipAddrTextBox.Name = "ipAddrTextBox";
            this.ipAddrTextBox.Size = new System.Drawing.Size(157, 19);
            this.ipAddrTextBox.TabIndex = 2;
            this.ipAddrTextBox.Text = "10.0.4.66";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(51, 12);
            this.label1.TabIndex = 3;
            this.label1.Text = "IPアドレス";
            // 
            // PortNumlabel
            // 
            this.PortNumlabel.AutoSize = true;
            this.PortNumlabel.Location = new System.Drawing.Point(251, 9);
            this.PortNumlabel.Name = "PortNumlabel";
            this.PortNumlabel.Size = new System.Drawing.Size(57, 12);
            this.PortNumlabel.TabIndex = 4;
            this.PortNumlabel.Text = "ポート番号";
            // 
            // portTextBox
            // 
            this.portTextBox.Location = new System.Drawing.Point(314, 6);
            this.portTextBox.Name = "portTextBox";
            this.portTextBox.Size = new System.Drawing.Size(157, 19);
            this.portTextBox.TabIndex = 5;
            this.portTextBox.Text = "12345";
            // 
            // msgTextBox
            // 
            this.msgTextBox.Location = new System.Drawing.Point(68, 31);
            this.msgTextBox.Name = "msgTextBox";
            this.msgTextBox.Size = new System.Drawing.Size(403, 19);
            this.msgTextBox.TabIndex = 6;
            // 
            // sendMsgButton
            // 
            this.sendMsgButton.Location = new System.Drawing.Point(477, 30);
            this.sendMsgButton.Name = "sendMsgButton";
            this.sendMsgButton.Size = new System.Drawing.Size(101, 23);
            this.sendMsgButton.TabIndex = 7;
            this.sendMsgButton.Text = "送信";
            this.sendMsgButton.UseVisualStyleBackColor = true;
            this.sendMsgButton.Click += new System.EventHandler(this.sendMsgButton_Click);
            // 
            // msgListBox
            // 
            this.msgListBox.FormattingEnabled = true;
            this.msgListBox.ItemHeight = 12;
            this.msgListBox.Location = new System.Drawing.Point(12, 581);
            this.msgListBox.Name = "msgListBox";
            this.msgListBox.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.msgListBox.ScrollAlwaysVisible = true;
            this.msgListBox.Size = new System.Drawing.Size(992, 52);
            this.msgListBox.TabIndex = 8;
            // 
            // desktopReqButton
            // 
            this.desktopReqButton.Location = new System.Drawing.Point(612, 27);
            this.desktopReqButton.Name = "desktopReqButton";
            this.desktopReqButton.Size = new System.Drawing.Size(142, 23);
            this.desktopReqButton.TabIndex = 9;
            this.desktopReqButton.Text = "画面リアルタイムキャプチャ";
            this.desktopReqButton.UseVisualStyleBackColor = true;
            this.desktopReqButton.Click += new System.EventHandler(this.desktopReqButton_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 34);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(50, 12);
            this.label2.TabIndex = 10;
            this.label2.Text = "メッセージ";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1016, 645);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.desktopReqButton);
            this.Controls.Add(this.msgListBox);
            this.Controls.Add(this.sendMsgButton);
            this.Controls.Add(this.msgTextBox);
            this.Controls.Add(this.portTextBox);
            this.Controls.Add(this.PortNumlabel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ipAddrTextBox);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.pictureBox);
            this.Name = "MainForm";
            this.Text = "RemoteKun";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox;
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.TextBox ipAddrTextBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label PortNumlabel;
        private System.Windows.Forms.TextBox portTextBox;
        private System.Windows.Forms.TextBox msgTextBox;
        private System.Windows.Forms.Button sendMsgButton;
        private System.Windows.Forms.ListBox msgListBox;
        private System.Windows.Forms.Button desktopReqButton;
        private System.Windows.Forms.Label label2;
    }
}

