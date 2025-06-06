using Microsoft.VisualBasic.ApplicationServices;
using Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;

namespace Server
{
    public class Lobby
    {
        static int MIN_NUMBER_OF_PLAYERS = 2;

        public string Name { get; set; }
        public string Host { get; set; }

        private Game? game_;
        public bool GameInProgress { get => game_ is not null; }
        public Game Game
        {
            get
            {
                if (game_ == null)
                {
                    throw new InvalidOperationException("Accessing Game before it was initialized");
                }

                return game_;
            }
        }

        private Dictionary<int, ClientHandler> guestInfo_;
        private ClientHandler hostInfo_;
        private Server server_;

        private Dictionary<Socket, ClientHandler> socketToClientDict_;
        private List<Socket> Sockets { get => socketToClientDict_.Keys.ToList(); }

        private CancellationTokenSource cts_;

        private Logger logger_;

        public readonly object UpdateLock = new object();

        public override string ToString()
        {
            return $"{Name}|{Host}|{guestInfo_.Count} guests|{(GameInProgress ? Game : null)}";
        }

        public ClientHandler GetClientHandler(int id)
        {
            lock (UpdateLock)
            {
                return guestInfo_[id];
            }
        }

        public Lobby(string name, ClientHandler host, Server server)
        {
            Name = name;

            Host = host.User.Name!;
            hostInfo_ = host;
            server_ = server;

            guestInfo_ = new Dictionary<int, ClientHandler>();
            socketToClientDict_ = new Dictionary<Socket, ClientHandler>();

            cts_ = new CancellationTokenSource();

            logger_ = new Logger($"Lobby-{name}");

            log($"Lobby is created");

            AddGuest(host);
        }

        private void log(string text)
        {
            _ = logger_.Log(text);
        }

        public List<UserData> GetGuestUsers()
        {
            lock (UpdateLock)
            {
                return guestInfo_.Values.Select(p => p.User).ToList();

            }
        }

        private void CloseLobby(string msg)
        {
            List<ClientHandler> guests;

            // Get list of users and terminate the message loop
            lock (UpdateLock)
            {
                log($"Clear the guest list and send cancel signal to loop");
                guests = guestInfo_.Values.ToList();
                
                cts_.Cancel();
            }

            log($"Send game log message to all users that the lobby is closing");
            var logMsg = GameLogClientMessage.Create(msg);
            foreach (var guest in guests)
            {
                // Send the message to all guests in the lobby
                if (guest.User.Name != Host)
                {
                    WriteUser(guest.Id, logMsg);
                }
            }

            // Notify all users that the lobby is closing
            log($"Notify all guests that the Lobby is closing");
            var exitMsg = LobbyClosingClientMessage.Create();
            foreach (var guest in guests)
            {
                var user = guest.User;
                if (user.Name != Host)
                    WriteUser(user.Id, exitMsg);

                user.ResetLobby();
                guest.Start();
            }

            server_.PurgeLobby(Name);
        }

        private void RemoveGuest(string msg, string leaver)
        {
            List<ClientHandler> guests;

            lock (UpdateLock)
            {
                // Remove the guest from the dictionary of guests
                var match = guestInfo_.FirstOrDefault(kv => kv.Value.User.Name == leaver);
                if (!match.Equals(default(KeyValuePair<int, ClientHandler>)))
                {
                    var id = match.Key;
                    var handler = match.Value;

                    log($"RemoveGuest - remove guest from dictionaries and start client handler loop");
                    guestInfo_.Remove(id);

                    if (handler != null)
                    {
                        socketToClientDict_.Remove(handler.User.Client.Client);
                        handler.Start();
                    }
                    else
                        log($"RemoveGuest - the removed guest handler is null");
                }

                guests = guestInfo_.Values.ToList();
            }

            // Send the message to all guests remaining in the lobby
            var logMsg = GameLogClientMessage.Create(msg);
            foreach (var c in guests)
                WriteUser(c.Id, logMsg);

            lock (UpdateLock)
            {
                var canStart = guests.Count >= MIN_NUMBER_OF_PLAYERS;

                WriteUser(hostInfo_.Id, CanStartGameClientMessage.Create(canStart));
                log($"Notify host if game can start");
            }
        }

        public void ExitLobbyRequest(string name)
        {
            if (name == Host)
            {
                log($"ExitLobbyRequest - host <{name}> is leaving the lobby");

                var logMsg = $"The host {name} left. Lobby {Name} is closing!";

                CloseLobby(logMsg);
            }
            else
            {
                var logMsg = $"The guest {name} left the lobby";

                RemoveGuest(logMsg, name);

                if (GameInProgress)
                    EndGame();
            }
        }

        internal void BroadcastGameLogMessage(string msg)
        {
            var logMsg = GameLogClientMessage.Create(msg);
            foreach (var user in GetGuestUsers())
            {
                WriteUser(user.Id, logMsg);
            }
        }

        public void StartGame()
        {
            var players = new List<Player>();
            lock (UpdateLock)
            {
                foreach (var client in guestInfo_.Values)
                {
                    var player = new Player(client.User.Id, client.User.Name!);
                    players.Add(player);
                    log($"StartGame - add player {player} to the game");
                }

                game_ = new Game(players);
                log($"StartGame - created a new game");
            }

            log($"StartGame - dealing the cards");
            // Deal cards to all players and announce the start of the game
            foreach (var p in players)
            {
                var msg1 = DealCardsClientMessage.Create(p.Cards);
                WriteUser(p.Id, msg1);
                log($"StartGame - sent cards to player: {p}");
            }

            if (!CheckGameOver())
                NotifyClientsOfNewTurn();
        }

