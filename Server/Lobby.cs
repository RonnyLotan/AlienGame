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

        public ClientHandler getClientHandler(int id)
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
            log(text);
        }

        public List<UserData> getGuestUsers()
        {
            lock (UpdateLock)
            {
                return guestInfo_.Values.Select(p => p.User).ToList();

            }
        }

        public void ExitLobbyRequest(string name)
        {
            if (name == Host)
            {
                log($"ExitLobbyRequest - host <{name}> is leaving the lobby");
                BroadcastGameLogMessage($"The host {name} left. Lobby is closing!");
                
                List<ClientHandler> guests;

                // Shut down the lobby by removing all users and terminating its message loop
                lock (UpdateLock)
                {
                    guests = guestInfo_.Values.ToList();
                    guestInfo_.Clear();

                    cts_.Cancel();
                }

                // Notify all users that the lobby is closing
                var exitMsg = LobbyClosingClientMessage.Create();
                foreach (var guest in guests)
                {
                    var user = guest.User;
                    if (user.Name !=  name)
                        WriteUser(user.Id, exitMsg);

                    user.ResetLobby();
                    guest.Start();
                }
            }
            else
            {
                log($"ExitLobbyRequest - guest <{name}> left the lobby");
                BroadcastGameLogMessage($"The guest {name} left the lobby");

                lock (UpdateLock)
                {
                    var keysToRemove = guestInfo_.Where(kvp => kvp.Value.User.Name == name)
                       .Select(kvp => kvp.Key)
                       .ToList();

                    guestInfo_.Remove(keysToRemove.First());

                    var canStart = guestInfo_.Count >= MIN_NUMBER_OF_PLAYERS;

                    WriteUser(hostInfo_.Id, CanStartGameClientMessage.Create(canStart));
                    log($"Notify host if game can start");
                }
            }
        }

        internal void BroadcastGameLogMessage(string msg)
        {
            var logMsg = GameLogClientMessage.Create(msg);
            foreach (var user in getGuestUsers())
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
                foreach (var user in getGuestUsers())
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
            foreach (var user in getGuestUsers())
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
                socketToClientDict_.Add(guest.User.Socket.Client, guest);
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

        public void Stop(String reason)
        {
            log($"Closing the lobby: {reason}, Game State: {this}");
            cts_.Cancel();
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
            server_.DisconnectUser(client);

            void close_lobby(GameLogClientMessage msg)
            {
                foreach (var c in guestInfo_.Values)
                {
                    // Send the message to all guests in the lobby
                    if (c.User.Name != Host)
                    {
                        WriteUser(c.Id, msg);
                        c.Start();
                    }
                }

                cts_.Cancel(true);
                server_.PurgeLobby(this);
            }

            void close_game(GameLogClientMessage msg)
            {
                foreach (var c in guestInfo_.Values)
                {
                    // Send the message to all guests in the lobby
                    if (c.User.Name != Host)
                    {
                        WriteUser(c.Id, msg);
                    }
                }

                EndGame();
            }

            if (client.User.Name == Host)
            {
                var msg = GameLogClientMessage.Create($"The host {Host} has left the Loby {(isError ? "because of error" : "")}. Everyone must exit.");
                close_lobby(msg);
            }
            else
            {
                var msg = GameLogClientMessage.Create($"The guest {client.User.Name} has left the Loby {(isError ? "because of error" : "")}.");
                close_game(msg);
            }
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

                foreach (Socket socket in readSockets)
                {
                    // Handle existing client
                    ClientHandler client;
                    lock (UpdateLock)
                    {
                        client = socketToClientDict_[socket];
                    }

                    var user = client.User;
                    log($"Received message from user <{user}>");
                    try
                    {
                        CommMessage? message;
                        if (user.Reader.ReadMessage(out message))
                        {
                            if (message is not null)
                            {
                                log($"MessageLoop - received message: {message.Text}");
                                messageHandler.Handle(message, user);
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
                }
            }
        }
    }
}
