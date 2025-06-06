using System.Windows.Forms;

namespace Client
{
    partial class Client
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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

        private static string FormHeader = "Alien Game Client";

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            ConnectButton = new Button();
            JoinLobbyButton = new Button();
            CardPicture1 = new PictureBox();
            CardPicture2 = new PictureBox();
            CardPicture3 = new PictureBox();
            CardPicture4 = new PictureBox();
            CardPicture5 = new PictureBox();
            ChatBox = new RichTextBox();
            ChatInputBox = new RichTextBox();
            GameLogTextBox = new RichTextBox();
            StatusLabel = new Label();
            OfferAcceptButton = new Button();
            OfferRejectButton = new Button();
            ConnectionStatusLabel = new Label();
            StartGameButton = new Button();
            ((System.ComponentModel.ISupportInitialize)CardPicture1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)CardPicture2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)CardPicture3).BeginInit();
            ((System.ComponentModel.ISupportInitialize)CardPicture4).BeginInit();
            ((System.ComponentModel.ISupportInitialize)CardPicture5).BeginInit();
            SuspendLayout();
            // 
            // ConnectButton
            // 
            ConnectButton.Enabled = false;
            ConnectButton.Location = new Point(33, 47);
            ConnectButton.Name = "ConnectButton";
            ConnectButton.Size = new Size(109, 25);
            ConnectButton.TabIndex = 0;
            ConnectButton.Text = "Connect";
            ConnectButton.UseVisualStyleBackColor = true;
            ConnectButton.Click += ConnectButton_Click;
            // 
            // JoinLobbyButton
            // 
            JoinLobbyButton.Enabled = false;
            JoinLobbyButton.Location = new Point(33, 78);
            JoinLobbyButton.Name = "JoinLobbyButton";
            JoinLobbyButton.Size = new Size(109, 23);
            JoinLobbyButton.TabIndex = 1;
            JoinLobbyButton.Text = "Join Lobby";
            JoinLobbyButton.UseVisualStyleBackColor = true;
            JoinLobbyButton.Click += JoinLobbyButton_Click;
            JoinLobbyButton.Visible = false;
            // 
            // CardPicture1
            // 
            CardPicture1.Location = new Point(616, 14);
            CardPicture1.Name = "CardPicture1";
            CardPicture1.Size = new Size(72, 119);
            CardPicture1.SizeMode = PictureBoxSizeMode.StretchImage;
            CardPicture1.TabIndex = 2;
            CardPicture1.TabStop = false;
            CardPicture1.Paint += CardPicture_Paint;
            CardPicture1.DoubleClick += CardPicture_DoubleClick;
            // 
            // CardPicture2
            // 
            CardPicture2.Location = new Point(694, 14);
            CardPicture2.Name = "CardPicture2";
            CardPicture2.Size = new Size(72, 119);
            CardPicture2.SizeMode = PictureBoxSizeMode.StretchImage;
            CardPicture2.TabIndex = 3;
            CardPicture2.TabStop = false;
            CardPicture2.Paint += CardPicture_Paint;
            CardPicture2.DoubleClick += CardPicture_DoubleClick;
            // 
            // CardPicture3
            // 
            CardPicture3.Location = new Point(772, 14);
            CardPicture3.Name = "CardPicture3";
            CardPicture3.Size = new Size(72, 119);
            CardPicture3.SizeMode = PictureBoxSizeMode.StretchImage;
            CardPicture3.TabIndex = 4;
            CardPicture3.TabStop = false;
            CardPicture3.Paint += CardPicture_Paint;
            CardPicture3.DoubleClick += CardPicture_DoubleClick;
            // 
            // CardPicture4
            // 
            CardPicture4.Location = new Point(850, 14);
            CardPicture4.Name = "CardPicture4";
            CardPicture4.Size = new Size(72, 119);
            CardPicture4.SizeMode = PictureBoxSizeMode.StretchImage;
            CardPicture4.TabIndex = 5;
            CardPicture4.TabStop = false;
            CardPicture4.Paint += CardPicture_Paint;
            CardPicture4.DoubleClick += CardPicture_DoubleClick;
            // 
            // CardPicture5
            // 
            CardPicture5.Location = new Point(928, 14);
            CardPicture5.Name = "CardPicture5";
            CardPicture5.Size = new Size(72, 119);
            CardPicture5.SizeMode = PictureBoxSizeMode.StretchImage;
            CardPicture5.TabIndex = 6;
            CardPicture5.TabStop = false;
            CardPicture5.Paint += CardPicture_Paint;
            CardPicture5.DoubleClick += CardPicture_DoubleClick;
            // 
            // ChatBox
            // 
            ChatBox.Location = new Point(21, 337);
            ChatBox.Name = "ChatBox";
            ChatBox.ReadOnly = true;
            ChatBox.Size = new Size(887, 138);
            ChatBox.TabIndex = 7;
            ChatBox.Text = "";
            // 
            // ChatInputBox
            // 
            ChatInputBox.Location = new Point(21, 480);
            ChatInputBox.Name = "ChatInputBox";
            ChatInputBox.Size = new Size(886, 44);
            ChatInputBox.TabIndex = 8;
            ChatInputBox.Text = "";
            ChatInputBox.KeyUp += ChatInputBox_KeyUp;
            // 
            // GameLogTextBox
            // 
            GameLogTextBox.Location = new Point(177, 14);
            GameLogTextBox.Name = "GameLogTextBox";
            GameLogTextBox.ReadOnly = true;
            GameLogTextBox.Size = new Size(413, 119);
            GameLogTextBox.TabIndex = 9;
            GameLogTextBox.Text = "";
            // 
            // StatusLabel
            // 
            StatusLabel.AutoSize = true;
            StatusLabel.Location = new Point(621, 148);
            StatusLabel.Name = "StatusLabel";
            StatusLabel.Size = new Size(0, 15);
            StatusLabel.TabIndex = 10;
            // 
            // OfferAcceptButton
            // 
            OfferAcceptButton.Location = new Point(668, 174);
            OfferAcceptButton.Name = "OfferAcceptButton";
            OfferAcceptButton.Size = new Size(72, 31);
            OfferAcceptButton.TabIndex = 11;
            OfferAcceptButton.Text = "Accept";
            OfferAcceptButton.UseVisualStyleBackColor = true;
            OfferAcceptButton.Visible = false;
            OfferAcceptButton.Click += OfferAcceptButton_Click;
            // 
            // OfferRejectButton
            // 
            OfferRejectButton.Location = new Point(746, 174);
            OfferRejectButton.Name = "OfferRejectButton";
            OfferRejectButton.Size = new Size(72, 31);
            OfferRejectButton.TabIndex = 12;
            OfferRejectButton.Text = "Reject";
            OfferRejectButton.UseVisualStyleBackColor = true;
            OfferRejectButton.Visible = false;
            OfferRejectButton.Click += OfferRejectButton_Click;
            // 
            // ConnectionStatusLabel
            // 
            ConnectionStatusLabel.AutoSize = true;
            ConnectionStatusLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            ConnectionStatusLabel.Location = new Point(32, 16);
            ConnectionStatusLabel.Name = "ConnectionStatusLabel";
            ConnectionStatusLabel.Size = new Size(114, 21);
            ConnectionStatusLabel.TabIndex = 13;
            ConnectionStatusLabel.Text = "Disconnected";
            // 
            // StartGameButton
            // 
            StartGameButton.Enabled = false;
            StartGameButton.Location = new Point(33, 107);
            StartGameButton.Name = "StartGameButton";
            StartGameButton.Size = new Size(109, 23);
            StartGameButton.TabIndex = 14;
            StartGameButton.Text = "Start Game";
            StartGameButton.UseVisualStyleBackColor = true;
            StartGameButton.Visible = false;
            StartGameButton.Click += StartGameButton_Click;
            // 
            // Client
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1033, 531);
            Controls.Add(StartGameButton);
            Controls.Add(ConnectionStatusLabel);
            Controls.Add(OfferRejectButton);
            Controls.Add(OfferAcceptButton);
            Controls.Add(StatusLabel);
            Controls.Add(GameLogTextBox);
            Controls.Add(ChatInputBox);
            Controls.Add(ChatBox);
            Controls.Add(CardPicture5);
            Controls.Add(CardPicture4);
            Controls.Add(CardPicture3);
            Controls.Add(CardPicture2);
            Controls.Add(CardPicture1);
            Controls.Add(JoinLobbyButton);
            Controls.Add(ConnectButton);
            Name = "Client";
            Text = FormHeader;
            Load += LoadAsync;
            ((System.ComponentModel.ISupportInitialize)CardPicture1).EndInit();
            ((System.ComponentModel.ISupportInitialize)CardPicture2).EndInit();
            ((System.ComponentModel.ISupportInitialize)CardPicture3).EndInit();
            ((System.ComponentModel.ISupportInitialize)CardPicture4).EndInit();
            ((System.ComponentModel.ISupportInitialize)CardPicture5).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button ConnectButton;
        private Button JoinLobbyButton;
        private PictureBox CardPicture1;
        private PictureBox CardPicture2;
        private PictureBox CardPicture3;
        private PictureBox CardPicture4;
        private PictureBox CardPicture5;
        private RichTextBox ChatBox;
        private RichTextBox ChatInputBox;
        private RichTextBox GameLogTextBox;
        private Label StatusLabel;
        private Button OfferAcceptButton;
        private Button OfferRejectButton;
        private Label ConnectionStatusLabel;
        private Button StartGameButton;
    }
}
