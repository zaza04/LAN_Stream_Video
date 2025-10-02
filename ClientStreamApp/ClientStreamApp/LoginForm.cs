using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClientStreamApp
{
    public partial class LoginForm : Form
    {
        // Properties
        public string Username { get; private set; } = "";
        public int UserId { get; private set; } = 0;
        public bool LoginSuccessful { get; private set; } = false;
        public string AuthToken { get; private set; } = "";

        // Encryption properties
        public string AESKey { get; private set; } = "";
        public bool EncryptionEnabled { get; private set; } = false;

        // Private fields
        private UdpClient udpClient;
        private string serverIP = "127.0.0.1";
        private int serverPort = 8888;

        // Encryption fields
        private string clientPrivateKey = "";
        private string clientPublicKey = "";

        // Constructor
        public LoginForm()
        {
            InitializeComponent();
            WireUpEventHandlers();
            InitializeEncryption();
        }

        // Initialize encryption components
        private void InitializeEncryption()
        {
            try
            {
                // Không subscribe to encryption logging để tránh spam
                // EncryptionHelper.OnLogMessage += LogEncryptionMessage;
                
                // Generate RSA key pair for this client session
                var keyPair = EncryptionHelper.GenerateRSAKeyPair();
                clientPublicKey = keyPair.PublicKey;
                clientPrivateKey = keyPair.PrivateKey;
                
                LogEncryptionMessage($"Client RSA keys generated");
            }
            catch (Exception ex)
            {
                LogEncryptionMessage($"Lỗi khởi tạo encryption: {ex.Message}");
                // Tiếp tục without encryption nếu có lỗi
                EncryptionEnabled = false;
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
            
            Console.WriteLine($"[LOGIN-ENCRYPTION] {message}");
            // Có thể thêm vào textbox hoặc log file nếu cần
        }

        // Event handlers wiring
        private void WireUpEventHandlers()
        {
            this.loginButton.Click += LoginButton_Click;
            this.cancelButton.Click += CancelButton_Click;
            this.passwordTextBox.KeyPress += PasswordTextBox_KeyPress;
            this.usernameTextBox.KeyPress += UsernameTextBox_KeyPress;

            // Set focus to username textbox when form loads
            this.Load += (s, e) => usernameTextBox.Focus();
        }

        // Keyboard event handlers
        private void UsernameTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                passwordTextBox.Focus();
                e.Handled = true;
            }
        }

        private void PasswordTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                LoginButton_Click(sender, e);
                e.Handled = true;
            }
        }

        // Button click handlers
        private async void LoginButton_Click(object sender, EventArgs e)
        {
            if (ValidateInput())
            {
                await PerformLogin();
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        // Input validation
        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(usernameTextBox.Text))
            {
                ShowStatus("Please enter username", Color.Red);
                usernameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(passwordTextBox.Text))
            {
                ShowStatus("Please enter password", Color.Red);
                passwordTextBox.Focus();
                return false;
            }

            return true;
        }

        // Main login logic
        private async Task PerformLogin()
        {
            try
            {
                // UI feedback
                SetLoginInProgress(true);
                ShowStatus("Connecting to server...", Color.Blue);

                // Create encrypted authentication request with public key
                var authRequest = AuthMessage.CreateEncryptedAuthRequest(
                    usernameTextBox.Text.Trim(),
                    passwordTextBox.Text,
                    clientPublicKey
                );

                ShowStatus("Preparing encrypted authentication...", Color.Blue);
                LogEncryptionMessage($"Tạo yêu cầu xác thực mã hóa - Username: {authRequest.Username}, Key exchange step: {authRequest.KeyExchangeStep}");

                string requestJson = JsonSerializer.Serialize(authRequest);
                byte[] requestData = Encoding.UTF8.GetBytes(requestJson);

                // Send UDP request to server
                udpClient = new UdpClient();
                IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);

                await udpClient.SendAsync(requestData, requestData.Length, serverEndPoint);
                ShowStatus("Encrypted request sent, waiting for response...", Color.Orange);
                LogEncryptionMessage($"Đã gửi yêu cầu mã hóa đến server - Data size: {requestData.Length} bytes");

                // Wait for response with timeout
                var timeoutTask = Task.Delay(5000); // 5 second timeout
                var receiveTask = udpClient.ReceiveAsync();

                var completedTask = await Task.WhenAny(receiveTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    ShowStatus("Connection timeout. Please check server.", Color.Red);
                    LogEncryptionMessage("Timeout - Không nhận được phản hồi từ server");
                    SetLoginInProgress(false);
                    return;
                }

                // Process server response
                var result = await receiveTask;
                string responseJson = Encoding.UTF8.GetString(result.Buffer);
                LogEncryptionMessage($"Nhận phản hồi từ server - Response size: {result.Buffer.Length} bytes");
                
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var authResponse = JsonSerializer.Deserialize<AuthMessage>(responseJson, options);

                if (authResponse != null)
                {
                    ProcessAuthResponse(authResponse);
                }
                else
                {
                    ShowStatus("Invalid server response", Color.Red);
                    LogEncryptionMessage("Lỗi: Không thể parse phản hồi từ server");
                    SetLoginInProgress(false);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Connection error", Color.Red);
                LogEncryptionMessage($"Lỗi kết nối: {ex.Message}");
                SetLoginInProgress(false);
            }
            finally
            {
                udpClient?.Close();
            }
        }

        // Process authentication response from server
        private void ProcessAuthResponse(AuthMessage authResponse)
        {
            LogEncryptionMessage($"Xử lý phản hồi từ server - Type: {authResponse.Type}, KeyExchangeStep: {authResponse.KeyExchangeStep}");

            if (authResponse.Type == "AUTH_SUCCESS")
            {
                // Check if response contains encrypted AES key
                if (!string.IsNullOrEmpty(authResponse.EncryptedAESKey))
                {
                    try
                    {
                        // Decrypt AES key using our private key
                        LogEncryptionMessage("Bắt đầu giải mã khóa AES từ server...");
                        AESKey = EncryptionHelper.RSADecrypt(authResponse.EncryptedAESKey, clientPrivateKey);
                        EncryptionEnabled = true;
                        
                        LogEncryptionMessage($"✅ Khóa AES đã được giải mã thành công - Length: {AESKey.Length}");
                        ShowStatus("🔐 Secure connection established!", Color.Green);
                    }
                    catch (Exception ex)
                    {
                        LogEncryptionMessage($"❌ Lỗi giải mã khóa AES: {ex.Message}");
                        ShowStatus("⚠️ Encryption setup failed, using unencrypted connection", Color.Orange);
                        EncryptionEnabled = false;
                    }
                }

                // Login successful
                Username = authResponse.Username;
                UserId = authResponse.UserId;
                AuthToken = authResponse.Token;
                LoginSuccessful = true;

                var encryptionStatus = EncryptionEnabled ? "🔐 Encrypted" : "🔓 Unencrypted";
                ShowStatus($"✅ Welcome {Username}! ({encryptionStatus})", Color.Green);
                LogEncryptionMessage($"Đăng nhập thành công - User: {Username}, Encryption: {EncryptionEnabled}");

                // Delay to show success message, then close form
                _ = DelayAndClose();
            }
            else if (authResponse.Type == "AUTH_FAILED")
            {
                ShowStatus($"❌ {authResponse.Message}", Color.Red);
                LogEncryptionMessage($"Đăng nhập thất bại: {authResponse.Message}");
                SetLoginInProgress(false);
            }
            else
            {
                ShowStatus("Unknown response from server", Color.Red);
                LogEncryptionMessage($"Phản hồi không xác định từ server: {authResponse.Type}");
                SetLoginInProgress(false);
            }
        }

        // Helper method to delay and close form
        private async Task DelayAndClose()
        {
            await Task.Delay(1000); // Show success message for 1 second
            if (!this.IsDisposed)
            {
                this.Invoke(new Action(() =>
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }));
            }
        }

        // UI update methods
        private void ShowStatus(string message, Color color)
        {
            if (statusLabel.InvokeRequired)
            {
                statusLabel.Invoke(new Action(() => ShowStatus(message, color)));
                return;
            }

            statusLabel.Text = message;
            statusLabel.ForeColor = color;
            statusLabel.Refresh();
        }

        private void SetLoginInProgress(bool inProgress)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetLoginInProgress(inProgress)));
                return;
            }

            loginButton.Enabled = !inProgress;
            usernameTextBox.Enabled = !inProgress;
            passwordTextBox.Enabled = !inProgress;
            cancelButton.Enabled = !inProgress;
            progressBar.Visible = inProgress;

            loginButton.Text = inProgress ? "Logging in..." : "Login";

            // Force UI update
            this.Refresh();
        }

        // Form closing handler
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Unsubscribe from encryption events
            EncryptionHelper.OnLogMessage -= LogEncryptionMessage;
            udpClient?.Close();
            base.OnFormClosing(e);
        }
    }

    // Auth message class for JSON serialization/deserialization
    public class AuthMessage
    {
        // Existing fields - Authentication
        public string Type { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Token { get; set; } = string.Empty;

        // New fields - Key Exchange for Encryption
        public string PublicKey { get; set; } = string.Empty;
        public string EncryptedAESKey { get; set; } = string.Empty;
        public string KeyExchangeStep { get; set; } = string.Empty;
        public bool IsEncrypted { get; set; } = false;

        // Helper method to create key exchange request
        public static AuthMessage CreateKeyExchangeRequest(string clientPublicKey)
        {
            return new AuthMessage
            {
                Type = "KEY_EXCHANGE_REQUEST",
                PublicKey = clientPublicKey,
                KeyExchangeStep = "CLIENT_PUBLIC_KEY",
                Message = "Client requesting secure key exchange"
            };
        }

        // Helper method to create encrypted auth request
        public static AuthMessage CreateEncryptedAuthRequest(string username, string password, string clientPublicKey)
        {
            return new AuthMessage
            {
                Type = "AUTH_REQUEST_ENCRYPTED",
                Username = username,
                Password = password,
                PublicKey = clientPublicKey,
                KeyExchangeStep = "AUTH_WITH_KEY_EXCHANGE",
                IsEncrypted = true,
                Message = "Authentication request with key exchange"
            };
        }
    }
}