        // Check is the game is over and if so end it
        public bool CheckGameOver()
        {
            var winner = Game.DoWeHaveAWinner();
            if (winner is not null)
            {
                log($"CheckGameOver - winner found: <{winner.Name}>");

                var loser = Game.FindPlayerWithJoker();
                log($"CheckGameOver - loser found: <{loser.Name}>");

                var msg = AnnounceWinnerClientMessage.Create(winner.Name, loser.Name);
                foreach (var user in GetGuestUsers())
                {
                    WriteUser(user.Id, msg);
                }
                log($"CheckGameOver - announce winner message sent to all clients");

                EndGame();

                return true;
            }

            return false;
        }

        public void NotifyClientsOfInterrupt(InterruptGameMessage msg)
        {
            log($"Interrupt game message sent to all clients");
            foreach (var user in GetGuestUsers())
            {
                WriteUser(user.Id, msg);
            }
        }

        public void NotifyClientsOfNewTurn()
        {
            log($"NotifyClientsOfNewTurn - notifying new Giver <{Game.Giver}> and Receiver <{Game.Receiver}>");

            // Let the Giver know they need to make an offer
            var msg2 = MakeOfferClientMessage.Create(Game.NumRejections, Game.Receiver.Name);
            WriteUser(Game.Giver.Id, msg2);

            // Let the Receiver know they will receive an offer
            var msg3 = ReceiveOfferClientMessage.Create(Game.Giver.Name);
            WriteUser(Game.Receiver.Id, msg3);

            BroadcastGameLogMessage($"{Game.Giver.Name} will offer cards to {Game.Receiver.Name}");
        }

        public void EndGame()
        {
            lock (UpdateLock)
            {
                game_ = null;
            }

            log($"EndGame - the game is terminated");
        }

        public void AddGuest(ClientHandler guest)
        {
            List<int> ids = new List<int>();
            int cnt;
            lock (UpdateLock)
            {
                ids = guestInfo_.Keys.ToList();

                guestInfo_.Add(guest.Id, guest);
                socketToClientDict_.Add(guest.User.Client.Client, guest);
                log($"Guest added to lobby. {guestInfo_.Count} guests");

                cnt = guestInfo_.Count;
            }

            var msg = GameLogClientMessage.Create($"{guest.User.Name} has entered the lobby");

            foreach (var i in ids)
            {
                WriteUser(i, msg);
            }
            log($"AddGuest - sent notification to all other guests");

            if (cnt == MIN_NUMBER_OF_PLAYERS)
            {
                WriteUser(hostInfo_.Id, CanStartGameClientMessage.Create(true));
                log($"AddGuest - notify host game can start");
            }
        }

        public void Start()
        {
            log($"Starting the lobby thread");

            var trd = new Thread(() => MessageLoop(cts_.Token));
            trd.IsBackground = true;
            trd.Start();
        }

        public void WriteUser(int id, CommMessage message)
        {
            log($"WriteUser - sending user #{id}: {message.Text}");
            lock (UpdateLock)
            {
                guestInfo_[id].User.Writer.WriteMessage(message);
            }
        }

        public void HandleUserLeft(ClientHandler client, bool isError)
        {
            if (client.User.Name == Host)
            {
                CloseLobby($"The host {Host} has left the Loby {(isError ? "because of error" : "")}. Everyone must exit.");
            }
            else
            {
                if (GameInProgress)
                    EndGame();

                RemoveGuest($"The guest {client.User.Name} has left the Loby {(isError ? "because of error" : "")}.", client.User.Name);
            }

            server_.DisconnectUser(client);
        }

        private void MessageLoop(CancellationToken token)
        {
            var messageHandler = new LobbyMessageHandler(this, logger_);
            log($"Starting message loop");

            while (!token.IsCancellationRequested)
            {
                // Build socket list for Select
                List<Socket> readSockets;
                lock (UpdateLock)
                {
                    readSockets = new List<Socket>(Sockets);
                }

                // Use Select to wait for any socket ready to read
                Socket.Select(readSockets, null, null, 1000 * 1000); // Timeout in microseconds

                if (readSockets.Count > 0)
                {
                    log($"Received messages on {readSockets.Count} sockets");
                }

                var i = 1;
                foreach (Socket socket in readSockets)
                {
                    // Handle existing client
                    ClientHandler client;
                    lock (UpdateLock)
                    {
                        client = socketToClientDict_[socket];
                    }

                    var user = client.User;
                    log($"#{i} message received from user <{user}>");
                    try
                    {
                        CommMessage? message;
                        if (user.Reader.ReadMessage(out message))
                        {
                            if (message is not null)
                            {
                                log($"MessageLoop - received message: {message.Text}");
                                try
                                {
                                    messageHandler.Handle(message, user);
                                }
                                catch (Exception e)
                                {
                                    log($"Caught error handling message for user <{user}>: {e.Message}");

                                    var reply = CommunicationErrorMessage.Create(e.Message);
                                    WriteUser(user.Id, reply);

                                    HandleUserLeft(client, false);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // User has disconnected.
                            log($"User <{user}> has left the game");
                            HandleUserLeft(client, false);
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        log($"User <{user}> had an error: {e.Message}");
                        HandleUserLeft(client, true);
                    }

                    i++;
                }
            }

            log($"exiting message loop");
        }
    }
}
