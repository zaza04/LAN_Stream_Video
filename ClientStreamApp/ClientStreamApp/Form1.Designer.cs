namespace ClientStreamApp
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            statusLabel = new Label();
            serverIPLabel = new Label();
            serverIPTextBox = new TextBox();
            connectButton = new Button();
            disconnectButton = new Button();
            videoPanel = new Panel();
            videoPictureBox = new PictureBox();
            chatTextBox = new TextBox();
            messageTextBox = new TextBox();
            sendButton = new Button();
            RecordBtn = new Button();
            StopRecordBtn = new Button();
            videoPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)videoPictureBox).BeginInit();
            SuspendLayout();
            // 
            // statusLabel
            // 
            statusLabel.AutoSize = true;
            statusLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            statusLabel.ForeColor = Color.Red;
            statusLabel.Location = new Point(14, 12);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(176, 20);
            statusLabel.TabIndex = 0;
            statusLabel.Text = "Trạng thái: Chưa kết nối";
            // 
            // serverIPLabel
            // 
            serverIPLabel.AutoSize = true;
            serverIPLabel.Location = new Point(14, 45);
            serverIPLabel.Name = "serverIPLabel";
            serverIPLabel.Size = new Size(69, 20);
            serverIPLabel.TabIndex = 1;
            serverIPLabel.Text = "Server IP:";
            // 
            // serverIPTextBox
            // 
            serverIPTextBox.Location = new Point(90, 41);
            serverIPTextBox.Margin = new Padding(3, 4, 3, 4);
            serverIPTextBox.Name = "serverIPTextBox";
            serverIPTextBox.Size = new Size(148, 27);
            serverIPTextBox.TabIndex = 2;
            serverIPTextBox.Text = "127.0.0.1";
            // 
            // connectButton
            // 
            connectButton.BackColor = Color.LightGreen;
            connectButton.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            connectButton.Location = new Point(246, 40);
            connectButton.Margin = new Padding(3, 4, 3, 4);
            connectButton.Name = "connectButton";
            connectButton.Size = new Size(86, 35);
            connectButton.TabIndex = 3;
            connectButton.Text = "Kết nối";
            connectButton.UseVisualStyleBackColor = false;
            connectButton.Click += connectButton_Click;
            // 
            // disconnectButton
            // 
            disconnectButton.BackColor = Color.LightCoral;
            disconnectButton.Enabled = false;
            disconnectButton.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            disconnectButton.Location = new Point(338, 40);
            disconnectButton.Margin = new Padding(3, 4, 3, 4);
            disconnectButton.Name = "disconnectButton";
            disconnectButton.Size = new Size(110, 35);
            disconnectButton.TabIndex = 4;
            disconnectButton.Text = "Ngắt kết nối";
            disconnectButton.UseVisualStyleBackColor = false;
            disconnectButton.Click += disconnectButton_Click;
            // 
            // videoPanel
            // 
            videoPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            videoPanel.BackColor = Color.Black;
            videoPanel.BorderStyle = BorderStyle.FixedSingle;
            videoPanel.Controls.Add(videoPictureBox);
            videoPanel.Location = new Point(14, 87);
            videoPanel.Margin = new Padding(3, 4, 3, 4);
            videoPanel.Name = "videoPanel";
            videoPanel.Size = new Size(548, 479);
            videoPanel.TabIndex = 5;
            // 
            // videoPictureBox
            // 
            videoPictureBox.BackColor = Color.Black;
            videoPictureBox.Dock = DockStyle.Fill;
            videoPictureBox.Location = new Point(0, 0);
            videoPictureBox.Margin = new Padding(3, 4, 3, 4);
            videoPictureBox.Name = "videoPictureBox";
            videoPictureBox.Size = new Size(546, 477);
            videoPictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
            videoPictureBox.TabIndex = 0;
            videoPictureBox.TabStop = false;
            videoPictureBox.Click += videoPictureBox_Click;
            // 
            // chatTextBox
            // 
            chatTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            chatTextBox.BackColor = Color.White;
            chatTextBox.Font = new Font("Consolas", 9F);
            chatTextBox.Location = new Point(583, 87);
            chatTextBox.Margin = new Padding(3, 4, 3, 4);
            chatTextBox.Multiline = true;
            chatTextBox.Name = "chatTextBox";
            chatTextBox.ReadOnly = true;
            chatTextBox.ScrollBars = ScrollBars.Vertical;
            chatTextBox.Size = new Size(319, 479);
            chatTextBox.TabIndex = 6;
            // 
            // messageTextBox
            // 
            messageTextBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            messageTextBox.Font = new Font("Segoe UI", 10F);
            messageTextBox.Location = new Point(583, 579);
            messageTextBox.Margin = new Padding(3, 4, 3, 4);
            messageTextBox.Name = "messageTextBox";
            messageTextBox.Size = new Size(250, 30);
            messageTextBox.TabIndex = 7;
            messageTextBox.KeyPress += messageTextBox_KeyPress;
            // 
            // sendButton
            // 
            sendButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            sendButton.BackColor = Color.LightBlue;
            sendButton.Enabled = false;
            sendButton.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            sendButton.Location = new Point(833, 579);
            sendButton.Margin = new Padding(3, 4, 3, 4);
            sendButton.Name = "sendButton";
            sendButton.Size = new Size(69, 33);
            sendButton.TabIndex = 8;
            sendButton.Text = "Gửi";
            sendButton.UseVisualStyleBackColor = false;
            sendButton.Click += sendButton_Click;
            // 
            // RecordBtn
            // 
            RecordBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            RecordBtn.BackColor = Color.OrangeRed;
            RecordBtn.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            RecordBtn.ForeColor = SystemColors.ButtonFace;
            RecordBtn.Location = new Point(15, 579);
            RecordBtn.Name = "RecordBtn";
            RecordBtn.Size = new Size(94, 33);
            RecordBtn.TabIndex = 9;
            RecordBtn.Text = "Ghi hình";
            RecordBtn.UseVisualStyleBackColor = false;
            RecordBtn.Click += RecordBtn_Click;
            // 
            // StopRecordBtn
            // 
            StopRecordBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            StopRecordBtn.BackColor = Color.Gray;
            StopRecordBtn.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            StopRecordBtn.ForeColor = SystemColors.ButtonHighlight;
            StopRecordBtn.Location = new Point(115, 579);
            StopRecordBtn.Name = "StopRecordBtn";
            StopRecordBtn.Size = new Size(94, 33);
            StopRecordBtn.TabIndex = 10;
            StopRecordBtn.Text = "Dừng ghi";
            StopRecordBtn.UseVisualStyleBackColor = false;
            StopRecordBtn.Click += StopRecordBtn_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(919, 628);
            Controls.Add(StopRecordBtn);
            Controls.Add(RecordBtn);
            Controls.Add(sendButton);
            Controls.Add(messageTextBox);
            Controls.Add(chatTextBox);
            Controls.Add(videoPanel);
            Controls.Add(disconnectButton);
            Controls.Add(connectButton);
            Controls.Add(serverIPTextBox);
            Controls.Add(serverIPLabel);
            Controls.Add(statusLabel);
            Margin = new Padding(3, 4, 3, 4);
            MinimumSize = new Size(683, 518);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Client";
            Load += Form1_Load;
            Resize += Form1_Resize;
            videoPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)videoPictureBox).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Label serverIPLabel;
        private System.Windows.Forms.TextBox serverIPTextBox;
        private System.Windows.Forms.Button connectButton;
        private System.Windows.Forms.Button disconnectButton;
        private System.Windows.Forms.Panel videoPanel;
        private System.Windows.Forms.PictureBox videoPictureBox;
        private System.Windows.Forms.TextBox chatTextBox;
        private System.Windows.Forms.TextBox messageTextBox;
        private System.Windows.Forms.Button sendButton;
        private Button RecordBtn;
        private Button StopRecordBtn;
    }
}