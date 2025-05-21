namespace Client
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
            UsernameLabel = new Label();
            UsernameTextBox = new TextBox();
            PasswordTextBox = new TextBox();
            PasswordLabel = new Label();
            ConnectButton = new Button();
            RegisterButton = new Button();
            SuspendLayout();
            // 
            // UsernameLabel
            // 
            UsernameLabel.AutoSize = true;
            UsernameLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            UsernameLabel.Location = new Point(54, 21);
            UsernameLabel.Name = "UsernameLabel";
            UsernameLabel.Size = new Size(67, 15);
            UsernameLabel.TabIndex = 0;
            UsernameLabel.Text = "Username:";
            UsernameLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // UsernameTextBox
            // 
            UsernameTextBox.Location = new Point(124, 19);
            UsernameTextBox.Name = "UsernameTextBox";
            UsernameTextBox.Size = new Size(145, 23);
            UsernameTextBox.TabIndex = 1;
            // 
            // PasswordTextBox
            // 
            PasswordTextBox.Location = new Point(124, 48);
            PasswordTextBox.Name = "PasswordTextBox";
            PasswordTextBox.Size = new Size(145, 23);
            PasswordTextBox.TabIndex = 2;
            PasswordTextBox.PasswordChar = '*';
            // 
            // PasswordLabel
            // 
            PasswordLabel.AutoSize = true;
            PasswordLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            PasswordLabel.Location = new Point(54, 51);
            PasswordLabel.Name = "PasswordLabel";
            PasswordLabel.Size = new Size(62, 15);
            PasswordLabel.TabIndex = 3;
            PasswordLabel.Text = "Password:";
            // 
            // ConnectButton
            // 
            ConnectButton.Location = new Point(144, 77);
            ConnectButton.Name = "ConnectButton";
            ConnectButton.Size = new Size(94, 24);
            ConnectButton.TabIndex = 4;
            ConnectButton.Text = "Connect";
            ConnectButton.UseVisualStyleBackColor = true;
            ConnectButton.Click += ConnectButton_Click;
            // 
            // RegisterButton
            // 
            RegisterButton.Location = new Point(144, 107);
            RegisterButton.Name = "RegisterButton";
            RegisterButton.Size = new Size(94, 24);
            RegisterButton.TabIndex = 5;
            RegisterButton.Text = "Register";
            RegisterButton.UseVisualStyleBackColor = true;
            RegisterButton.Click += RegisterButton_Click;
            // 
            // LoginForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(347, 166);
            Controls.Add(RegisterButton);
            Controls.Add(ConnectButton);
            Controls.Add(PasswordLabel);
            Controls.Add(PasswordTextBox);
            Controls.Add(UsernameTextBox);
            Controls.Add(UsernameLabel);
            Name = "LoginForm";
            Text = "Login Form";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label UsernameLabel;
        private TextBox UsernameTextBox;
        private TextBox PasswordTextBox;
        private Label PasswordLabel;
        private Button ConnectButton;
        private Button RegisterButton;
    }
}