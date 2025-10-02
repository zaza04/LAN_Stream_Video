namespace ServerStreamApp
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            disconnectButton = new Button();
            connectButton = new Button();
            videoPanel = new Panel();
            messageTextBox = new TextBox();
            sendButton = new Button();
            chatBox = new TextBox();
            clientBox = new TextBox();
            statusLabel = new Label();
            numClient = new Label();
            SuspendLayout();
            // 
            // disconnectButton
            // 
            disconnectButton.BackColor = Color.FromArgb(255, 128, 128);
            disconnectButton.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            disconnectButton.Location = new Point(1022, 13);
            disconnectButton.Margin = new Padding(3, 4, 3, 4);
            disconnectButton.Name = "disconnectButton";
            disconnectButton.Size = new Size(126, 34);
            disconnectButton.TabIndex = 0;
            disconnectButton.Text = "Ngắt kết nối";
            disconnectButton.UseVisualStyleBackColor = false;
            disconnectButton.Click += disconnectButton_Click;
            // 
            // connectButton
            // 
            connectButton.BackColor = Color.PaleGreen;
            connectButton.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            connectButton.Location = new Point(1154, 13);
            connectButton.Margin = new Padding(3, 4, 3, 4);
            connectButton.Name = "connectButton";
            connectButton.Size = new Size(97, 34);
            connectButton.TabIndex = 1;
            connectButton.Text = "Khởi động";
            connectButton.UseVisualStyleBackColor = false;
            connectButton.Click += connectButton_Click;
            // 
            // videoPanel
            // 
            videoPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            videoPanel.BackColor = Color.Black;
            videoPanel.Location = new Point(14, 55);
            videoPanel.Margin = new Padding(3, 4, 3, 4);
            videoPanel.Name = "videoPanel";
            videoPanel.Size = new Size(726, 454);
            videoPanel.TabIndex = 2;
            // 
            // messageTextBox
            // 
            messageTextBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            messageTextBox.Location = new Point(746, 705);
            messageTextBox.Margin = new Padding(3, 4, 3, 4);
            messageTextBox.Name = "messageTextBox";
            messageTextBox.Size = new Size(415, 27);
            messageTextBox.TabIndex = 4;
            messageTextBox.KeyPress += messageTextBox_KeyPress;
            // 
            // sendButton
            // 
            sendButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            sendButton.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            sendButton.Location = new Point(1169, 701);
            sendButton.Margin = new Padding(3, 4, 3, 4);
            sendButton.Name = "sendButton";
            sendButton.Size = new Size(86, 31);
            sendButton.TabIndex = 5;
            sendButton.Text = "Gửi";
            sendButton.UseVisualStyleBackColor = true;
            sendButton.Click += SendButton_Click;
            // 
            // chatBox
            // 
            chatBox.Location = new Point(746, 54);
            chatBox.Multiline = true;
            chatBox.Name = "chatBox";
            chatBox.ReadOnly = true;
            chatBox.ScrollBars = ScrollBars.Vertical;
            chatBox.Size = new Size(509, 640);
            chatBox.TabIndex = 6;
            // 
            // clientBox
            // 
            clientBox.Location = new Point(14, 516);
            clientBox.Multiline = true;
            clientBox.Name = "clientBox";
            clientBox.ScrollBars = ScrollBars.Vertical;
            clientBox.Size = new Size(726, 216);
            clientBox.TabIndex = 7;
            // 
            // statusLabel
            // 
            statusLabel.AutoSize = true;
            statusLabel.BackColor = Color.Transparent;
            statusLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            statusLabel.ForeColor = Color.Red;
            statusLabel.Location = new Point(14, 20);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(176, 20);
            statusLabel.TabIndex = 8;
            statusLabel.Text = "Trạng thái: Chưa kết nối";
            // 
            // numClient
            // 
            numClient.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            numClient.AutoSize = true;
            numClient.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            numClient.ForeColor = Color.DarkBlue;
            numClient.Location = new Point(842, 20);
            numClient.Name = "numClient";
            numClient.Size = new Size(127, 20);
            numClient.TabIndex = 10;
            numClient.Text = "Số lượng kết nối:";
            numClient.Click += label1_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1267, 751);
            Controls.Add(numClient);
            Controls.Add(statusLabel);
            Controls.Add(clientBox);
            Controls.Add(chatBox);
            Controls.Add(videoPanel);
            Controls.Add(sendButton);
            Controls.Add(messageTextBox);
            Controls.Add(connectButton);
            Controls.Add(disconnectButton);
            Margin = new Padding(3, 4, 3, 4);
            MinimumSize = new Size(683, 758);
            Name = "Form1";
            Text = "Server";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button disconnectButton;
        private Button connectButton;
        private Panel videoPanel;
        private TextBox messageTextBox;
        private Button sendButton;
        private TextBox chatBox;
        private TextBox clientBox;
        private Label statusLabel;
        private Label numClient;
    }
}
