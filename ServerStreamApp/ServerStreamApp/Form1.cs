#nullable disable

using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Text;
using ServerStreamApp.Data;
using ServerStreamApp.Models;
using System.Text.Json;
using Timer = System.Windows.Forms.Timer;

namespace ServerStreamApp
{
    public partial class Form1 : Form
    {
        // Khai báo các biến của lớp
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private PictureBox videoPictureBox;
        private UdpClient udpServer;
        private List<IPEndPoint> connectedClients = new List<IPEndPoint>();
        private bool isStreaming = false;
        private int udpPort = 8888;
        private int maxPacketSize = 65000;
        private int frameCounter = 0;
        private int frameSkip = 2;
        private readonly object clientLock = new object();
        private ImageCodecInfo jpegEncoder;
        private Dictionary<IPEndPoint, ClientStats> clientStats = new Dictionary<IPEndPoint, ClientStats>();
        private Dictionary<string, User> authenticatedUsersByToken = new Dictionary<string, User>();
        private Dictionary<IPEndPoint, User> streamingClients = new Dictionary<IPEndPoint, User>();

        // Encryption management
        private Dictionary<IPEndPoint, string> clientAESKeys = new Dictionary<IPEndPoint, string>();
        private Dictionary<string, string> tokenAESKeys = new Dictionary<string, string>(); // token -> AES key
        private readonly object encryptionLock = new object();

        // Thêm biến này để định danh server
        private readonly string ServerName = "Server";

        // Save Log - REMOVED (integrated into database)
        // private Timer saveLogTimer;
        // private List<string> authenticationLogs = new List<string>();

        private class ClientStats
        {
            public long BytesSent { get; set; }
            public int PacketsSent { get; set; }
            public DateTime ConnectTime { get; set; }
            public DateTime LastActivity { get; set; }

            public ClientStats()
            {
                ConnectTime = DateTime.Now;
                LastActivity = DateTime.Now;
                BytesSent = 0;
                PacketsSent = 0;
            }
        }

        public Form1()
        {
            InitializeComponent();
            SetupControls();
            InitializeVideoCapture();
            jpegEncoder = GetJpegEncoder();
            chatBox.Clear();
            InitializeEncryption();
            UpdateClientCount(); // Initialize client count display
        }

        private void InitializeEncryption()
        {
            try
            {
                // Không subscribe to encryption logging để tránh spam
                // EncryptionHelper.OnLogMessage += LogEncryptionMessage;
                AppendClientBoxLog("🔐 Encryption system initialized");
            }
            catch (Exception ex)
            {
                AppendClientBoxLog($"❌ Encryption initialization failed: {ex.Message}");
            }
        }

        private void LogEncryptionMessage(string message)
        {
            // Chỉ log những sự kiện quan trọng, bỏ qua chi tiết verbose
            if (message.Contains("Bắt đầu") ||
                message.Contains("thành công - Input:") ||
                message.Contains("thành công - Original:") ||
                message.Contains("Encrypted frame sent") ||
                message.Contains("Encrypted chat sent") ||
                message.Contains("Decrypted") ||
                message.Contains("Frame encryption failed") ||
                message.Contains("Chat encryption failed") ||
                message.Contains("linked to new endpoint") ||
                message.Contains("Đã trích xuất IV") ||
                message.Contains("Đã tạo IV mới") ||
                message.Contains("IV length:") ||
                message.Contains("Cipher length:"))
            {
                // Bỏ qua các log chi tiết này
                return;
            }

            // Log encryption events to clientBox instead of chatBox
            AppendClientBoxLog($"[ENCRYPTION] {message}");
        }

        // Method để cập nhật số lượng client kết nối
        private void UpdateClientCount()
        {
            if (numClient.InvokeRequired)
            {
                numClient.Invoke(new Action(UpdateClientCount));
                return;
            }

            int connectedCount = 0;

            lock (clientLock)
            {
                connectedCount = connectedClients.Count;
            }

            numClient.Text = $"Số client kết nối: {connectedCount}";
        }

        // Method để cleanup encryption keys khi client disconnect
        private void CleanupClientEncryption(IPEndPoint clientEndPoint)
        {
            lock (encryptionLock)
            {
                if (clientAESKeys.ContainsKey(clientEndPoint))
                {
                    clientAESKeys.Remove(clientEndPoint);
                    LogEncryptionMessage($"🗑️ Encryption cleaned for {clientEndPoint}");
                }
            }
        }

        // Method để cleanup token-based encryption keys khi logout
        private void CleanupTokenEncryption(string token)
        {
            lock (encryptionLock)
            {
                if (tokenAESKeys.ContainsKey(token))
                {
                    tokenAESKeys.Remove(token);
                    LogEncryptionMessage($"🗑️ Token encryption cleaned: {token}");
                }
            }
        }

