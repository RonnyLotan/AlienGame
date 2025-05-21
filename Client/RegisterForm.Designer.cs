namespace Client
{
    partial class RegisterForm
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
            RegisterButton = new Button();
            EmailTextBox = new TextBox();
            EmailLabel = new Label();
            UsernameTextBox = new TextBox();
            PasswordLabel = new Label();
            PasswordTextBox = new TextBox();
            SuspendLayout();
            // 
            // UsernameLabel
            // 
            UsernameLabel.AutoSize = true;
            UsernameLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            UsernameLabel.Location = new Point(21, 15);
            UsernameLabel.Name = "UsernameLabel";
            UsernameLabel.Size = new Size(67, 15);
            UsernameLabel.TabIndex = 6;
            UsernameLabel.Text = "Username:";
            UsernameLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // RegisterButton
            // 
            RegisterButton.Location = new Point(113, 115);
            RegisterButton.Name = "RegisterButton";
            RegisterButton.Size = new Size(94, 24);
            RegisterButton.TabIndex = 11;
            RegisterButton.Text = "Register";
            RegisterButton.UseVisualStyleBackColor = true;
            RegisterButton.Click += RegisterButton_Click;
            // 
            // EmailTextBox
            // 
            EmailTextBox.Location = new Point(96, 41);
            EmailTextBox.Name = "EmailTextBox";
            EmailTextBox.Size = new Size(145, 23);
            EmailTextBox.TabIndex = 8;
            // 
            // EmailLabel
            // 
            EmailLabel.AutoSize = true;
            EmailLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            EmailLabel.Location = new Point(2, 44);
            EmailLabel.Name = "EmailLabel";
            EmailLabel.Size = new Size(86, 15);
            EmailLabel.TabIndex = 9;
            EmailLabel.Text = "Email Address:";
            // 
            // UsernameTextBox
            // 
            UsernameTextBox.Location = new Point(96, 12);
            UsernameTextBox.Name = "UsernameTextBox";
            UsernameTextBox.Size = new Size(145, 23);
            UsernameTextBox.TabIndex = 7;
            // 
            // PasswordLabel
            // 
            PasswordLabel.AutoSize = true;
            PasswordLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            PasswordLabel.Location = new Point(26, 73);
            PasswordLabel.Name = "PasswordLabel";
            PasswordLabel.Size = new Size(62, 15);
            PasswordLabel.TabIndex = 13;
            PasswordLabel.Text = "Password:";
            // 
            // PasswordTextBox
            // 
            PasswordTextBox.Location = new Point(96, 70);
            PasswordTextBox.Name = "PasswordTextBox";
            PasswordTextBox.Size = new Size(145, 23);
            PasswordTextBox.TabIndex = 12;
            PasswordTextBox.PasswordChar = '*';
            // 
            // RegisterForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(254, 164);
            Controls.Add(PasswordLabel);
            Controls.Add(PasswordTextBox);
            Controls.Add(RegisterButton);
            Controls.Add(EmailLabel);
            Controls.Add(EmailTextBox);
            Controls.Add(UsernameTextBox);
            Controls.Add(UsernameLabel);
            Name = "RegisterForm";
            Text = "Register Form";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label UsernameLabel;
        private Button RegisterButton;
        private TextBox EmailTextBox;
        private Label EmailLabel;
        private TextBox UsernameTextBox;
        private Label PasswordLabel;
        private TextBox PasswordTextBox;
    }
}