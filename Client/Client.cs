using Shared;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace Client
{
    public partial class Client : Form
    {
        private CancellationTokenSource cts_;

        private UserData? user_;
        internal UserData User
        {
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

        private List<PictureBox> cardPictures_;

        Logger logger_;

        public Client()
        {
            InitializeComponent();

            cts_ = new CancellationTokenSource();
            cardPictures_ = new List<PictureBox>() { CardPicture1, CardPicture2, CardPicture3, CardPicture4, CardPicture5 };

            logger_ = new Logger($"Unknown Client");
        }

        public void log(string text)
        {
            _ = logger_.Log(text);
        }

        private void LoadAsync(object sender, EventArgs e)
        {
            var trd = new Thread(() =>
            {
                if (!ClientLoop(cts_.Token))
                    ShutDown();
            });

            trd.IsBackground = true;
            trd.Start();
        }

        public void StartGame(List<Card> cardList)
        {
            log($"Start game. Received these cards: {string.Join(',', cardList)}");

            UpdateConnectionStatusInGame();
            game_ = new GameState(this, User.Logger, cardList);
            DisplayCards(Game.Cards);

            // Prepare the end Game button
            GUI.ActionComponent(StartGameButton, () =>
            {
                StartGameButton.Text = "End Game";
                StartGameButton.Visible = true;
                StartGameButton.Enabled = true;
                StartGameButton.Focus();
            });

            GUI.ActionComponent(JoinLobbyButton, () =>
            {
                JoinLobbyButton.Enabled = false;
            });

            AppendToGameLog("Game is starting");
        }

        public void EndGame(string winner, string loser)
        {
            AppendToGameLog($"Game over!!!");
            AppendToGameLog($"The winner is {winner}");
            AppendToGameLog($"The loser is {loser}");

            ResetForNewGame();
        }

        public void InterruptGame(string user)
        {
            AppendToGameLog($"Game interrupted by {user}!!!");

            ResetForNewGame();
        }

        public void ExitLobby()
        {
            User.IsHost = false;
            User.InLobby = false;

            UpdateConnectionStatusConnected();

            GUI.ActionComponent(ConnectButton, () =>
            {
                ConnectButton.Enabled = true;
            });

            GUI.ActionComponent(StartGameButton, () =>
            {
                StartGameButton.Visible = false;
            });

            PrepareForJoinLobby();

            log($"Updated UI after lobby exit");
        }

        private void ResetForNewGame()
        {
            game_ = null;

            ClearCardDisplay();

            AppendToGameLog($"---------------------------------------");

            UpdateConnectionStatusInLobby();

            // Prepare the Start Game button
            GUI.ActionComponent(StartGameButton, () =>
            {
                if (User.IsHost)
                {
                    StartGameButton.Text = "Start Game";
                    StartGameButton.Visible = true;
                }
                else
                    StartGameButton.Visible = false;
            });

            GUI.ActionComponent(JoinLobbyButton, () =>
            {
                JoinLobbyButton.Enabled = true;
            });

            ActivateNotYourTurnMode();
            UpdateStatus("");
        }

        public void ActivateNotYourTurnMode()
        {
            foreach (PictureBox pictureBox in cardPictures_)
            {
                GUI.ActionComponent(pictureBox, () =>
                {
                    pictureBox.Enabled = false;
                    pictureBox.Invalidate();
                });
            }

            GUI.ActionComponent(OfferAcceptButton, () =>
            {
                OfferAcceptButton.Visible = false;
                OfferAcceptButton.Enabled = false;
            });

            GUI.ActionComponent(OfferRejectButton, () =>
            {
                OfferRejectButton.Visible = false;
                OfferRejectButton.Enabled = false;
            });

            UpdateStatus("Not your turn!");
        }

        public void ActivateMakeOfferMode(List<int> rejectedCardIndices)
        {
            foreach (PictureBox pictureBox in cardPictures_)
            {
                GUI.ActionComponent(pictureBox, () =>
                {
                    pictureBox.Enabled = true;
                    pictureBox.Invalidate();
                });
            }

            foreach (int i in rejectedCardIndices)
            {
                GUI.ActionComponent(cardPictures_[i], () =>
                {
                    cardPictures_[i].Enabled = false;
                    cardPictures_[i].Invalidate();
                });
            }

            UpdateStatus("Make an offer");

            GUI.ActionComponent(OfferAcceptButton, () =>
            {
                OfferAcceptButton.Visible = false;
                OfferAcceptButton.Enabled = false;
            });

            GUI.ActionComponent(OfferRejectButton, () =>
            {
                OfferRejectButton.Visible = false;
                OfferRejectButton.Enabled = false;
            });
        }

        public void ActivateAwaitResponseMode()
        {
            UpdateStatus("Waiting for reponse to offer");
        }

        public void ActivateAwaitOfferMode()
        {
            GUI.ActionComponent(OfferAcceptButton, () =>
            {
                OfferAcceptButton.Visible = true;
                OfferAcceptButton.Enabled = false;
            });

            GUI.ActionComponent(OfferRejectButton, () =>
            {
                OfferRejectButton.Visible = true;
                OfferRejectButton.Enabled = false;
            });

            UpdateStatus($"Waiting to receive #{Game.NumRejections + 1} offer from {Game.GiverName}");
        }

        public void ActivateNeedToReplyMode()
        {
            GUI.ActionComponent(OfferAcceptButton, () =>
            {
                OfferAcceptButton.Visible = true;
                OfferAcceptButton.Enabled = true;
            });

            GUI.ActionComponent(OfferRejectButton, () =>
            {
                OfferRejectButton.Visible = true;
                OfferRejectButton.Enabled = true;
            });
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            log($"User clicked the {ConnectButton.Text} button");
            if (!User.Connected)
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
                                if (registerForm.ShowDialog() == DialogResult.OK)
                                {
                                    log($"User completed registration form");
                                    var username = registerForm.Username;
                                    var password = registerForm.Password;
                                    var email = registerForm.Email;

                                    var reply = RegisterRequestServerMessage.Create(username, password, email);
                                    WriteToServer(reply);
                                }
                            }
                        }
                        else
                        {
                            log($"User completed login form");
                            string username = loginForm.Username;
                            string password = loginForm.Password;

                            User.Name = username;

                            var reply = LoginRequestServerMessage.Create(username, password);
                            WriteToServer(reply);
                        }
                    }
                }
            }
            else
            {
                GUI.ActionComponent(this, () =>
                {
                    Text = FormHeader;
                });


                PrepareForLogin();

                var msg = LogoutServerMessage.Create();
                WriteToServer(msg);
            }
        }

        private void JoinLobbyButton_Click(object sender, EventArgs e)
        {
            log($"User clicked the {JoinLobbyButton.Text} button");
            if (!User.InLobby)
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
                                    WriteToServer(reply);
                                }
                            }
                        }
                        else
                        {
                            log($"User completed join lobby form");
                            var lobbyName = joinLobbyForm.LobbyName;
                            var entryCode = joinLobbyForm.EntryCode;

                            var reply = JoinLobbyRequestServerMessage.Create(lobbyName, entryCode);
                            WriteToServer(reply);
                        }
                    }
                }
            }
            else
            {
                log($"User asked to exit lobby");

                var reply = ExitLobbyRequestServerMessage.Create(User.Name);
                WriteToServer(reply);

                ExitLobby();
            }
        }

        private bool ClientLoop(CancellationToken token)
        {
            log("ClientLoop - Main client loop started");

            // Add code here to search for server

            var client = new TcpClient(Global.SERVER_IP, Global.SERVER_TCPPORT);
            log("Client trying to connect to server");
            if (client is null || !client.Connected)
            {
                MessageBox.Show("Failed to establish connection!!! Quitting", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                log($"ClientLoop - Failed to establish initial connection to server");
                return false;
            }
            log("ClientLoop - Client connected to server");

            if (!EstablishConnection(client!, token))
            {
                MessageBox.Show("Failed to establish connection!!! Quitting", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                client.Close();

                log($"ClientLoop - Failed to get encryption keys");
                return false;
            }
            log($"ClientLoop - got encryption keys");

            PrepareForLogin();

            var handler = new MessageHandler(this, User);
            CommMessage? message;
            try
            {
                log($"Enter main loop");
                while (!token.IsCancellationRequested && User.Reader.ReadMessage(out message))
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

        public void LoginUser(bool success, string? reason)
        {
            log($"LoginUser - recieved response to login attempt - {(success ? "Success" : "Failure")}{(success ? "" : ":" + reason!)}");
            if (success)
            {
                log($"LoginUser - login succeeded");
                UpdateConnectionStatusConnected();

                User.Connected = true;

                GUI.ActionComponent(this, () =>
                {
                    Text = FormHeader + $" - {User.Name}";
                });

                GUI.ActionComponent(ConnectButton, () =>
                {
                    ConnectButton.Text = "Disconnect";
                });
                log($"LoginUser - updating connection button");

                PrepareForJoinLobby();
            }
            else
            {
                log($"LoginUser - Failed. Notifying user");
                MessageBox.Show($"Login Error - {reason}", "Login", MessageBoxButtons.OK, MessageBoxIcon.Error);
                User.ResetName();
            }
        }

        private void PrepareForLogin()
        {
            User.Connected = false;
            UpdateConnectionStatusNotConnected();

            log($"Login - Prepare UI for login");

            GUI.ActionComponent(ConnectButton, () =>
            {
                ConnectButton.Text = "Connect";
                ConnectButton.Enabled = true;
                ConnectButton.Visible = true;
                ConnectButton.Focus();
            });

            GUI.ActionComponent(JoinLobbyButton, () =>
            {
                JoinLobbyButton.Visible = false;
            });

            GUI.ActionComponent(GameLogTextBox, () =>
            {
                GameLogTextBox.Clear();
            });

            GUI.AppendText($"Please log in or register", GameLogTextBox, false, true);

            // Wait for login to complete
            log($"Login - done preparing UI for login");
        }

        private void PrepareForJoinLobby()
        {
            log($"Prepare UI for enterring loby");
            GUI.ActionComponent(JoinLobbyButton, () =>
            {
                JoinLobbyButton.Text = "Join Lobby";
                JoinLobbyButton.Visible = true;
                JoinLobbyButton.Enabled = true;
                JoinLobbyButton.Focus();
            });

            GUI.AppendText($"Please enter existing lobby or create new one", GameLogTextBox, false, true);
        }

        public void UserJoinLobby(bool success, bool isHost, string? reason)
        {
            if (success)
            {
                User.IsHost = isHost;
                User.InLobby = true;

                UpdateConnectionStatusInLobby();

                GUI.ActionComponent(JoinLobbyButton, () =>
                {
                    JoinLobbyButton.Text = "Exit Lobby";
                });

                GUI.ActionComponent(ConnectButton, () =>
                {
                    ConnectButton.Enabled = false;
                });

                GUI.ActionComponent(StartGameButton, () =>
                {
                    StartGameButton.Visible = true;
                    StartGameButton.Enabled = false;
                });

                log($"Updated UI after successful lobby entrance");
            }
            else
            {
                log($"User failed to join lobby");
                MessageBox.Show($"Join Lobby Error - {reason}", "Join Lobby", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool EstablishConnection(TcpClient client, CancellationToken token)
        {
            var nws = client.GetStream();
            var reader = new StreamReader(nws, Encoding.UTF8, leaveOpen: true);
            var writer = new StreamWriter(nws, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };

            log("EstablishConnection - got streams");

            var sessionAesKey = Encryption.GenerateAesKey();
            var aesMsg = AesKeyMessage.Create(Encryption.RsaEncrypt(sessionAesKey, Global.PUBLIC_KEY)).Text;
            writer.WriteLine(aesMsg);
            log($"EstablishConnection - sent AES key <{sessionAesKey}> to server");

            int? myId = null;

            // Get public key and Id from server
            while (!token.IsCancellationRequested)
            {
                log("EstablishConnection - enter loop waiting to receive user Id from server");
                string? msg;
                if ((msg = reader.ReadLine()) != null)
                {
                    log($"EstablishConnection - got message from server: {msg}");
                    var parsedMessage = CommMessage.FromText(msg);
                    log($"EstablishConnection - parsed message: {parsedMessage.Text}");

                    if (parsedMessage.Type == CommMessage.MessageType.UserId && parsedMessage is UserIdMessage userIdMsg)
                    {
                        myId = userIdMsg.Id;

                        log($"EstablishConnection - got user id message. My Id: {myId}");

                        break;
                    }
                }
            }
            log("EstablishConnection - done getting userId from server");

            if (myId is not null)
            {
                logger_ = new Logger($"Client #{myId}");

                try
                {
                    User = new UserData(myId.Value, client, nws, sessionAesKey, logger_);
                    return true;
                }
                catch (Exception e)
                {
                    log($"EstablishConnection - failed create UserData: {e.Message}");
                    return false;
                }

            }
            else
                log($"EstablishConnection - failed to get UserId");

            return false;
        }

        internal void EnableStartGame(bool canStart)
        {
            if (canStart)
            {
                User.Logger.Log($"EnableStartGame - activate StartGame button");
                GUI.ActionComponent(StartGameButton, () => { StartGameButton.Enabled = true; StartGameButton.Focus(); });
            }
            else
            {
                User.Logger.Log($"EnableStartGame - deactivate StartGame button");
                GUI.ActionComponent(StartGameButton, () => { StartGameButton.Enabled = false; });
            }
        }

        public void DisplayCards(List<Card> cards)
        {
            void updatePicture(PictureBox pict, Image? image)
            {
                GUI.ActionComponent(pict, () =>
                {
                    pict.Image = image;
                    pict.Invalidate();
                });
            }

            log($"Updating the card display: {string.Join(',', cards)}");

            int i = 0;
            foreach (Card c in cards)
            {
                updatePicture(cardPictures_[i++], c.Picture);
            }

            if (i < cardPictures_.Count)
            {
                updatePicture(cardPictures_[i], null);
            }
        }

        public void ClearCardDisplay()
        {
            foreach (var pb in cardPictures_)
            {
                GUI.ActionComponent(pb, () =>
                {
                    pb.Image = null;
                    pb.Invalidate();
                });
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

        public void DisableRejectedCard(int index)
        {
            var pb = cardPictures_[index];

            GUI.ActionComponent(pb, () =>
            {
                var grayedImage = GUI.MakeGrayscale(pb.Image);
                pb.Image = grayedImage;
                pb.Invalidate();
            });
        }

        void UpdateConnectionStatusConnected()
        {
            UpdateConnectionStatus("Connected!", Color.Green);
        }

        void UpdateConnectionStatusInLobby()
        {
            UpdateConnectionStatus($"In Lobby - {(User.IsHost ? CommMessage.LobbyStatus.Host : CommMessage.LobbyStatus.Guest)}!", Color.Purple);
        }

        void UpdateConnectionStatusNotConnected()
        {
            UpdateConnectionStatus("Not Connected!", Color.Black);
        }

        void UpdateConnectionStatusInGame()
        {
            UpdateConnectionStatus("In Game!", Color.Red);
        }

        void UpdateConnectionStatus(string text, Color color)
        {
            GUI.ActionComponent(ConnectionStatusLabel, () =>
           {
               ConnectionStatusLabel.Text = text;
               ConnectionStatusLabel.ForeColor = color;
           });

            log($"Updating connection status label: <{text}>");
        }

        public void AppendToChat(string text, bool bold, bool endLine)
        {
            log($"Adding text <{text}> to ChatBox");
            GUI.AppendText(text, ChatBox, bold, endLine);
        }

        public void AppendToGameLog(string text)
        {
            log($"Adding text <{text}> to GameLog");
            GUI.AppendText(text, GameLogTextBox, false, true);
        }

        public void UpdateStatus(string text)
        {
            log($"Updating status label text: {text}");
            GUI.Update(text, StatusLabel);
        }

        private void WriteToServer(CommMessage message)
        {
            log($"Sending mesage to server: <{message.Text}>");
            User.Writer.WriteMessage(message, true);
        }

        private void ChatInputBox_KeyUp(object sender, KeyEventArgs e)
        {
            bool isValid(string text)
            {
                return (!text.Contains('|') && text.Length <= 120);
            }

            if (e.KeyCode != Keys.Enter)
                return;

            var userInput = ChatInputBox.Text;
            userInput = userInput.Substring(0, userInput.Length - 1);

            if (isValid(userInput))
            {
                log($"User done creating chat text <{userInput}>. Sending broadcast message");

                var response = BroadcastChatServerMessage.Create(userInput);
                WriteToServer(response);

                GUI.AppendText("me: ", ChatBox, true, false);
                GUI.AppendText(userInput, ChatBox, false, true);
            }

            GUI.ActionComponent(ChatInputBox, () =>
            {
                ChatInputBox.Text = "";
                ChatInputBox.Select(0, 0);
                ChatInputBox.Focus();
            });
        }

        private void OfferAcceptButton_Click(object sender, EventArgs e)
        {
            log($"User accepted the offer");

            var response = ResponseToOfferMessage.Create(true);
            WriteToServer(response);
        }

        private void OfferRejectButton_Click(object sender, EventArgs e)
        {
            log($"User rejected the offer");
            var response = ResponseToOfferMessage.Create(false);
            WriteToServer(response);

            Game.NumRejections++;

            Game.PlayerMode = GameState.Mode.AwaitOffer;
        }

        private void CardPicture_DoubleClick(object sender, EventArgs e)
        {
            // Ignore double clicks unless we are in MakeOffer mode
            if (Game.PlayerMode != GameState.Mode.MakeOffer)
                return;

            // Clear previous selection (remove border by forcing repaint)
            if (Game.OfferedCardIndex is not null)
            {
                var index = Game.OfferedCardIndex.Value;
                Game.OfferedCardIndex = null;
                cardPictures_[index].Invalidate();
            }

            // Set new selected
            var selectedPictureBox = sender as PictureBox;
            for (int i = 0; i < cardPictures_.Count; i++)
            {
                if (cardPictures_[i] == selectedPictureBox)
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
            WriteToServer(response);
        }

        private void CardPicture_Paint(object sender, PaintEventArgs e)
        {
            if (game_ is null)
                return;

            PictureBox? pb = sender as PictureBox;
            if (pb is not null)
            {
                if (Game.OfferedCardIndex is not null)
                {
                    if (pb == cardPictures_[Game.OfferedCardIndex.Value])
                    {
                        using (Pen pen = new Pen(Color.Red, 3))
                        {
                            e.Graphics.DrawRectangle(pen, 0, 0, pb.Width - 1, pb.Height - 1);
                        }
                    }
                }
            }
        }

        private void StartGameButton_Click(object sender, EventArgs e)
        {
            if (game_ is null)
            {
                log($"Send start game message to server");

                var response = StartGameServerMessage.Create();
                WriteToServer(response);
            }
            else
            {
                log($"Send interupt game message to server");

                var response = InterruptGameMessage.Create(User.Name);
                WriteToServer(response);
            }
        }

        public void ShutDown()
        {
            cts_.Cancel();
            User.ShutDown();

            Application.Exit();
        }
    }
}