        // Method để xử lý encrypted chat từ client
        private void ProcessEncryptedChatFromClient(IPEndPoint sender, string encryptedMessage)
        {
            try
            {
                // Kiểm tra xem client có AES key không
                string aesKey = null;
                lock (encryptionLock)
                {
                    if (!clientAESKeys.TryGetValue(sender, out aesKey))
                    {
                        LogEncryptionMessage($"❌ No encryption key for {sender}");
                        return;
                    }
                }

                // Trích xuất phần encrypted content
                string encryptedContent = encryptedMessage.Substring("[ENCRYPTED_CHAT]".Length);

                // Giải mã tin nhắn
                string decryptedMessage = EncryptionHelper.AESDecrypt(encryptedContent, aesKey);

                // Parse decrypted message: [CHAT][Username][Content]
                string[] parts = decryptedMessage.Split(new[] { "][" }, StringSplitOptions.None);
                if (parts.Length >= 3)
                {
                    // Format: [CHAT][Client][Nội dung]
                    string clientInfo = parts[1].TrimEnd(']');
                    string content = decryptedMessage.Substring(decryptedMessage.IndexOf(parts[2].TrimStart('[')));
                    if (content.EndsWith("]")) content = content.Substring(0, content.Length - 1);

                    // Hiển thị tin nhắn trong chatBox với encryption indicator
                    this.BeginInvoke(new Action(() =>
                    {
                        AppendLog($"🔐 {clientInfo}: {content}");
                    }));

                    // Gửi lại tin nhắn này đến tất cả client khác
                    BroadcastChatMessage(sender, clientInfo, content);
                }
            }
            catch (Exception ex)
            {
                LogEncryptionMessage($"❌ Chat decryption failed for {sender}: {ex.Message}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
            }

            if (udpServer != null)
            {
                isStreaming = false;
                udpServer.Close();
            }

            base.OnFormClosing(e);
        }

        private async Task ListenForClients()
        {
            try
            {
                while (isStreaming)
                {
                    UdpReceiveResult result = await udpServer.ReceiveAsync();
                    string message = Encoding.UTF8.GetString(result.Buffer).Trim();

                    // XỬ LÝ AUTHENTICATION REQUEST từ client  
                    if (message.Contains("AUTH_REQUEST"))
                    {
                        await ProcessAuthRequest(result.RemoteEndPoint, message);
                        continue; // Đảm bảo không xử lý tiếp các nhánh dưới
                    }

                    // Giữ nguyên các chức năng khác
                    if (message.StartsWith("[ENCRYPTED_CHAT]"))
                    {
                        // Xử lý tin nhắn chat đã mã hóa từ client
                        ProcessEncryptedChatFromClient(result.RemoteEndPoint, message);
                    }
                    else if (message.StartsWith("[CHAT]"))
                    {
                        // Phân tích cú pháp tin nhắn
                        string[] parts = message.Split(new[] { "][" }, StringSplitOptions.None);
                        if (parts.Length >= 3)
                        {
                            // Format: [CHAT][Client][Nội dung]
                            string clientInfo = parts[1].TrimEnd(']');
                            string content = message.Substring(message.IndexOf(parts[2].TrimStart('[')));
                            if (content.EndsWith("]")) content = content.Substring(0, content.Length - 1);

                            // Hiển thị tin nhắn trong chatBox
                            this.BeginInvoke(new Action(() =>
                            {
                                AppendLog($"{clientInfo}: {content}");
                            }));

                            // Gửi lại tin nhắn này đến tất cả client khác
                            BroadcastChatMessage(result.RemoteEndPoint, clientInfo, content);
                        }
                    }
                    else if (message.StartsWith("[REGISTER]"))
                    {
                        // Format: [REGISTER][token]
                        string token = message.Substring("[REGISTER]".Length).Trim('[', ']');
                        User user = null;

                        lock (clientLock)
                        {
                            if (authenticatedUsersByToken.TryGetValue(token, out user))
                            {
                                // Xác thực bằng token thành công, gán user cho endpoint mới này
                                streamingClients[result.RemoteEndPoint] = user;
                            }
                        }

                        // Liên kết AES key với endpoint mới nếu có
                        lock (encryptionLock)
                        {
                            if (tokenAESKeys.TryGetValue(token, out string aesKey))
                            {
                                clientAESKeys[result.RemoteEndPoint] = aesKey;
                                // Chỉ log một lần khi user đầu tiên connect với encryption
                                // LogEncryptionMessage($"🔗 Encryption linked to new endpoint {result.RemoteEndPoint}");
                            }
                        }

                        if (user != null)
                        {
                            await ProcessClientRegistration(result.RemoteEndPoint, user);
                        }
                        else
                        {
                            // Log hoặc xử lý token không hợp lệ
                            AppendLog($"❌ Invalid token registration attempt from {result.RemoteEndPoint}");
                        }
                    }
                    else if (message == "UNREGISTER")
                    {
                        string clientIdentifier;
                        User disconnectedUser = null;
                        lock (clientLock)
                        {
                            if (streamingClients.TryGetValue(result.RemoteEndPoint, out User user))
                            {
                                clientIdentifier = $"User '{user.Username}'";
                                disconnectedUser = user;
                            }
                            else
                            {
                                clientIdentifier = $"Client {result.RemoteEndPoint}";
                            }

                            // Cleanup
                            connectedClients.Remove(result.RemoteEndPoint);
                            streamingClients.Remove(result.RemoteEndPoint);
                            clientStats.Remove(result.RemoteEndPoint);
                        }

                        // Log activity DISCONNECT
                        if (disconnectedUser != null)
                        {
                            var dbHelper = new DatabaseHelper();
                            dbHelper.LogActivity(ActivityType.DISCONNECT, disconnectedUser, result.RemoteEndPoint.Address.ToString());
                        }

                        this.BeginInvoke(new Action(() =>
                        {
                            AppendLog($"{clientIdentifier} đã ngắt kết nối lúc {DateTime.Now:HH:mm:ss}");
                            UpdateClientStats();
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                if (!IsDisposed && !Disposing)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        //AppendLog($"Lỗi nhận tin nhắn: {ex.Message}");
                    }));
                }
            }
            finally
            {
                if (isStreaming)
                {
                    await Task.Delay(1000);
                    _ = ListenForClients();
                }
            }
        }

        private async Task ProcessAuthRequest(IPEndPoint clientEndPoint, string message)
        {
            try
            {
                // Deserialize authentication request từ client
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var authRequest = JsonSerializer.Deserialize<AuthMessage>(message, options);

                if (authRequest != null && (authRequest.Type == "AUTH_REQUEST" || authRequest.Type == "AUTH_REQUEST_ENCRYPTED"))
                {
                    // Sử dụng DatabaseHelper để xác thực
                    var dbHelper = new DatabaseHelper();
                    var user = dbHelper.ValidateUser(authRequest.Username, authRequest.Password);

                    AuthMessage authResponse = new AuthMessage();

                    if (user != null)
                    {
                        // Xác thực thành công
                        string token = Guid.NewGuid().ToString();
                        authResponse.Type = "AUTH_SUCCESS";
                        authResponse.Username = user.Username;
                        authResponse.UserId = user.UserId;
                        authResponse.Token = token;
                        authResponse.Message = "Authentication successful";

                        // Xử lý mã hóa nếu client yêu cầu
                        if (authRequest.Type == "AUTH_REQUEST_ENCRYPTED" && !string.IsNullOrEmpty(authRequest.PublicKey))
                        {
                            try
                            {
                                LogEncryptionMessage($"🔐 Encryption setup for {clientEndPoint}");

                                // Tạo khóa AES cho client này
                                string aesKey = EncryptionHelper.GenerateAESKey();

                                // Mã hóa khóa AES bằng public key của client
                                string encryptedAESKey = EncryptionHelper.RSAEncrypt(aesKey, authRequest.PublicKey);

                                // Lưu khóa AES cho client này
                                lock (encryptionLock)
                                {
                                    clientAESKeys[clientEndPoint] = aesKey;
                                    tokenAESKeys[token] = aesKey; // Lưu theo token để persistent
                                }

                                // Thêm thông tin mã hóa vào response
                                authResponse.EncryptedAESKey = encryptedAESKey;
                                authResponse.KeyExchangeStep = "SERVER_ENCRYPTED_AES_KEY";
                                authResponse.IsEncrypted = true;

                                LogEncryptionMessage($"✅ Encrypted connection ready for {clientEndPoint}");
                                authResponse.Message = "Authentication successful with encryption";
                            }
                            catch (Exception encEx)
                            {
                                LogEncryptionMessage($"❌ Encryption setup failed for {clientEndPoint}: {encEx.Message}");
                                authResponse.Message = "Authentication successful but encryption setup failed";
                            }
                        }

                        // Log login vào database
                        dbHelper.LogLogin(user.UserId, clientEndPoint.Address.ToString());

                        // Log activity LOGIN
                        dbHelper.LogActivity(ActivityType.LOGIN, user, clientEndPoint.Address.ToString());

                        this.BeginInvoke(new Action(() =>
                        {
                            AppendLog($"✅ User authenticated: {user.Username} (ID: {user.UserId}) from {clientEndPoint}");
                        }));

                        // Thêm client vào danh sách đã xác thực bằng token
                        lock (clientLock)
                        {
                            authenticatedUsersByToken[token] = user;
                        }
                    }
                    else
                    {
                        // Xác thực thất bại
                        authResponse.Type = "AUTH_FAILED";
                        authResponse.Message = "Invalid username or password";

                        // Log activity AUTH_FAILED
                        dbHelper.LogActivity(ActivityType.AUTH_FAILED, (int?)null, clientEndPoint.Address.ToString());

                        this.BeginInvoke(new Action(() =>
                        {
                            AppendLog($"❌ Authentication failed for: {authRequest.Username} from {clientEndPoint}");
                        }));
                    }

                    // Gửi response về client
                    string responseJson = JsonSerializer.Serialize(authResponse);
                    byte[] responseData = Encoding.UTF8.GetBytes(responseJson);

                    await udpServer.SendAsync(responseData, responseData.Length, clientEndPoint);
                }
            }
            catch (Exception ex)
            {
                this.BeginInvoke(new Action(() =>
                {
                    AppendLog($"Lỗi xử lý authentication: {ex.Message}");
                }));

                // Gửi lỗi về client
                try
                {
                    var errorResponse = new AuthMessage
                    {
                        Type = "AUTH_FAILED",
                        Message = "Server error during authentication"
                    };
                    string errorJson = JsonSerializer.Serialize(errorResponse);
                    byte[] errorData = Encoding.UTF8.GetBytes(errorJson);
                    await udpServer.SendAsync(errorData, errorData.Length, clientEndPoint);
                }
                catch { }
            }
        }

        // Thêm phương thức này vào trong class Form1
        private async Task ProcessClientRegistration(IPEndPoint clientEndPoint, User user)
        {
            bool isFirstRegister = false;
            lock (clientLock)
            {
                if (!connectedClients.Contains(clientEndPoint))
                {
                    connectedClients.Add(clientEndPoint);
                    clientStats[clientEndPoint] = new ClientStats();
                    isFirstRegister = true;
                }
            }

            if (isFirstRegister)
            {
                // Log activity CONNECT
                var dbHelper = new DatabaseHelper();
                dbHelper.LogActivity(ActivityType.CONNECT, user, clientEndPoint.Address.ToString());

                this.BeginInvoke(new Action(() =>
                {
                    AppendLog($"User '{user.Username}' đã kết nối từ {clientEndPoint} lúc {DateTime.Now:HH:mm:ss}");
                    UpdateClientStats();
                }));

                // Gửi xác nhận đăng ký về client (tùy chọn)
                var response = new AuthMessage
                {
                    Type = "REGISTERED",
                    Message = "Đăng ký thành công"
                };
                string responseJson = JsonSerializer.Serialize(response);
                byte[] responseData = Encoding.UTF8.GetBytes(responseJson);
                await udpServer.SendAsync(responseData, responseData.Length, clientEndPoint);
            }
        }

        private void UpdateClientStats()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateClientStats));
                return;
            }

