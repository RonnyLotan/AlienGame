namespace Client
{
    partial class JoinLobbyForm
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
            LobbyNameTextBox = new TextBox();
            EntryCodeTextBox = new TextBox();
            PasswordLabel = new Label();
            EnterButton = new Button();
            CreateNewButton = new Button();
            SuspendLayout();
            // 
            // UsernameLabel
            // 
            UsernameLabel.AutoSize = true;
            UsernameLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            UsernameLabel.Location = new Point(43, 21);
            UsernameLabel.Name = "UsernameLabel";
            UsernameLabel.Size = new Size(79, 15);
            UsernameLabel.TabIndex = 0;
            UsernameLabel.Text = "Lobby Name:";
            UsernameLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // LobbyNameTextBox
            // 
            LobbyNameTextBox.Location = new Point(124, 19);
            LobbyNameTextBox.Name = "LobbyNameTextBox";
            LobbyNameTextBox.Size = new Size(145, 23);
            LobbyNameTextBox.TabIndex = 1;
            // 
            // EntryCodeTextBox
            // 
            EntryCodeTextBox.Location = new Point(124, 48);
            EntryCodeTextBox.Name = "EntryCodeTextBox";
            EntryCodeTextBox.PasswordChar = '*';
            EntryCodeTextBox.Size = new Size(145, 23);
            EntryCodeTextBox.TabIndex = 2;
            // 
            // PasswordLabel
            // 
            PasswordLabel.AutoSize = true;
            PasswordLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            PasswordLabel.Location = new Point(54, 51);
            PasswordLabel.Name = "PasswordLabel";
            PasswordLabel.Size = new Size(70, 15);
            PasswordLabel.TabIndex = 3;
            PasswordLabel.Text = "Entry Code:";
            // 
            // EnterButton
            // 
            EnterButton.Location = new Point(144, 77);
            EnterButton.Name = "EnterButton";
            EnterButton.Size = new Size(94, 24);
            EnterButton.TabIndex = 4;
            EnterButton.Text = "Enter";
            EnterButton.UseVisualStyleBackColor = true;
            EnterButton.Click += EnterButton_Click;
            // 
            // CreateNewButton
            // 
            CreateNewButton.Location = new Point(144, 107);
            CreateNewButton.Name = "CreateNewButton";
            CreateNewButton.Size = new Size(94, 24);
            CreateNewButton.TabIndex = 5;
            CreateNewButton.Text = "Create New";
            CreateNewButton.UseVisualStyleBackColor = true;
            CreateNewButton.Click += CreateNewButton_Click;
            // 
            // JoinLobbyForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(347, 166);
            Controls.Add(CreateNewButton);
            Controls.Add(EnterButton);
            Controls.Add(PasswordLabel);
            Controls.Add(EntryCodeTextBox);
            Controls.Add(LobbyNameTextBox);
            Controls.Add(UsernameLabel);
            Name = "JoinLobbyForm";
            Text = "Login Form";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label UsernameLabel;
        private TextBox LobbyNameTextBox;
        private TextBox EntryCodeTextBox;
        private Label PasswordLabel;
        private Button EnterButton;
        private Button CreateNewButton;
    }
}