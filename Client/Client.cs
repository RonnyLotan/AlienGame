using Shared;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace Client
{
    public partial class Client : Form
    {
        private CancellationTokenSource cts_;
        
        private UserData? user_;
        internal UserData User { 
            get 
            {
                if (user_ == null)
                {
                    throw new InvalidOperationException("Accessing User before it was initialized");
                }

                return user_; 
            } 
            private set
            {
                user_ = value;
            }
        }

        private GameState? game_;
        internal GameState Game
        {
            get
            {
                if (game_ == null)
                {
                    throw new InvalidOperationException("Accessing Game before it was initialized");
                }

                return game_;
            }
            set
            {
                game_ = value;
            }
        }

        private List<PictureBox> cardPictures;

        public Client()
        {
            InitializeComponent();

            cts_ = new CancellationTokenSource();
            cardPictures = new List<PictureBox>() { CardPicture1, CardPicture2, CardPicture3, CardPicture4, CardPicture5 };
        }

        public void log(string text)
        {
            _ = User.Logger.Log(text);  
        }

        private void LoadAsync(object sender, EventArgs e)
        {
            var trd = new Thread(() => ClientLoop(cts_.Token));
            trd.IsBackground = true;
            trd.Start();            
        }

        public void ActivateNotYourTurnMode()
        {
            foreach (PictureBox pictureBox in cardPictures)
            {
                GUI.InvokeControl(pictureBox, () =>
                {
                    pictureBox.Enabled = false;
                });
            }

            GUI.InvokeControl(OfferAcceptButton, () =>
            {
                OfferAcceptButton.Enabled = false;
            });

            GUI.InvokeControl(OfferRejectButton, () =>
            {
                OfferRejectButton.Enabled = false;
            });

            UpdateStatus("Not your turn!");
        }

        public void ActivateMakeOfferMode(List<int> rejectedCardIndices)
        {
            foreach (PictureBox pictureBox in cardPictures)
            {
                GUI.InvokeControl(pictureBox, () =>
                {
                    pictureBox.Enabled = true;
                });

                UpdateStatus("Make an offer");
            }

            foreach (int i in rejectedCardIndices)
            {
                GUI.InvokeControl(cardPictures[i], () =>
                {
                    cardPictures[i].Enabled = false;
                });                
            }

            GUI.InvokeControl(OfferAcceptButton, () =>
            {
                OfferAcceptButton.Enabled = false;
            });

            GUI.InvokeControl(OfferRejectButton, () =>
            {
                OfferRejectButton.Enabled = false;
            });
        }

        public void RespondToOffer()
        {
            GUI.InvokeControl(OfferAcceptButton, () =>
            {
                OfferAcceptButton.Enabled = true;
            });

            GUI.InvokeControl(OfferRejectButton, () =>
            {
                OfferRejectButton.Enabled = true;
            });

            UpdateStatus("Respond to offer");
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            log($"User clicked the {ConnectButton.Text} button");
            if (ConnectButton.Text == "Connect")
            {
                using (var loginForm = new LoginForm())
                {
                    if (loginForm.ShowDialog() == DialogResult.OK)
                    {
                        if (loginForm.IsRegisterClicked)
                        {
                            log($"User clicked the Register button");
                            using (var registerForm = new RegisterForm())
                            {
                                if (loginForm.ShowDialog() == DialogResult.OK)
                                {
                                    log($"User completed registration form");
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
                            log($"User completed login form");
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
            log($"User clicked the {JoinLobbyButton.Text} button");
            if (JoinLobbyButton.Text == "Join Lobby")
            {
                using (var joinLobbyForm = new JoinLobbyForm())
                {
                    if (joinLobbyForm.ShowDialog() == DialogResult.OK)
                    {
                        if (joinLobbyForm.IsCreateNewClicked)
                        {
                            log($"User clicked the Create New button");
                            using (var createNewForm = new CreateNewLobbyForm())
                            {
                                if (createNewForm.ShowDialog() == DialogResult.OK)
                                {
                                    log($"User completed registration form");
                                    var lobbyName = createNewForm.LobbyName;
                                    var entryCode = createNewForm.EntryCode;
                                   
                                    var reply = CreateLobbyRequestServerMessage.Create(lobbyName, entryCode);
                                    User.Writer.WriteMessage(reply);
                                }
                            }
                        }
                        else
                        {
                            log($"User completed login form");
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
                // handle exit lobby
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

            if (Login(user, token))
            {
                MessageBox.Show("Failed to log in!!! Quitting", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                client.Close();

                return false;
            }

            if (EnterLobby(user, token))
            {
                MessageBox.Show("Failed to enter lobby!!! Quitting", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                client.Close();

                return false;
            }

            var handler = new MessageHandler(this, user);
            CommMessage? message;
            try
            {
                log($"Enter main loop");
                while (!token.IsCancellationRequested && user.Reader.ReadMessage(out message))
                    handler.Handle(message!);
            }
            catch (Exception ex)
            {
                log($"Exception thrown in main loop - {ex.Message}");
                MessageBox.Show($"Communication Error - {ex.Message}\nQuitting", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        private bool Login(UserData user, CancellationToken token)
        {
            log($"Prepare UI for login");

            // 
            GUI.InvokeControl(ConnectButton, () => { ConnectButton.Enabled = true; });
            GUI.AppendLine($"Please log in or register\n", GameLogTextBox);                

            // Wait for login to complete
            while (!token.IsCancellationRequested)
            {
                log($"Inside login/registration loop");
                CommMessage? message;
                while (!token.IsCancellationRequested && user.Reader.ReadMessage(out message))
                {
                    if (message!.Type == CommMessage.MessageType.Login && message is LoginResponseMessage loginResponseMsg)
                    {
                        log($"Recieved response to login attempt - {loginResponseMsg.Text}");
                        if (loginResponseMsg.Success)
                        {
                            user.IsLoggedIn = true;
                            GUI.InvokeControl(ConnectionStatusLabel, () =>
                            {
                                ConnectionStatusLabel.Text = "Connected!";
                                ConnectionStatusLabel.ForeColor = Color.Green;
                            });

                            GUI.InvokeControl(ConnectButton, () =>
                            {
                                ConnectButton.Text = "Disconnect";
                            });                            

                            GUI.InvokeControl(JoinLobbyButton, () =>
                            {
                                JoinLobbyButton.Enabled = true;
                            });

                            log($"Updated UI after successful login");

                            return true;                            
                        }
                        else
                        {
                            log($"Login failed. Notifying user");
                            MessageBox.Show($"Login Error - {loginResponseMsg.Reason}", "Login", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }

                    }
                    if (message!.Type == CommMessage.MessageType.Register && message is RegisterResponseMessage registerResponseMsg)
                    {
                        log($"Received response to login attempt - {registerResponseMsg.Text}");
                        if (registerResponseMsg.Success)
                        {
                            log($"Registration succeeded. Notifying user");
                            MessageBox.Show($"Registration succeeded - please log in", "Registration", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            log($"Registration failed. Notifying user");
                            MessageBox.Show($"Registration failed - {registerResponseMsg.Reason}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }

            return false;
        }

        private bool EnterLobby(UserData user, CancellationToken token)
        {
            log($"Prepare UI for enterring loby");
            GUI.InvokeControl(JoinLobbyButton, () => { JoinLobbyButton.Enabled = true; });
            GUI.InvokeControl(GameLogTextBox, () =>
            {
                GameLogTextBox.AppendText($"Please enter existing lobby or create new one\n");
                GameLogTextBox.SelectionStart = GameLogTextBox.Text.Length;
                GameLogTextBox.ScrollToCaret();
            });

            // Wait for login to complete
            while (!token.IsCancellationRequested)
            {
                log($"Inside join lobby/create new lobby loop");
                CommMessage? message;
                while (!token.IsCancellationRequested && user.Reader.ReadMessage(out message))
                {
                    if (message!.Type == CommMessage.MessageType.JoinLobby && message is JoinLobbyResponseMessage joinResponseMsg)
                    {
                        log($"Receved response to join lobby attempt - {joinResponseMsg.Text}");
                        if (joinResponseMsg.Success)
                        {
                            user.IsInLobby = true;
                            GUI.InvokeControl(ConnectionStatusLabel, () =>
                            {
                                ConnectionStatusLabel.Text = "In Lobby!";
                                ConnectionStatusLabel.ForeColor = Color.Purple;
                            });

                            GUI.InvokeControl(ConnectButton, () =>
                            {
                                JoinLobbyButton.Text = "Exit Lobby";
                            });                            

                            log($"Updated UI after successful lobby entrance");

                            return true;
                        }
                        else
                        {
                            log($"Join lobby failed. Notifying user");
                            MessageBox.Show($"Join Lobby Error - {joinResponseMsg.Reason}", "Join Lobby", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }

                    }
                    if (message!.Type == CommMessage.MessageType.CreateLobby && message is CreateLobbyResponseMessage createResponseMsg)
                    {
                        log($"Received response to create lobby attempt - {createResponseMsg.Text}");
                        if (createResponseMsg.Success)
                        {
                            log($"Lobby creation succeeded. Notifying user");
                            MessageBox.Show($"Lobby creation succeeded - please log in", "Create Lobby", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            log($"Lobby creation failed. Notifying user");
                            MessageBox.Show($"Lobby creation failed - {createResponseMsg.Reason}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        public void DisplayCards(List<Card> cards)
        {
            void updatePicture(PictureBox pict, Image? image)
            {
                GUI.InvokeControl(pict, () =>
                {
                    pict.Image = image;
                });                
            }

            log($"Updating the card display: {cards}");

            int i = 0;
            foreach (Card c in cards)
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
            Game.removeCard();
            DisplayCards(Game.Cards);
        }

        public void AddAcceptedCard(Card card)
        {
            Game.AppendCard(card);
            DisplayCards(Game.Cards);
        }

        public void AppendToChat(string text)
        {
            log($"Adding text {text} to ChatBox");
            GUI.AppendLine(text, ChatBox);
        }

        public void AppendToGameLog(string text)
        {
            log($"Adding text {text} to GameLog");
            GUI.AppendLine(text, ChatBox);
        }

        public void UpdateStatus(string text)
        {
            log($"Updating status lable text: {text}");
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
                log($"User done creating chat text [{userInput}]. Sending broadcast message");

                var response = BroadcastChatServerMessage.Create(userInput);
                User.Writer.WriteMessage(response);
            }

            ChatInputBox.Clear();
        }

        private void OfferAcceptButton_Click(object sender, EventArgs e)
        {
            log($"User accepted the offer");

            var response = ResponseToOfferMessage.Create(true);
            User.Writer.WriteMessage(response);
        }

        private void OfferRejectButton_Click(object sender, EventArgs e)
        {
            log($"User rejected the offer");
            var response = ResponseToOfferMessage.Create(false);
            User.Writer.WriteMessage(response);

            Game.NumRejections++;
        }

        private void CardPicture_DoubleClick(object sender, EventArgs e)
        {
            // Clear previous selection (remove border by forcing repaint)
            if (Game.OfferedCardIndex is not null)
            {
                cardPictures[Game.OfferedCardIndex.Value].Invalidate();
            }

            // Set new selected
            var selectedPictureBox = sender as PictureBox;
            for (int i = 0; i < cardPictures.Count; i++)
            {
                if (cardPictures[i] == selectedPictureBox)
                {
                    Game.OfferedCardIndex = i;
                    break;
                }                
            }

            log($"User selected the #{Game.OfferedCardIndex} card. Sending message to server");

            if (selectedPictureBox is not null)
                selectedPictureBox.Invalidate(); // Force repaint to show selection

            Game.PlayerMode = GameState.Mode.WaitForReponse;            
            var response = OfferCardServerMessage.Create(Game.OfferedCard);
            User.Writer.WriteMessage(response);
        }

        private void CardPicture_Paint(object sender, PaintEventArgs e)
        {
            PictureBox? pb = sender as PictureBox;
            if (pb is not null && Game.OfferedCardIndex is not null)
            {
                if (pb == cardPictures[Game.OfferedCardIndex.Value])
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