            // Update client count label
            UpdateClientCount();

            // clientBox now only shows encryption logs and technical info
            // Basic client info is moved to dedicated method
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== ENCRYPTION & TECHNICAL LOGS ===");
            sb.AppendLine($"Server Status: {(isStreaming ? "Running" : "Stopped")}");
            sb.AppendLine($"UDP Port: {udpPort}");
            sb.AppendLine();

            clientBox.Text = sb.ToString();
        }

        // New method to append encryption logs to clientBox
        private void AppendClientBoxLog(string message)
        {
            if (clientBox.InvokeRequired)
            {
                clientBox.Invoke(new Action(() => AppendClientBoxLog(message)));
                return;
            }

            clientBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            
            // Auto-scroll to bottom
            clientBox.SelectionStart = clientBox.Text.Length;
            clientBox.ScrollToCaret();
        }

        private void SetupControls()
        {
            // Cấu hình Form
            this.AutoScaleMode = AutoScaleMode.None;
            this.MinimumSize = new Size(800, 600);

            // Thiết lập Anchor cho các controls
            disconnectButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            connectButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            videoPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            videoPanel.Margin = new Padding(10);

            chatBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            chatBox.Margin = new Padding(3);
            chatBox.Multiline = true;
            chatBox.ScrollBars = ScrollBars.Vertical;
            chatBox.ReadOnly = true;

            clientBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            clientBox.Margin = new Padding(3);
            clientBox.Multiline = true;
            clientBox.ScrollBars = ScrollBars.Vertical;

            messageTextBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            messageTextBox.Width = 240;

            sendButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            // Điều chỉnh vị trí
            disconnectButton.Location = new Point(this.ClientSize.Width - 230, 10);
            connectButton.Location = new Point(this.ClientSize.Width - 100, 10);

            videoPanel.Location = new Point(10, 50);
            videoPanel.Size = new Size(this.ClientSize.Width - 370, this.ClientSize.Height - 200);

            chatBox.Location = new Point(this.ClientSize.Width - 350, 50);
            chatBox.Size = new Size(340, this.ClientSize.Height - 150);

            clientBox.Location = new Point(10, this.ClientSize.Height - 140);
            clientBox.Size = new Size(this.ClientSize.Width - 370, 130);

            messageTextBox.Location = new Point(this.ClientSize.Width - 350, this.ClientSize.Height - 70);
            sendButton.Location = new Point(this.ClientSize.Width - 100, this.ClientSize.Height - 70);

            // Xử lý resize form
            this.Resize += (s, e) =>
            {
                disconnectButton.Location = new Point(this.ClientSize.Width - 230, 10);
                connectButton.Location = new Point(this.ClientSize.Width - 100, 10);

                videoPanel.Size = new Size(this.ClientSize.Width - 370, this.ClientSize.Height - 200);

                chatBox.Location = new Point(this.ClientSize.Width - 350, 50);
                chatBox.Size = new Size(340, this.ClientSize.Height - 150);

                clientBox.Location = new Point(10, this.ClientSize.Height - 140);
                clientBox.Size = new Size(this.ClientSize.Width - 370, 130);

                messageTextBox.Location = new Point(this.ClientSize.Width - 350, this.ClientSize.Height - 70);
                sendButton.Location = new Point(this.ClientSize.Width - 100, this.ClientSize.Height - 70);
            };
        }

        private void SendButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(messageTextBox.Text))
            {
                string message = messageTextBox.Text.Trim();

                // Hiển thị tin nhắn của server
                AppendLog($"{ServerName}: {message}");

                // Gửi tin nhắn đến tất cả client đã kết nối
                SendMessageToAllClients(message);

                messageTextBox.Clear();
                messageTextBox.Focus();
            }
        }



        private void messageTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                SendButton_Click(sender, e);
            }
        }

        private void InitializeVideoCapture()
        {
            // Thêm PictureBox vào videoPanel
            videoPictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            videoPanel.Controls.Add(videoPictureBox);

            // Kiểm tra thiết bị camera
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (videoDevices.Count == 0)
            {
                MessageBox.Show("No camera found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                connectButton.Enabled = false;
                return;
            }

            // Khởi tạo video source với camera đầu tiên
            videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
            videoSource.NewFrame += VideoSource_NewFrame;
        }

        // Thay thế phương thức VideoSource_NewFrame hiện tại
        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                // Tính toán xem có cần gửi frame này không
                bool needToSendFrame = isStreaming && connectedClients.Count > 0;
                bool shouldSendThisFrame = false;

                if (needToSendFrame)
                {
                    frameCounter++;
                    shouldSendThisFrame = frameCounter % frameSkip == 0;
                }

                // Luôn hiển thị frame trên UI
                Bitmap uiFrame = (Bitmap)eventArgs.Frame.Clone();
                if (videoPictureBox.InvokeRequired)
                {
                    videoPictureBox.BeginInvoke(new Action(() => UpdateVideoFrame(uiFrame)));
                }
                else
                {
                    UpdateVideoFrame(uiFrame);
                }

                // Chỉ gửi frame khi cần thiết
                if (shouldSendThisFrame)
                {
                    // Clone frame một lần và gửi trên thread riêng biệt
                    Bitmap frameToSend = (Bitmap)eventArgs.Frame.Clone();
                    ThreadPool.QueueUserWorkItem(_ => SendFrameToClients(frameToSend));
                }
            }
            catch (ObjectDisposedException)
            {
                // Xử lý trường hợp form đã đóng
            }
            catch (Exception ex)
            {
                if (!IsDisposed && !Disposing)
                {
                    this.BeginInvoke(new Action(() => AppendLog($"Lỗi xử lý frame: {ex.Message}")));
                }
            }
        }

        // Thay thế phương thức SendFrameToClients hiện tại
        private void SendFrameToClients(Bitmap frame)
        {
            try
            {
                List<IPEndPoint> clientsCopy;
                lock (clientLock)
                {
                    // Chỉ gửi cho client đã đăng ký và đã xác thực
                    clientsCopy = connectedClients
                        .Where(ep => streamingClients.ContainsKey(ep))
                        .ToList();

                    if (clientsCopy.Count == 0)
                    {
                        frame.Dispose();
                        return;
                    }
                }

                using (frame)
                using (var ms = new MemoryStream())
                {
                    using (Bitmap resizedFrame = ResizeImage(frame, 480, 360))
                    using (EncoderParameters encoderParams = new EncoderParameters(1))
                    {
                        encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 40L);
                        resizedFrame.Save(ms, jpegEncoder, encoderParams);

                        byte[] imageBytes = ms.ToArray();
                        string frameId = DateTime.Now.Ticks.ToString();

                        SendFrameData(imageBytes, frameId, clientsCopy);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!IsDisposed && !Disposing)
                {
                    this.BeginInvoke(new Action(() => AppendLog($"Lỗi gửi frame: {ex.Message}")));
                }
            }
        }

        // Thêm phương thức mới này
        private void SendFrameData(byte[] imageBytes, string frameId, List<IPEndPoint> clients)
        {
            if (!isStreaming || udpServer == null || clients.Count == 0)
                return;

            List<IPEndPoint> failedClients = new List<IPEndPoint>();

            if (imageBytes.Length > maxPacketSize)
            {
                // Gửi frame lớn theo từng phần nhỏ
                int totalPackets = (int)Math.Ceiling((double)imageBytes.Length / maxPacketSize);

                for (int i = 0; i < totalPackets; i++)
                {
                    int offset = i * maxPacketSize;
                    int size = Math.Min(maxPacketSize, imageBytes.Length - offset);

                    byte[] frameData = new byte[size];
                    Buffer.BlockCopy(imageBytes, offset, frameData, 0, size);

                    byte[] header = Encoding.UTF8.GetBytes($"[FRAME][{frameId}][{i}][{totalPackets}]");

                    // Mã hóa frame data cho từng client riêng biệt
                    SendEncryptedFrameToClients(header, frameData, clients, failedClients);
                }
            }
            else
            {
                // Gửi frame nhỏ trực tiếp
                byte[] header = Encoding.UTF8.GetBytes($"[FRAME][{frameId}][0][1]");

                // Mã hóa frame data cho từng client riêng biệt
                SendEncryptedFrameToClients(header, imageBytes, clients, failedClients);
            }

            // Xử lý các client thất bại sau khi gửi
            if (failedClients.Count > 0)
            {
                RemoveFailedClients(failedClients);
            }
        }

        // Method mới để gửi encrypted frame data
        private void SendEncryptedFrameToClients(byte[] header, byte[] frameData, List<IPEndPoint> clients, List<IPEndPoint> failedClients)
        {
            foreach (var client in clients)
            {
                try
                {
                    if (udpServer != null && isStreaming)
                    {
                        byte[] finalPacket;

                        // Kiểm tra xem client có sử dụng mã hóa không
                        lock (encryptionLock)
                        {
                            if (clientAESKeys.ContainsKey(client))
                            {
                                // Client sử dụng mã hóa
                                var aesKey = clientAESKeys[client];

                                try
                                {
                                    // Mã hóa frame data
                                    var encryptedFrameData = EncryptionHelper.AESEncryptBytes(frameData, aesKey);

                                    // Tạo header cho encrypted data
                                    byte[] encryptedHeader = Encoding.UTF8.GetBytes($"[ENCRYPTED_FRAME][{header.Length}]");

                                    // Tạo packet: [ENCRYPTED_FRAME][header_length] + original_header + encrypted_data
                                    finalPacket = new byte[encryptedHeader.Length + header.Length + encryptedFrameData.Length];
                                    Buffer.BlockCopy(encryptedHeader, 0, finalPacket, 0, encryptedHeader.Length);
                                    Buffer.BlockCopy(header, 0, finalPacket, encryptedHeader.Length, header.Length);
                                    Buffer.BlockCopy(encryptedFrameData, 0, finalPacket, encryptedHeader.Length + header.Length, encryptedFrameData.Length);

                                    LogEncryptionMessage($"📹 Encrypted frame sent to {client} - Original: {frameData.Length}b, Encrypted: {encryptedFrameData.Length}b");
                                }
                                catch (Exception encEx)
                                {
                                    LogEncryptionMessage($"❌ Frame encryption failed for {client}: {encEx.Message}");
                                    // Fallback to unencrypted
                                    finalPacket = new byte[header.Length + frameData.Length];
                                    Buffer.BlockCopy(header, 0, finalPacket, 0, header.Length);
                                    Buffer.BlockCopy(frameData, 0, finalPacket, header.Length, frameData.Length);
                                }
                            }
                            else
                            {
                                // Client không sử dụng mã hóa
                                finalPacket = new byte[header.Length + frameData.Length];
                                Buffer.BlockCopy(header, 0, finalPacket, 0, header.Length);
                                Buffer.BlockCopy(frameData, 0, finalPacket, header.Length, frameData.Length);
                            }
                        }

                        udpServer.Send(finalPacket, finalPacket.Length, client);

                        // Cập nhật thống kê lưu lượng
                        lock (clientLock)
                        {
                            if (!clientStats.ContainsKey(client))
                            {
                                clientStats[client] = new ClientStats();
                            }

                            clientStats[client].BytesSent += finalPacket.Length;
                            clientStats[client].PacketsSent++;
                            clientStats[client].LastActivity = DateTime.Now;
                        }
                    }
                }
                catch
                {
                    if (!failedClients.Contains(client))
                    {
                        failedClients.Add(client);
                    }
                }
            }
        }

        // Thêm phương thức trợ giúp này để gửi gói tin
        private void SendPacketToClients(byte[] packet, List<IPEndPoint> clients, List<IPEndPoint> failedClients)
        {
            foreach (var client in clients)
            {
                try
                {
                    if (udpServer != null && isStreaming)
                    {
                        udpServer.Send(packet, packet.Length, client);

                        // Cập nhật thống kê lưu lượng
                        lock (clientLock)
                        {
                            if (!clientStats.ContainsKey(client))
                            {
                                clientStats[client] = new ClientStats();
                            }



                            clientStats[client].BytesSent += packet.Length;
                            clientStats[client].PacketsSent++;
                            clientStats[client].LastActivity = DateTime.Now;
                        }
                    }
                }
                catch
                {
                    if (!failedClients.Contains(client))
                    {
                        failedClients.Add(client);
                    }
                }
            }
        }

        // Thêm phương thức này để xóa an toàn các client bị lỗi
        private void RemoveFailedClients(List<IPEndPoint> failedClients)
        {
            if (failedClients.Count == 0) return;

            lock (clientLock)
            {
                foreach (var client in failedClients)
                {
                    connectedClients.Remove(client);
                    clientStats.Remove(client);
                    streamingClients.Remove(client); // Cleanup streaming clients
                }
            }

            // Cleanup encryption keys for disconnected clients
            foreach (var client in failedClients)
            {
                CleanupClientEncryption(client);
            }

            // Sử dụng BeginInvoke bên ngoài khối lock
            if (!IsDisposed && !Disposing)
            {
                this.BeginInvoke(new Action(() =>
                {
                    // Log client disconnections
                    foreach (var client in failedClients)
                    {
                        AppendLog($"🔌 Client {client} đã ngắt kết nối");
                    }
                    UpdateClientStats(); // Chỉ cập nhật clientBox
                }));
            }
        }

        private async void connectButton_Click(object sender, EventArgs e)
        {
            try
            {

                if (videoSource != null && !videoSource.IsRunning)
                {
                    // Vô hiệu hóa nút ngay lập tức để tránh nhấn nhiều lần
                    connectButton.Enabled = false;

                    // Cập nhật trạng thái thành "Đang kết nối..."
                    statusLabel.Text = "Trạng thái: Đang kết nối...";
                    statusLabel.ForeColor = Color.Orange;

                    // Đợi trước khi thực sự kết nối
                    await Task.Delay(100);

                    // Tiến hành kết nối camera
                    videoSource.Start();
                    UpdateConnectionStatus(true);

                    // Khởi động UDP server
                    if (udpServer == null)
                    {
                        InitializeUdpServer();
                    }
                    else
                    {
                        isStreaming = true;
                        UpdateClientStats();
                    }

                    // Log SERVER_START activity
                    var dbHelper = new DatabaseHelper();
                    dbHelper.LogActivity(ActivityType.SERVER_START, (int?)null, "127.0.0.1");

                    // saveLogTimer and saveLog functionality removed - using database logging

                    disconnectButton.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                // Vẫn giữ lại log lỗi camera
                AppendLog($"Camera connection error: {ex.Message}");
                connectButton.Enabled = true;
                UpdateConnectionStatus(false);
            }
        }

        // Sửa phương thức disconnectButton_Click:
        private void disconnectButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (videoSource != null && videoSource.IsRunning)
                {
                    videoSource.SignalToStop();
                    videoSource.WaitForStop();
                    UpdateConnectionStatus(false);

                    // Dừng UDP server
                    StopUdpServer();

                    // Log SERVER_STOP activity
                    var dbHelper = new DatabaseHelper();
                    dbHelper.LogActivity(ActivityType.SERVER_STOP, (int?)null, "127.0.0.1");

                    // saveLog functionality removed - using database logging

                    connectButton.Enabled = true;
                    disconnectButton.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Camera disconnection error: {ex.Message}");
            }
        }

        private void UpdateVideoFrame(Bitmap frame)
        {
            try
            {
                if (videoPictureBox.Image != null)
                {
                    var oldImage = videoPictureBox.Image;
                    videoPictureBox.Image = (Bitmap)frame.Clone();
                    oldImage.Dispose();
                }
                else
                {
                    videoPictureBox.Image = (Bitmap)frame.Clone();
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Frame update error: {ex.Message}");
            }
        }

        private void AppendLog(string message)
        {
            if (chatBox.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => AppendLog(message)));
                return;
            }

            // Filter: Only log connection, authentication, and chat messages to chatBox
            // Technical/encryption logs go to clientBox instead
            if (message.Contains("[ENCRYPTION]") || 
                message.Contains("🔐 Encryption") ||
                message.Contains("❌ Encryption") ||
                message.Contains("✅ Encrypted"))
            {
                // Redirect encryption logs to clientBox
                AppendClientBoxLog(message);
                return;
            }

            chatBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            chatBox.ScrollToCaret();
        }

        private void InitializeUdpServer()
        {
            try
            {
                udpServer = new UdpClient(udpPort);
                udpServer.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                isStreaming = true;

                // Xóa dòng này: AppendLog($"UDP Server started on port {udpPort}");
                // Chỉ hiển thị thông tin trong clientBox
                UpdateClientStats();

                // Bắt đầu một Task để nhận các thông điệp từ clients
                Task.Run(() => ListenForClients());
            }
            catch (Exception ex)
            {
                string errorMsg = $"UDP Server startup error: {ex.Message}";
                AppendLog(errorMsg); // Log the error to the chatBox
                MessageBox.Show(errorMsg, "Server Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                // Reset UI state
                this.BeginInvoke(new Action(() =>
                {
                    UpdateConnectionStatus(false);
                    connectButton.Enabled = true;
                    disconnectButton.Enabled = false;
                }));
            }
        }

        // Phương thức resize hình ảnh
        private Bitmap ResizeImage(Bitmap image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        // Lấy JPEG encoder
        private ImageCodecInfo GetJpegEncoder()
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            return codecs.FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
        }

        // Thêm phương thức UpdateConnectionStatus
        private void UpdateConnectionStatus(bool isConnected)
        {
            if (statusLabel.InvokeRequired)
            {
                statusLabel.Invoke(new Action(() => UpdateConnectionStatus(isConnected)));
                return;
            }

            if (isConnected)
            {
                statusLabel.Text = "Trạng thái: Đã kết nối";
                statusLabel.ForeColor = Color.Green;
            }
            else
            {
                statusLabel.Text = "Trạng thái: Chưa kết nối";
                statusLabel.ForeColor = Color.Red;
            }
        }

        // Thêm phương thức StopUdpServer
        private void StopUdpServer()
        {
            isStreaming = false;

            if (udpServer != null)
            {
                try
                {
                    udpServer.Close();
                    udpServer = null;
                }
                catch (Exception ex)
                {
                    AppendLog($"Lỗi khi dừng UDP server: {ex.Message}");
                }
            }

            // Xóa danh sách client và thống kê
            lock (clientLock)
            {
                connectedClients.Clear();
                clientStats.Clear();
            }

            // Cập nhật hiển thị
            if (!IsDisposed && !Disposing)
            {
                this.BeginInvoke(new Action(UpdateClientStats));
            }
        }

        private void SendMessageToAllClients(string message)
        {
            try
            {
                if (!isStreaming || udpServer == null)
                {
                    AppendLog("Không thể gửi tin nhắn: Server chưa khởi động");
                    return;
                }

                List<IPEndPoint> clientsCopy;
                lock (clientLock)
                {
                    if (connectedClients.Count == 0)
                    {
                        AppendLog("Không có client nào kết nối");
                        return;
                    }

                    clientsCopy = new List<IPEndPoint>(connectedClients);
                }

                // Định dạng tin nhắn theo yêu cầu: [CHAT][Server][Nội dung]
                string serverMessage = $"[CHAT][{ServerName}][{message}]";
                byte[] data = Encoding.UTF8.GetBytes(serverMessage);

                List<IPEndPoint> failedClients = new List<IPEndPoint>();
                foreach (var client in clientsCopy)
                {
                    try
                    {
                        udpServer.Send(data, data.Length, client);

                        lock (clientLock)
                        {
                            if (clientStats.TryGetValue(client, out ClientStats stats))
                            {
                                stats.BytesSent += data.Length;
                                stats.PacketsSent++;
                                stats.LastActivity = DateTime.Now;
                            }
                        }
                    }
                    catch
                    {
                        if (!failedClients.Contains(client))
                        {
                            failedClients.Add(client);
                        }
                    }
                }

                // Xử lý các client thất bại
                if (failedClients.Count > 0)
                {
                    RemoveFailedClients(failedClients);
                }
            }
            catch (Exception ex)
            {
                this.BeginInvoke(new Action(() => AppendLog($"Lỗi gửi tin nhắn: {ex.Message}")));
            }
        }

        // Thêm phương thức BroadcastChatMessage:
        private void BroadcastChatMessage(IPEndPoint sender, string senderName, string content)
        {
            try
            {
                List<IPEndPoint> clientsCopy;
                lock (clientLock)
                {
                    clientsCopy = new List<IPEndPoint>(connectedClients);
                }

                if (clientsCopy.Count > 0)
                {
                    // Định dạng tin nhắn để gửi đến client
                    string broadcastMessage = $"[CHAT][{senderName}][{content}]";

                    // Gửi đến tất cả client ngoại trừ người gửi
                    foreach (var client in clientsCopy.Where(c => !c.Equals(sender)))
                    {
                        try
                        {
                            if (udpServer != null && isStreaming)
                            {
                                byte[] data;

                                // Kiểm tra xem client có sử dụng mã hóa không
                                lock (encryptionLock)
                                {
                                    if (clientAESKeys.ContainsKey(client))
                                    {
                                        // Client sử dụng mã hóa
                                        var aesKey = clientAESKeys[client];

                                        try
                                        {
                                            // Mã hóa tin nhắn chat
                                            var encryptedMessage = EncryptionHelper.AESEncrypt(broadcastMessage, aesKey);
                                            var finalMessage = $"[ENCRYPTED_CHAT]{encryptedMessage}";
                                            data = Encoding.UTF8.GetBytes(finalMessage);

                                            LogEncryptionMessage($"💬 Encrypted chat sent to {client} - Sender: {senderName}");
                                        }
                                        catch (Exception encEx)
                                        {
                                            LogEncryptionMessage($"❌ Chat encryption failed for {client}: {encEx.Message}");
                                            // Fallback to unencrypted
                                            data = Encoding.UTF8.GetBytes(broadcastMessage);
                                        }
                                    }
                                    else
                                    {
                                        // Client không sử dụng mã hóa
                                        data = Encoding.UTF8.GetBytes(broadcastMessage);
                                    }
                                }

                                udpServer.Send(data, data.Length, client);

                                lock (clientLock)
                                {
                                    if (clientStats.TryGetValue(client, out ClientStats stats))
                                    {
                                        stats.BytesSent += data.Length;
                                        stats.PacketsSent++;
                                        stats.LastActivity = DateTime.Now;
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Lỗi sẽ được xử lý khi gửi frame tiếp theo
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.BeginInvoke(new Action(() =>
                {
                    AppendLog($"Lỗi broadcast chat: {ex.Message}");
                }));
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}