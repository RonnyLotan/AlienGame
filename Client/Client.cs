using Shared;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace Client
{
    public partial class Client : Form
    {
        private CancellationTokenSource cts_;        
        internal UserData User { get; private set; }

        private Thread trd_;
        
        private List<Card> cards_;
        public required List<Card> Cards
        {
            get { return cards_; }
            set
            {
                cards_ = value;
                DisplayCards();
            }
        }

        // When acting as Giver
        public int? OfferedCardIndex { get; set; }
        public Card OfferedCard { get => Cards[OfferedCardIndex ?? 0]; }
        public List<int> RejectedCardIndices { get; set; }

        public String? ReceiverName;

        // When acting as receiver
        public int NumRejections = 0;
        public Card? ReceivedCard { get; set; }

        private List<PictureBox> cardPictures;

        public enum Mode
        {
            NotMyTurn,

            // Giver modes
            MakeOffer,
            WaitForReponse,

            // Receiver modes
            NeedToReply,
            Respond
        }

        private Mode playerMode_;
        public Mode PlayerMode
        {
            get { return playerMode_; }
            set
            {
                playerMode_ = value;
                switch (playerMode_)
                {
                    case Mode.NotMyTurn:
                        foreach (PictureBox pictureBox in cardPictures)
                        {
                            pictureBox.Enabled = false;
                        }

                        OfferAcceptButton.Enabled = false;
                        OfferRejectButton.Enabled = false;

                        OfferedCardIndex = null;
                        RejectedCardIndices.Clear();
                        ReceiverName = null;
                        NumRejections = 0;
                        ReceivedCard = null;

                        UpdateStatus("Not your turn");

                        break;

                    case Mode.MakeOffer:
                        foreach (PictureBox pictureBox in cardPictures)
                        {
                            pictureBox.Enabled = true;
                        }

                        foreach (int i in RejectedCardIndices)
                        {
                            cardPictures[i].Enabled = false;
                        }

                        OfferAcceptButton.Enabled = false;
                        OfferRejectButton.Enabled = false;

                        break;

                    case Mode.WaitForReponse:
                        break;

                    case Mode.Respond:
                        OfferAcceptButton.Enabled = true;
                        OfferRejectButton.Enabled = true;
                        break;
                }
            }
        }

        public Logger logger_ = new Logger();

        public Client()
        {
            InitializeComponent();

            cardPictures = new List<PictureBox>() { CardPicture1, CardPicture2, CardPicture3, CardPicture4, CardPicture5 };
        }

        private async void LoadAsync(object sender, EventArgs e)
        {
            async void init_client_and_connect()
            {
                try
                {
                    client = new TcpClient(Global.SERVER_IP, Global.SERVER_TCPPORT);
                    
                    isConnected = true;
                }
                catch (Exception ex)
                {
                    //await UI.type($"Failed to connect to server: {ex.Message}");
                    isConnected = false;
                }
            }

            init_client_and_connect();

            cts_ = new CancellationTokenSource();

            trd_ = new Thread(() => ClientLoop(cts_.Token));
            trd_.IsBackground = true;
            trd_.Start();            
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            _ = User.Logger.Log($"User clicked the {ConnectButton.Text} button");
            if (ConnectButton.Text == "Connect")
            {
                using (var loginForm = new LoginForm())
                {
                    if (loginForm.ShowDialog() == DialogResult.OK)
                    {
                        if (loginForm.IsRegisterClicked)
                        {
                            _ = User.Logger.Log($"User clicked the Register button");
                            using (var registerForm = new RegisterForm())
                            {
                                if (loginForm.ShowDialog() == DialogResult.OK)
                                {
                                    _ = User.Logger.Log($"User completed registration form");
                                    var username = registerForm.Username;
                                    var password = registerForm.Password;
                                    var email = registerForm.Email;

                                    var reply = RegisterRequestServerMessage.Create(username, password, email);
                                    User.Writer.WriteMessage(reply);
                                }
                            }
                        }
                        else
                        {
                            _ = User.Logger.Log($"User completed login form");
                            string username = loginForm.Username;
                            string password = loginForm.Password;

                            var reply = LoginRequestServerMessage.Create(username, password);
                            User.Writer.WriteMessage(reply);                            
                        }
                    }
                }
            }
            else
            {
                // handle disconnect
            }
        }

        private void JoinLobbyButton_Click(object sender, EventArgs e)
        {
            _ = User.Logger.Log($"User clicked the {JoinLobbyButton.Text} button");
            if (JoinLobbyButton.Text == "Join Lobby")
            {
                using (var joinLobbyForm = new JoinLobbyForm())
                {
                    if (joinLobbyForm.ShowDialog() == DialogResult.OK)
                    {
                        if (joinLobbyForm.IsCreateNewClicked)
                        {
                            _ = User.Logger.Log($"User clicked the Create New button");
                            using (var createNewForm = new CreateNewLobbyForm())
                            {
                                if (createNewForm.ShowDialog() == DialogResult.OK)
                                {
                                    _ = User.Logger.Log($"User completed registration form");
                                    var lobbyName = createNewForm.LobbyName;
                                    var entryCode = createNewForm.EntryCode;
                                   
                                    var reply = CreateNewLobbyRequestServerMessage.Create(lobbyName, entryCode);
                                    User.Writer.WriteMessage(reply);
                                }
                            }
                        }
                        else
                        {
                            _ = User.Logger.Log($"User completed login form");
                            var lobbyName = joinLobbyForm.LobbyName;
                            var entryCode = joinLobbyForm.EntryCode;

                            var reply = JoinLobbyRequestServerMessage.Create(lobbyName, entryCode);
                            User.Writer.WriteMessage(reply);
                        }
                    }
                }
            }
            else
            {
                // handle disconnect
            }
        }

        private bool ClientLoop(CancellationToken token)
        {
            // Add code here to search for server

            var client = new TcpClient(Global.SERVER_IP, Global.SERVER_TCPPORT);
            if (client is null || !client.Connected)
            {
                MessageBox.Show("Failed to establish connection!!! Quitting", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            UserData? user;
            if ((user = EstablishConnection(client!, token)) is null)
            {
                MessageBox.Show("Failed to establish connection!!! Quitting", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                client.Close();

                return false;
            }

            var handler = new MessageHandler(this, user);
            CommMessage? message;
            try
            {
                _ = User.Logger.Log($"Enter main loop");
                while (!token.IsCancellationRequested && user.Reader.ReadMessage(out message))
                    handler.Handle(message!);
            }
            catch (Exception ex)
            {
                _ = User.Logger.Log($"Exception thrown in main loop - {ex.Message}");
                MessageBox.Show($"Communication Error - {ex.Message}\nQuitting", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        private bool Login(UserData user, CancellationToken token)
        {
            _ = User.Logger.Log($"Prepare UI for login");
            GUI.invokeControl(ConnectButton, () => { ConnectButton.Enabled = true; });
            GUI.invokeControl(GameLogTextBox, () =>
            {
                GameLogTextBox.AppendText($"Please log in or register\n");
                GameLogTextBox.SelectionStart = GameLogTextBox.Text.Length;
                GameLogTextBox.ScrollToCaret();
            });

            // Wait for login to complete
            while (!token.IsCancellationRequested)
            {
                _ = User.Logger.Log($"Inside login/registration loop");
                CommMessage? message;
                while (!token.IsCancellationRequested && user.Reader.ReadMessage(out message))
                {
                    if (message!.Type == CommMessage.MessageType.Login && message is LoginResponseMessage loginResponseMsg)
                    {
                        _ = User.Logger.Log($"Receved response to login attempt - {loginResponseMsg.Text}");
                        if (loginResponseMsg.Success)
                        {
                            user.IsLoggedIn = true;
                            GUI.invokeControl(ConnectionStatusLabel, () =>
                            {
                                ConnectionStatusLabel.Text = "Connected!";
                                ConnectionStatusLabel.ForeColor = Color.Green;
                            });

                            GUI.invokeControl(ConnectButton, () =>
                            {
                                ConnectButton.Text = "Disconnect";
                            });                            

                            GUI.invokeControl(JoinLobbyButton, () =>
                            {
                                JoinLobbyButton.Enabled = true;
                            });

                            _ = User.Logger.Log($"Updated UI after successful login");

                            return true;                            
                        }
                        else
                        {
                            _ = User.Logger.Log($"Login failed. Notifying user");
                            MessageBox.Show($"Login Error - {loginResponseMsg.Reason}", "Login", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }

                    }
                    if (message!.Type == CommMessage.MessageType.Register && message is RegisterResponseMessage registerResponseMsg)
                    {
                        _ = User.Logger.Log($"Received response to login attempt - {registerResponseMsg.Text}");
                        if (registerResponseMsg.Success)
                        {
                            _ = User.Logger.Log($"Registration succeeded. Notifying user");
                            MessageBox.Show($"Registration succeeded - please log in", "Registration", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            _ = User.Logger.Log($"Registration failed. Notifying user");
                            MessageBox.Show($"Registration failed - {registerResponseMsg.Reason}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }

            return false;
        }

        private UserData? EstablishConnection(TcpClient client, CancellationToken token)
        {
            var nws = client.GetStream();
            var reader = new StreamReader(nws, Encoding.UTF8);
            var writer = new StreamWriter(nws, Encoding.UTF8) { AutoFlush = true };

            var (privateKey, publicKey) = Encryption.GenerateRsaKeyPair();

            // Send my public key
            writer.WriteLine(PublicKeyMessage.Create(publicKey, 0).Text);

            int? myId = null;
            string? serverPublicKey = null;
            string? aesKey = null;

            // Get public key and Id from server
            while (!token.IsCancellationRequested)
            {
                string? msg;
                if ((msg = reader.ReadLine()) != null)
                {
                    var parsedMessage = CommMessage.FromText(msg);

                    if (parsedMessage.Type == CommMessage.MessageType.PublicKey && parsedMessage is PublicKeyMessage publicKeyMsg)
                    {
                        myId = publicKeyMsg.Id;
                        serverPublicKey = publicKeyMsg.Key;

                        break;
                    }
                }
            }

            if (myId is not null && serverPublicKey is not null)
            {
                Logger logger = new Logger($"[[Client:{myId}]]");
                _ = logger.Log($"Got public key from server");

                // Get public key and Id from server
                while (!token.IsCancellationRequested)
                {
                    string? msg;
                    if ((msg = reader.ReadLine()) != null)
                    {
                        var parsedMessage = CommMessage.FromText(msg);

                        if (parsedMessage.Type == CommMessage.MessageType.AesKey && parsedMessage is AesKeyMessage aesKeyMsg)
                        {
                            aesKey = aesKeyMsg.Key;

                            break;
                        }
                    }
                }

                if (aesKey is not null)
                {
                    reader.Close();
                    writer.Close();
                    nws.Close();

                    return new UserData(myId.Value, client, aesKey, logger);
                }
            }

            return null;
        }

        public void DisplayCards()
        {
            void updatePicture(PictureBox pict, Image? image)
            {
                GUI.invokeControl(pict, () =>
                {
                    pict.Image = image;
                });                
            }

            _ = logger_.Log($"Updating the card display: {Cards}");

            int i = 0;
            foreach (Card c in Cards)
            {
                updatePicture(cardPictures[i++], c.Picture);
            }

            if (i < cardPictures.Count)
            {
                updatePicture(cardPictures[i], null);
            }
        }

        public void RemoveOfferedCard()
        {
            _ = logger_.Log($"Remove card {OfferedCard} from cards: {Cards}");
            Cards.Remove(OfferedCard);
            DisplayCards();
        }

        public void AddAcceptedCard(Card card)
        {
            _ = logger_.Log($"Add card {card} to cards: {Cards}");
            Cards.Append(card);
            DisplayCards();
        }

        public void AppendToChat(string text)
        {
            _ = logger_.Log($"Adding text {text} to ChatBox");
            GUI.AppendLine(text, ChatBox);
        }

        public void AppendToGameLog(string text)
        {
            _ = logger_.Log($"Adding text {text} to GameLog");
            GUI.AppendLine(text, ChatBox);
        }

        public void UpdateStatus(string text)
        {
            _ = logger_.Log($"Updating status lable text: {text}");
            GUI.Update(text, StatusLabel);
        }

        private void ChatInputBox_KeyDown(object sender, KeyEventArgs e)
        {
            bool isValid(string text)
            {
                return (!text.Contains('|') || text.Length <= 120);
            }

            if (e.KeyCode != Keys.Enter)
                return;            

            string userInput = ChatInputBox.Text;  // Get the text

            if (isValid(userInput))
            {
                _ = logger_.Log($"User done creating chat text [{userInput}]. Sending broadcast message");

                var response = BroadcastChatServerMessage.Generate(userInput).Text;
                writer.WriteLine(response);
            }

            ChatInputBox.Clear();
        }

        private void OfferAcceptButton_Click(object sender, EventArgs e)
        {
            logger_.Log($"User accepted the offer");

            var response = ResponseToOfferMessage.Generate(true).Text;
            writer.WriteLine(response);
        }

        private void OfferRejectButton_Click(object sender, EventArgs e)
        {
            logger_.Log($"User rejected the offer");
            var response = ResponseToOfferMessage.Generate(false).Text;
            writer.WriteLine(response);

            NumRejections++;
        }

        private void CardPicture_DoubleClick(object sender, EventArgs e)
        {
            // Clear previous selection (remove border by forcing repaint)
            if (OfferedCardIndex is not null)
            {
                cardPictures[OfferedCardIndex.Value].Invalidate();
            }

            // Set new selected
            var selectedPictureBox = sender as PictureBox;
            for (int i = 0; i < cardPictures.Count; i++)
            {
                if (cardPictures[i] == selectedPictureBox)
                {
                    OfferedCardIndex = i;
                    break;
                }                
            }

            _ = logger_.Log($"User selected the #{OfferedCardIndex} card. Sending message to server");

            if (selectedPictureBox is not null)
                selectedPictureBox.Invalidate(); // Force repaint to show selection

            PlayerMode = Mode.WaitForReponse;
            var response = OfferCardServerMessage.Generate(OfferedCard).Text;
            writer.WriteLine(response);
        }

        private void CardPicture_Paint(object sender, PaintEventArgs e)
        {
            PictureBox? pb = sender as PictureBox;
            if (pb is not null && OfferedCardIndex is not null)
            {
                if (pb == cardPictures[OfferedCardIndex.Value])
                {
                    using (Pen pen = new Pen(Color.Red, 3))
                    {
                        e.Graphics.DrawRectangle(pen, 0, 0, pb.Width - 1, pb.Height - 1);
                    }
                }
            }
        }
    }
}
