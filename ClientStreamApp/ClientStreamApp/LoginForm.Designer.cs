namespace ClientStreamApp
{
    partial class LoginForm
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
            titleLabel = new Label();
            usernameLabel = new Label();
            usernameTextBox = new TextBox();
            passwordLabel = new Label();
            passwordTextBox = new TextBox();
            statusLabel = new Label();
            progressBar = new ProgressBar();
            loginButton = new Button();
            cancelButton = new Button();
            errorLabel = new Label();
            SuspendLayout();
            // 
            // titleLabel
            // 
            titleLabel.AutoSize = true;
            titleLabel.Font = new Font("Segoe UI Black", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            titleLabel.ForeColor = Color.DarkBlue;
            titleLabel.Location = new Point(166, 18);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new Size(113, 41);
            titleLabel.TabIndex = 0;
            titleLabel.Text = "LOGIN";
            titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // usernameLabel
            // 
            usernameLabel.AutoSize = true;
            usernameLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            usernameLabel.Location = new Point(50, 88);
            usernameLabel.Name = "usernameLabel";
            usernameLabel.Size = new Size(82, 20);
            usernameLabel.TabIndex = 1;
            usernameLabel.Text = "Username:";
            // 
            // usernameTextBox
            // 
            usernameTextBox.Location = new Point(140, 84);
            usernameTextBox.Margin = new Padding(3, 4, 3, 4);
            usernameTextBox.Name = "usernameTextBox";
            usernameTextBox.Size = new Size(235, 27);
            usernameTextBox.TabIndex = 2;
            // 
            // passwordLabel
            // 
            passwordLabel.AutoSize = true;
            passwordLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            passwordLabel.Location = new Point(50, 138);
            passwordLabel.Name = "passwordLabel";
            passwordLabel.Size = new Size(77, 20);
            passwordLabel.TabIndex = 3;
            passwordLabel.Text = "Password:";
            // 
            // passwordTextBox
            // 
            passwordTextBox.Location = new Point(140, 134);
            passwordTextBox.Margin = new Padding(3, 4, 3, 4);
            passwordTextBox.Name = "passwordTextBox";
            passwordTextBox.PasswordChar = '*';
            passwordTextBox.Size = new Size(235, 27);
            passwordTextBox.TabIndex = 4;
            // 
            // statusLabel
            // 
            statusLabel.AutoSize = true;
            statusLabel.ForeColor = Color.Gray;
            statusLabel.Location = new Point(50, 188);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(152, 20);
            statusLabel.TabIndex = 5;
            statusLabel.Text = "Enter your credentials";
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // progressBar
            // 
            progressBar.Location = new Point(51, 223);
            progressBar.Margin = new Padding(3, 4, 3, 4);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(324, 10);
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.TabIndex = 6;
            progressBar.Visible = false;
            // 
            // loginButton
            // 
            loginButton.BackColor = Color.DodgerBlue;
            loginButton.FlatStyle = FlatStyle.Flat;
            loginButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            loginButton.ForeColor = Color.White;
            loginButton.Location = new Point(190, 262);
            loginButton.Margin = new Padding(3, 4, 3, 4);
            loginButton.Name = "loginButton";
            loginButton.Size = new Size(89, 32);
            loginButton.TabIndex = 7;
            loginButton.Text = "Login";
            loginButton.UseVisualStyleBackColor = false;
            // 
            // cancelButton
            // 
            cancelButton.BackColor = Color.Gray;
            cancelButton.FlatStyle = FlatStyle.Flat;
            cancelButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            cancelButton.ForeColor = Color.White;
            cancelButton.Location = new Point(285, 262);
            cancelButton.Margin = new Padding(3, 4, 3, 4);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(91, 32);
            cancelButton.TabIndex = 8;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = false;
            cancelButton.Click += CancelButton_Click;
            // 
            // errorLabel
            // 
            errorLabel.AutoSize = false;
            errorLabel.MaximumSize = new Size(350, 0);
            errorLabel.Size = new Size(350, 40);
            errorLabel.TextAlign = ContentAlignment.MiddleLeft;
            errorLabel.ForeColor = Color.Red;
            // 
            // LoginForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(460, 317);
            Controls.Add(cancelButton);
            Controls.Add(loginButton);
            Controls.Add(progressBar);
            Controls.Add(statusLabel);
            Controls.Add(passwordTextBox);
            Controls.Add(passwordLabel);
            Controls.Add(usernameTextBox);
            Controls.Add(usernameLabel);
            Controls.Add(titleLabel);
            Controls.Add(errorLabel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(3, 4, 3, 4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "LoginForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Stream Video Login";
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label usernameLabel;
        private System.Windows.Forms.TextBox usernameTextBox;
        private System.Windows.Forms.Label passwordLabel;
        private System.Windows.Forms.TextBox passwordTextBox;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Button loginButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Label errorLabel;
    }
}