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

        // Private fields
        private UdpClient udpClient;
        private string serverIP = "127.0.0.1";
        private int serverPort = 8888;

        // Constructor
        public LoginForm()
        {
            InitializeComponent();
            WireUpEventHandlers();
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

                // Create authentication request
                var authRequest = new AuthMessage
                {
                    Type = "AUTH_REQUEST",
                    Username = usernameTextBox.Text.Trim(),
                    Password = passwordTextBox.Text
                };

                string requestJson = JsonSerializer.Serialize(authRequest);
                byte[] requestData = Encoding.UTF8.GetBytes(requestJson);

                // Send UDP request to server
                udpClient = new UdpClient();
                IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);

                await udpClient.SendAsync(requestData, requestData.Length, serverEndPoint);
                ShowStatus("Request sent, waiting for response...", Color.Orange);

                // Wait for response with timeout
                var timeoutTask = Task.Delay(5000); // 5 second timeout
                var receiveTask = udpClient.ReceiveAsync();

                var completedTask = await Task.WhenAny(receiveTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    ShowStatus("Connection timeout. Please check server.", Color.Red);
                    SetLoginInProgress(false);
                    return;
                }

                // Process server response
                var result = await receiveTask;
                string responseJson = Encoding.UTF8.GetString(result.Buffer);
                var authResponse = JsonSerializer.Deserialize<AuthMessage>(responseJson);

                if (authResponse != null)
                {
                    ProcessAuthResponse(authResponse);
                }
                else
                {
                    ShowStatus("Invalid server response", Color.Red);
                    SetLoginInProgress(false);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Connection error", Color.Red);
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
            if (authResponse.Type == "AUTH_SUCCESS")
            {
                // Login successful
                Username = authResponse.Username;
                UserId = authResponse.UserId;
                LoginSuccessful = true;

                ShowStatus($"✅ Welcome {Username}!", Color.Green);

                // Delay to show success message, then close form
                _ = DelayAndClose();
            }
            else if (authResponse.Type == "AUTH_FAILED")
            {
                ShowStatus($"❌ {authResponse.Message}", Color.Red);
                SetLoginInProgress(false);
            }
            else
            {
                ShowStatus("Unknown response from server", Color.Red);
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
            udpClient?.Close();
            base.OnFormClosing(e);
        }
    }

    // Auth message class for JSON serialization/deserialization
    public class AuthMessage
    {
        public string Type { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int UserId { get; set; }
    }
}