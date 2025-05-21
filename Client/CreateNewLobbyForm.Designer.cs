namespace Client
{
    partial class CreateNewLobbyForm
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
            LobbyNameLabel = new Label();
            CreateButton = new Button();
            LobbyNameTextBox = new TextBox();
            EntryCodeLabel = new Label();
            EntryCodeTextBox = new TextBox();
            SuspendLayout();
            // 
            // LobbyNameLabel
            // 
            LobbyNameLabel.AutoSize = true;
            LobbyNameLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            LobbyNameLabel.Location = new Point(17, 15);
            LobbyNameLabel.Name = "LobbyNameLabel";
            LobbyNameLabel.Size = new Size(79, 15);
            LobbyNameLabel.TabIndex = 6;
            LobbyNameLabel.Text = "Lobby Name:";
            LobbyNameLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // CreateButton
            // 
            CreateButton.Location = new Point(113, 87);
            CreateButton.Name = "CreateButton";
            CreateButton.Size = new Size(94, 24);
            CreateButton.TabIndex = 11;
            CreateButton.Text = "Create";
            CreateButton.UseVisualStyleBackColor = true;
            CreateButton.Click += CreateButton_Click;
            // 
            // LobbyNameTextBox
            // 
            LobbyNameTextBox.Location = new Point(96, 12);
            LobbyNameTextBox.Name = "LobbyNameTextBox";
            LobbyNameTextBox.Size = new Size(145, 23);
            LobbyNameTextBox.TabIndex = 7;
            // 
            // EntryCodeLabel
            // 
            EntryCodeLabel.AutoSize = true;
            EntryCodeLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            EntryCodeLabel.Location = new Point(26, 45);
            EntryCodeLabel.Name = "EntryCodeLabel";
            EntryCodeLabel.Size = new Size(70, 15);
            EntryCodeLabel.TabIndex = 13;
            EntryCodeLabel.Text = "Entry Code:";
            // 
            // EntryCodeTextBox
            // 
            EntryCodeTextBox.Location = new Point(96, 42);
            EntryCodeTextBox.Name = "EntryCodeTextBox";
            EntryCodeTextBox.PasswordChar = '*';
            EntryCodeTextBox.Size = new Size(145, 23);
            EntryCodeTextBox.TabIndex = 12;
            // 
            // CreateNewLobbyForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(266, 121);
            Controls.Add(EntryCodeLabel);
            Controls.Add(EntryCodeTextBox);
            Controls.Add(CreateButton);
            Controls.Add(LobbyNameTextBox);
            Controls.Add(LobbyNameLabel);
            Name = "CreateNewLobbyForm";
            Text = "Create New Lobby Form";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label LobbyNameLabel;
        private Button CreateButton;
        private TextBox LobbyNameTextBox;
        private Label EntryCodeLabel;
        private TextBox EntryCodeTextBox;
    }
}