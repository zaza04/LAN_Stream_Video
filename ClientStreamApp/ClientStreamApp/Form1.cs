using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClientStreamApp
{
    public partial class Form1 : Form
    {
        // UDP client
        private UdpClient udpClient;
        private const int SERVER_PORT = 8888;
        private bool isConnected = false;

        // Frame processing
        private Dictionary<long, byte[][]> framePackets = new Dictionary<long, byte[][]>();
        private Dictionary<long, int> framePacketCounts = new Dictionary<long, int>();

        private bool isRecording = false;
        private List<byte[]> recordedFrames = new List<byte[]>();
        private DateTime recordingStartTime;

        // Encryption properties
        public string AESKey { get; private set; } = "";
        public bool EncryptionEnabled { get; private set; } = false;


        public Form1()
        {
            InitializeComponent();

            // Hiển thị form đăng nhập trước
            if (!ShowLoginForm())
            {
                // Người dùng hủy đăng nhập, thoát ứng dụng
                Application.Exit();
                Environment.Exit(0);
                return;
            }

            // Tiếp tục khởi tạo các control khác
            SetupControls();
            // ... các khởi tạo khác
        }

        private void SetupControls()
        {
            // You can initialize or configure your controls here if needed.
            // For example, set default values, visibility, or other properties.
            // If you don't need any specific setup, you can leave this method empty.
        }

        private bool ShowLoginForm()
        {
            using (var loginForm = new LoginForm())
            {
                var result = loginForm.ShowDialog();

                if (result == DialogResult.OK && loginForm.LoginSuccessful)
                {
                    // Lưu thông tin người dùng và token
                    this.CurrentUsername = loginForm.Username;
                    this.CurrentUserId = loginForm.UserId;
                    this.authToken = loginForm.AuthToken;
                    
                    // Lưu thông tin encryption
                    this.AESKey = loginForm.AESKey;
                    this.EncryptionEnabled = loginForm.EncryptionEnabled;

                    // Cập nhật tiêu đề cửa sổ
                    var encryptionStatus = EncryptionEnabled ? "🔐" : "🔓";
                    this.Text = $"Video Client - {CurrentUsername} {encryptionStatus}";
                    
                    // Initialize encryption logging
                    if (EncryptionEnabled)
                    {
                        EncryptionHelper.OnLogMessage += LogEncryptionMessage;
                        LogEncryptionMessage($"✅ Client encryption initialized - AES Key available");
                    }

                    return true;
                }

                return false;
            }
        }
        
        private void LogEncryptionMessage(string message)
        {
            // Filter out verbose logs
            if (message.Contains("Bắt đầu") || 
                message.Contains("thành công - Input:") || 
                message.Contains("thành công - Original:") ||
                message.Contains("Đã trích xuất IV") ||
                message.Contains("Đã tạo IV mới") ||
                message.Contains("IV length:") ||
                message.Contains("Cipher length:"))
            {
                return;
            }
            
            Console.WriteLine($"[CLIENT-ENCRYPTION] {message}");
            // Có thể thêm vào chat box nếu cần
        }

        // Thêm các thuộc tính này vào Form1
        public string CurrentUsername { get; private set; } = "";
        public int CurrentUserId { get; private set; } = 0;
        private string authToken = "";

        private void Form1_Load(object sender, EventArgs e)
        {
            // Khởi tạo form
            // chatTextBox.AppendText("Chào mừng đến với ứng dụng Video Streaming Client\r\n");

            // Apply proper anchoring to controls
            SetupControlAnchors();

            // Thiết lập chế độ hiển thị cho video để lấp đầy khung và co giãn theo kích thước
            videoPictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
            ConfigureButtons();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            // Xử lý khi form thay đổi kích thước
            AdjustControls();
        }

        private void SetupControlAnchors()
        {
            // Thiết lập anchoring để các control tự động điều chỉnh khi form thay đổi kích thước
            videoPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            chatTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            messageTextBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            sendButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        }

        private void ConfigureButtons()
        {
            // Thiết lập trạng thái ban đầu cho các nút
            connectButton.Enabled = true;
            disconnectButton.Enabled = false;
            sendButton.Enabled = false;

            // Đảm bảo các nút có Z-index cao hơn
            connectButton.BringToFront();
            disconnectButton.BringToFront();
            sendButton.BringToFront();

            // Thiết lập trạng thái ban đầu cho recording buttons
            RecordBtn.Enabled = false;
            StopRecordBtn.Enabled = false;
        }

        private void AdjustControls()
        {
            // Điều chỉnh khoảng cách giữa các control
            int spacing = 10;

            // Đảm bảo messageTextBox và sendButton được đặt đúng vị trí
            messageTextBox.Left = chatTextBox.Left;
            messageTextBox.Top = chatTextBox.Bottom + spacing;
            messageTextBox.Width = chatTextBox.Width - sendButton.Width - spacing;

            sendButton.Left = messageTextBox.Right + spacing;
            sendButton.Top = messageTextBox.Top;
        }

        private async void connectButton_Click(object sender, EventArgs e)
        {
            if (isConnected) return;

            framePackets.Clear();
            framePacketCounts.Clear();

            string serverIP = serverIPTextBox.Text.Trim();

            try
            {
                statusLabel.Text = "Trạng thái: Đang kết nối...";
                statusLabel.ForeColor = Color.Orange;
                connectButton.Enabled = false;

                // Chỉ tạo UdpClient một lần, dùng cho mọi thao tác
                if (udpClient != null)
                {
                    try
                    {
                        udpClient.Close();
                        udpClient.Dispose();
                    }
                    catch { }
                    udpClient = null;
                }

                await Task.Delay(1000);

                udpClient = new UdpClient(0);
                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpClient.Client.ReceiveBufferSize = 1024 * 1024;
                udpClient.Connect(serverIP, SERVER_PORT);

                // Gửi REGISTER kèm token
                string registerMessage = $"[REGISTER][{this.authToken}]";
                byte[] registerMsg = Encoding.UTF8.GetBytes(registerMessage);

                for (int i = 0; i < 3; i++)
                {
                    await udpClient.SendAsync(registerMsg, registerMsg.Length);
                    //chatTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Sent REGISTER to server ({i + 1}/3)\r\n");
                    await Task.Delay(100);
                }

                statusLabel.Text = "Trạng thái: Đã kết nối";
                statusLabel.ForeColor = Color.Green;
                disconnectButton.Enabled = true;
                sendButton.Enabled = true;
                isConnected = true;

                await Task.Delay(1000);
                RecordBtn.Enabled = true;

                chatTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Connected to server at {serverIP}:{SERVER_PORT}\r\n");

                _ = ReceiveFramesAsync();
                _ = SendKeepAliveAsync(registerMsg);
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Trạng thái: Chưa kết nối";
                statusLabel.ForeColor = Color.Red;
                connectButton.Enabled = true;

                MessageBox.Show($"Connection error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Thêm phương thức mới để gửi gói tin giữ kết nối
        private async Task SendKeepAliveAsync(byte[] registerMsg)
        {
            while (isConnected && udpClient != null)
            {
                try
                {
                    await Task.Delay(3000); // Gửi mỗi 3 giây
                    if (isConnected && udpClient != null)
                    {
                        await udpClient.SendAsync(registerMsg, registerMsg.Length);
                    }
                }
                catch { /* Bỏ qua lỗi */ }
            }
        }

        // Sửa phương thức disconnect
        private async void disconnectButton_Click(object sender, EventArgs e)
        {
            if (!isConnected || udpClient == null) return;

            try
            {
                // Stop recording if active
                if (isRecording)
                {
                    isRecording = false;
                    recordedFrames.Clear();
                }

                // Đánh dấu đã ngắt kết nối trước để dừng tác vụ giữ kết nối
                isConnected = false;

                // Gửi thông báo hủy đăng ký
                byte[] unregisterMsg = Encoding.UTF8.GetBytes("UNREGISTER");
                await udpClient.SendAsync(unregisterMsg, unregisterMsg.Length);

                // Đóng kết nối
                udpClient.Close();
                udpClient.Dispose(); // Thêm Dispose để giải phóng tài nguyên
                udpClient = null;

                // Cập nhật UI
                statusLabel.Text = "Trạng thái: Chưa kết nối";
                statusLabel.ForeColor = Color.Red;
                connectButton.Enabled = true;
                disconnectButton.Enabled = false;
                sendButton.Enabled = false;

                // Disable recording buttons
                RecordBtn.Enabled = false;
                StopRecordBtn.Enabled = false;

                // Xóa hình
                if (videoPictureBox.Image != null)
                {
                    Image oldImage = videoPictureBox.Image;
                    videoPictureBox.Image = null;
                    oldImage.Dispose();
                }

                // Log
                chatTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Disconnected from server\r\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Disconnect error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

                // Đảm bảo nút connect vẫn được bật lại ngay cả khi có lỗi
                connectButton.Enabled = true;
            }
        }

        private void messageTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                SendMessage();
            }
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            SendMessage();
        }

        private async void SendMessage()
        {
            if (!isConnected || udpClient == null) return;

            if (!string.IsNullOrWhiteSpace(messageTextBox.Text))
            {
                string message = messageTextBox.Text.Trim();

                try
                {
                    // Hiển thị tin nhắn trên giao diện client
                    var encryptionIndicator = EncryptionEnabled ? "🔐" : "🔓";
                    chatTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] You {encryptionIndicator}: {message}\r\n");

                    // Định dạng tin nhắn theo chuẩn server yêu cầu: [CHAT][Tên][Nội dung]
                    string chatMessage = $"[CHAT][{CurrentUsername}][{message}]";
                    byte[] messageData;

                    // Mã hóa tin nhắn nếu encryption được bật
                    if (EncryptionEnabled && !string.IsNullOrEmpty(AESKey))
                    {
                        try
                        {
                            // Mã hóa tin nhắn chat
                            string encryptedMessage = EncryptionHelper.AESEncrypt(chatMessage, AESKey);
                            string finalMessage = $"[ENCRYPTED_CHAT]{encryptedMessage}";
                            messageData = Encoding.UTF8.GetBytes(finalMessage);
                            
                            LogEncryptionMessage($"💬 Encrypted chat sent - Original: {chatMessage.Length}b, Encrypted: {finalMessage.Length}b");
                        }
                        catch (Exception encEx)
                        {
                            LogEncryptionMessage($"❌ Chat encryption failed: {encEx.Message}");
                            // Fallback to unencrypted
                            messageData = Encoding.UTF8.GetBytes(chatMessage);
                        }
                    }
                    else
                    {
                        // Tin nhắn không mã hóa
                        messageData = Encoding.UTF8.GetBytes(chatMessage);
                    }

                    // Gửi tin nhắn đến server
                    await udpClient.SendAsync(messageData, messageData.Length);

                    // Xóa tin nhắn và đặt focus
                    messageTextBox.Clear();
                    messageTextBox.Focus();
                }
                catch (Exception ex)
                {
                    chatTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Send message error: {ex.Message}\r\n");
                }
            }
        }

        private async Task ReceiveFramesAsync()
        {
            try
            {
                while (isConnected && udpClient != null)
                {
                    var result = await udpClient.ReceiveAsync();
                    ProcessReceivedData(result.Buffer);
                }
            }
            catch (ObjectDisposedException)
            {
                // UDP client đã đóng
            }
            catch (Exception ex)
            {
                if (isConnected && !IsDisposed)
                {
                    this.Invoke(new Action(() =>
                    {
                        chatTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Receive data error: {ex.Message}\r\n");
                    }));
                }
            }
        }

        private void ProcessReceivedData(byte[] data)
        {
            try
            {
                string fullMessage = Encoding.UTF8.GetString(data);

                // Kiểm tra xem có phải dữ liệu mã hóa không
                if (fullMessage.StartsWith("[ENCRYPTED_CHAT]"))
                {
                    // Xử lý tin nhắn chat đã mã hóa
                    ProcessEncryptedChatMessage(fullMessage);
                }
                else if (fullMessage.StartsWith("[ENCRYPTED_FRAME]"))
                {
                    // Xử lý frame video đã mã hóa
                    ProcessEncryptedVideoFrame(data);
                }
                else if (fullMessage.StartsWith("[CHAT]"))
                {
                    // Xử lý tin nhắn chat không mã hóa
                    ProcessChatMessage(fullMessage);
                }
                else if (fullMessage.StartsWith("[FRAME]"))
                {
                    // Xử lý frame video không mã hóa
                    ProcessVideoFrame(data);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Process data error: {ex.Message}");
            }
        }

        private void ProcessEncryptedChatMessage(string encryptedMessage)
        {
            if (!EncryptionEnabled || string.IsNullOrEmpty(AESKey))
            {
                LogEncryptionMessage("❌ Nhận encrypted chat nhưng không có khóa AES");
                return;
            }

            try
            {
                // Trích xuất phần mã hóa từ message
                string encryptedContent = encryptedMessage.Substring("[ENCRYPTED_CHAT]".Length);
                
                // Giải mã tin nhắn
                string decryptedMessage = EncryptionHelper.AESDecrypt(encryptedContent, AESKey);
                
                LogEncryptionMessage($"💬 Decrypted chat message received");
                
                // Xử lý tin nhắn đã giải mã
                ProcessChatMessage(decryptedMessage);
            }
            catch (Exception ex)
            {
                LogEncryptionMessage($"❌ Lỗi giải mã tin nhắn chat: {ex.Message}");
            }
        }

        private void ProcessEncryptedVideoFrame(byte[] data)
        {
            if (!EncryptionEnabled || string.IsNullOrEmpty(AESKey))
            {
                LogEncryptionMessage("❌ Nhận encrypted frame nhưng không có khóa AES");
                return;
            }

            try
            {
                string dataString = Encoding.UTF8.GetString(data);
                
                // Parse header: [ENCRYPTED_FRAME][header_length]
                var headerEndIndex = dataString.IndexOf(']', "[ENCRYPTED_FRAME][".Length);
                if (headerEndIndex == -1) return;
                
                var headerLengthStr = dataString.Substring("[ENCRYPTED_FRAME][".Length, 
                    headerEndIndex - "[ENCRYPTED_FRAME][".Length);
                
                if (!int.TryParse(headerLengthStr, out int originalHeaderLength)) return;
                
                // Tính toán vị trí các phần
                int encryptedHeaderStart = "[ENCRYPTED_FRAME][".Length + headerLengthStr.Length + 1; // +1 cho ']'
                int encryptedDataStart = encryptedHeaderStart + originalHeaderLength;
                
                // Trích xuất header gốc
                byte[] originalHeader = new byte[originalHeaderLength];
                Buffer.BlockCopy(data, encryptedHeaderStart, originalHeader, 0, originalHeaderLength);
                
                // Trích xuất dữ liệu mã hóa
                byte[] encryptedFrameData = new byte[data.Length - encryptedDataStart];
                Buffer.BlockCopy(data, encryptedDataStart, encryptedFrameData, 0, encryptedFrameData.Length);
                
                // Giải mã frame data
                byte[] decryptedFrameData = EncryptionHelper.AESDecryptBytes(encryptedFrameData, AESKey);
                
                // Tạo lại packet gốc
                byte[] originalPacket = new byte[originalHeader.Length + decryptedFrameData.Length];
                Buffer.BlockCopy(originalHeader, 0, originalPacket, 0, originalHeader.Length);
                Buffer.BlockCopy(decryptedFrameData, 0, originalPacket, originalHeader.Length, decryptedFrameData.Length);
                
                LogEncryptionMessage($"📹 Decrypted frame - Encrypted: {encryptedFrameData.Length}b, Decrypted: {decryptedFrameData.Length}b");
                
                // Xử lý frame đã giải mã
                ProcessVideoFrame(originalPacket);
            }
            catch (Exception ex)
            {
                LogEncryptionMessage($"❌ Lỗi giải mã video frame: {ex.Message}");
            }
        }

        private void ProcessChatMessage(string message)
        {
            try
            {
                // Parse: [CHAT][Sender][Content]
                int firstClose = message.IndexOf(']');
                if (firstClose < 0) return;

                int secondOpen = message.IndexOf('[', firstClose);
                int secondClose = message.IndexOf(']', secondOpen);
                if (secondOpen < 0 || secondClose < 0) return;

                string sender = message.Substring(secondOpen + 1, secondClose - secondOpen - 1);

                int thirdOpen = message.IndexOf('[', secondClose);
                if (thirdOpen < 0) return;

                string content = message.Substring(thirdOpen + 1);
                if (content.EndsWith("]"))
                    content = content.Substring(0, content.Length - 1);

                // Hiển thị tin nhắn với encryption indicator nếu có
                this.Invoke(new Action(() =>
                {
                    // Kiểm tra xem có phải tin nhắn từ server với encryption indicator không
                    var encryptionIndicator = "";
                    if (sender.StartsWith("🔐 "))
                    {
                        sender = sender.Substring(2); // Remove encryption indicator from sender name
                        encryptionIndicator = "🔐 ";
                    }
                    
                    chatTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {encryptionIndicator}{sender}: {content}\r\n");
                    chatTextBox.ScrollToCaret();
                }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Parse chat error: {ex.Message}");
            }
        }

        private void ProcessVideoFrame(byte[] data)
        {
            try
            {
                // Tìm vị trí kết thúc của header
                int headerEndIndex = -1;
                for (int i = 0; i < Math.Min(100, data.Length - 1); i++)
                {
                    if (data[i] == ']' && data[i + 1] != '[')
                    {
                        headerEndIndex = i + 1;
                        break;
                    }
                }

                if (headerEndIndex <= 0) return;

                // Trích xuất header
                string headerText = Encoding.UTF8.GetString(data, 0, headerEndIndex);

                string headerContent = headerText.Substring(1, headerText.Length - 2);
                string[] headerParts = headerContent.Split(new[] { "][" }, StringSplitOptions.None);

                if (headerParts.Length >= 4)
                {
                    // Lấy thông tin frame
                    long frameId = long.Parse(headerParts[1]);
                    int packetNumber = int.Parse(headerParts[2]);
                    int totalPackets = int.Parse(headerParts[3]);

                    // Lấy dữ liệu hình ảnh
                    byte[] imageData = new byte[data.Length - headerEndIndex];
                    Buffer.BlockCopy(data, headerEndIndex, imageData, 0, imageData.Length);

                    // Lưu trữ gói tin
                    if (!framePackets.ContainsKey(frameId))
                    {
                        framePackets[frameId] = new byte[totalPackets][];
                        framePacketCounts[frameId] = totalPackets;
                    }

                    // Lưu gói tin
                    framePackets[frameId][packetNumber] = imageData;

                    // Kiểm tra hoàn thành
                    bool isComplete = true;
                    for (int i = 0; i < totalPackets; i++)
                    {
                        if (framePackets[frameId][i] == null)
                        {
                            isComplete = false;
                            break;
                        }
                    }

                    // Hiển thị frame nếu đã đủ
                    if (isComplete)
                    {
                        DisplayFrame(frameId);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Process video frame error: {ex.Message}");
            }
        }

        private void DisplayFrame(long frameId)
        {
            try
            {
                // Tính kích thước
                int totalSize = 0;
                foreach (byte[] packet in framePackets[frameId])
                {
                    if (packet != null)
                    {
                        totalSize += packet.Length;
                    }
                }

                // Kết hợp các gói
                byte[] frameData = new byte[totalSize];
                int offset = 0;

                foreach (byte[] packet in framePackets[frameId])
                {
                    if (packet != null)
                    {
                        Buffer.BlockCopy(packet, 0, frameData, offset, packet.Length);
                        offset += packet.Length;
                    }
                }

                // Capture frame nếu đang recording
                if (isRecording)
                {
                    byte[] frameCopy = new byte[frameData.Length];
                    Buffer.BlockCopy(frameData, 0, frameCopy, 0, frameData.Length);
                    recordedFrames.Add(frameCopy);
                }

                // Xóa dữ liệu
                framePackets.Remove(frameId);
                framePacketCounts.Remove(frameId);

                // Chuyển thành hình ảnh
                using (MemoryStream ms = new MemoryStream(frameData))
                {
                    Image frame = Image.FromStream(ms);

                    if (!IsDisposed)
                    {
                        this.Invoke(new Action(() =>
                        {
                            // Cập nhật PictureBox
                            if (videoPictureBox.Image != null)
                            {
                                Image oldImage = videoPictureBox.Image;
                                videoPictureBox.Image = frame;
                                oldImage.Dispose();
                            }
                            else
                            {
                                videoPictureBox.Image = frame;
                            }
                        }));
                    }
                }
            }
            catch
            {
                // Bỏ qua lỗi hiển thị frame
            }
        }



        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Ngắt kết nối khi đóng form
            if (isConnected && udpClient != null)
            {
                try
                {
                    byte[] unregisterMsg = Encoding.UTF8.GetBytes("UNREGISTER");
                    udpClient.Send(unregisterMsg, unregisterMsg.Length);
                    udpClient.Close();
                }
                catch
                {
                    // Bỏ qua lỗi
                }
            }

            base.OnFormClosing(e);
        }

        private void videoPictureBox_Click(object sender, EventArgs e)
        {
            // Add any click handling logic here if needed
        }

        private async void RecordBtn_Click(object sender, EventArgs e)
        {
            if (!isConnected || udpClient == null) return;

            try
            {
                // Disable record button và show loading
                RecordBtn.Enabled = false;
                RecordBtn.Text = "Đang bắt đầu...";

                // Gửi thông báo record start đến server
                string recordMessage = $"[RECORD_START][{CurrentUsername}]";
                byte[] recordData = Encoding.UTF8.GetBytes(recordMessage);
                await udpClient.SendAsync(recordData, recordData.Length);

                // Initialize recording
                recordedFrames.Clear();
                recordingStartTime = DateTime.Now;
                isRecording = true;

                // Timeout 1000ms rồi enable Stop button
                await Task.Delay(1000);

                RecordBtn.Text = "Ghi hình";
                StopRecordBtn.Enabled = true;

                // Log
                chatTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Started recording\r\n");

            }
            catch (Exception ex)
            {
                chatTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Recording start error: {ex.Message}\r\n");
                RecordBtn.Enabled = true;
                RecordBtn.Text = "Ghi hình";
            }
        }

        private async void StopRecordBtn_Click(object sender, EventArgs e)
        {
            if (!isRecording || udpClient == null) return;

            try
            {
                // Gửi thông báo record stop đến server
                string stopMessage = $"[RECORD_STOP][{CurrentUsername}]";
                byte[] stopData = Encoding.UTF8.GetBytes(stopMessage);
                await udpClient.SendAsync(stopData, stopData.Length);

                // Stop recording
                isRecording = false;
                StopRecordBtn.Enabled = false;

                // Tính duration
                var duration = DateTime.Now - recordingStartTime;
                chatTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Recording stopped. Duration: {duration:mm\\:ss}\r\n");

                // Timeout 1000ms
                await Task.Delay(1000);

                // Show save dialog và create video
                if (recordedFrames.Count > 0)
                {
                    await SaveRecordedVideo();
                }
                else
                {
                    chatTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] No frames recorded\r\n");
                }

                // Reset UI
                RecordBtn.Enabled = true;

            }
            catch (Exception ex)
            {
                chatTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Recording stop error: {ex.Message}\r\n");
                RecordBtn.Enabled = true;
                StopRecordBtn.Enabled = false;
            }
        }

        private async Task SaveRecordedVideo()
        {
            try
            {
                // Show save file dialog
                using (SaveFileDialog saveDialog = new SaveFileDialog())
                {
                    saveDialog.Filter = "Video files (*.mp4)|*.mp4|AVI files (*.avi)|*.avi|All files (*.*)|*.*";
                    saveDialog.DefaultExt = "mp4";
                    saveDialog.FileName = $"Recording_{DateTime.Now:yyyyMMdd_HHmmss}";

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        chatTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Saving video... Please wait\r\n");

                        await CreateVideoFile(saveDialog.FileName);

                        chatTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Video saved: {saveDialog.FileName}\r\n");
                        chatTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Total frames: {recordedFrames.Count}\r\n");

                        // Optional: Open file location
                        if (MessageBox.Show("Video saved successfully! Open file location?", "Success",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        {
                            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{saveDialog.FileName}\"");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                chatTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Save video error: {ex.Message}\r\n");
                MessageBox.Show($"Error saving video: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                recordedFrames.Clear();
            }
        }

        private async Task CreateVideoFile(string fileName)
        {
            await Task.Run(() =>
            {
                try
                {
                    // Tạo thư mục temp cho frames
                    string tempDir = Path.Combine(Path.GetTempPath(), $"VideoFrames_{DateTime.Now.Ticks}");
                    Directory.CreateDirectory(tempDir);

                    // Lưu từng frame thành file JPG
                    for (int i = 0; i < recordedFrames.Count; i++)
                    {
                        string framePath = Path.Combine(tempDir, $"frame_{i:D6}.jpg");
                        File.WriteAllBytes(framePath, recordedFrames[i]);
                    }

                    // Sử dụng FFmpeg để tạo MP4
                    string ffmpegPath = Path.Combine(Application.StartupPath, "ffmpeg.exe");
                    string inputPattern = Path.Combine(tempDir, "frame_%06d.jpg");
                    string ffmpegArgs = $"-r 10 -i \"{inputPattern}\" -c:v libx264 -pix_fmt yuv420p -y \"{fileName}\"";

                    var processInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = ffmpegArgs,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using (var process = System.Diagnostics.Process.Start(processInfo))
                    {
                        process.WaitForExit();

                        if (process.ExitCode == 0)
                        {
                            this.Invoke(new Action(() =>
                            {
                                chatTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] MP4 video created successfully\r\n");
                            }));
                        }
                        else
                        {
                            string error = process.StandardError.ReadToEnd();
                            this.Invoke(new Action(() =>
                            {
                                chatTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] FFmpeg error: {error}\r\n");
                            }));
                        }
                    }

                    // Cleanup temp directory
                    Directory.Delete(tempDir, true);
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(() =>
                    {
                        chatTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Create video file error: {ex.Message}\r\n");
                    }));
                }
            });
        }
    }
}