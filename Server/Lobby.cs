using Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Lobby
    {
        static int MIN_NUMBER_OF_PLAYERS = 2;

        List<Player> Players;
        public string Name { get; set; }
        public string Host { get; set; }

        private Game? game_;
        public bool GameInProgress { get => game_ is not null; }
        public Game? Game { get => game_; }

        private Dictionary<int, ClientHandler> guestInfo_;
        private ClientHandler hostInfo_;
        private Server server_;

        private Dictionary<Socket, ClientHandler> socketToClientDict_;
        private List<Socket> Sockets { get => socketToClientDict_.Keys.ToList(); }

        private CancellationTokenSource cts_;

        private Logger logger_;

        public readonly object UpdateLock = new object();

        public Lobby(string name, ClientHandler host, Server server)
        {
            Name = name;
            Players = new List<Player>();

            Host = host.User.Name!;
            hostInfo_ = host;
            server_ = server;

            guestInfo_ = new Dictionary<int, ClientHandler>();
            socketToClientDict_ = new Dictionary<Socket, ClientHandler>();

            cts_ = new CancellationTokenSource();

            logger_ = new Logger($"Lobby-{name}");

            _ = logger_.Log($"Lobby is created");

            AddGuest(host);
        }

        public List<UserData> getGuestUsers()
        {
            lock (UpdateLock)
            {
                return guestInfo_.Values.Select(p => p.User).ToList();

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
                    _ = logger_.Log($"StartGame - add player {player} to the game");
                }

                game_ = new Game(Players);
                _ = logger_.Log($"StartGame - created a new game");
            }

            _ = logger_.Log($"StartGame - dealing the cards");
            foreach (var p in Players)
            {
                var msg = DealCardsClientMessage.Create(p.Cards);
                WriteUser(p.Id, msg);

                _ = logger_.Log($"StartGame - sent cards to player: {p}");
            }
        }

        public void EndGame()
        {
            lock (UpdateLock)
            {
                Players.Clear();
                game_ = null;
            }
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
                _ = logger_.Log($"Guest added to lobby. {guestInfo_.Count} guests");

                cnt = guestInfo_.Count;
            }

            var msg = GameLogClientMessage.Create($"AddGuest - User {guest.User.Name} has entered the lobby");

            foreach (var i in ids)
            {
                WriteUser(i, msg);
            }
            _ = logger_.Log($"AddGuest - sent notification to all other guests");

            if (cnt == MIN_NUMBER_OF_PLAYERS)
            {
                WriteUser(hostInfo_.Id, CanStartGameClientMessage.Create());
                _ = logger_.Log($"AddGuest - notify host game can start");
            }
        }

        public void Start()
        {
            _ = logger_.Log($"Starting the lobby thread");

            var trd = new Thread(() => MessageLoop(cts_.Token));
            trd.IsBackground = true;
            trd.Start();
        }

        public void Stop(String reason)
        {
            _ = logger_.Log($"Closing the lobby: {reason}, Game State: {this}");
            cts_.Cancel();
        }

        public void WriteUser(int id, CommMessage message)
        {
            _ = logger_.Log($"WriteUser - sending user #{id}: {message}");
            lock (UpdateLock)
            {
                guestInfo_[id].User.Writer.WriteMessage(message);
            }
        }

        void HandleUserLeft(ClientHandler client, bool isError)
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
                var msg = GameLogClientMessage.Create($"The host {Host} has left the Loby {(isError ? "because of error" : "")}. Everyone must exit.");
                close_game(msg);
            }
        }

        private void MessageLoop(CancellationToken token)
        {
            var messageHandler = new LobbyMessageHandler(this, logger_);
            logger_.Log($"Starting message loop");

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
                    logger_.Log($"Received message from user {user}");
                    try
                    {
                        CommMessage? message;
                        if (client.User.Reader.ReadMessage(out message))
                        {
                            if (message is not null)
                            {
                                logger_.Log($"MessageLoop - received message: {message.Text}");
                                messageHandler.Handle(message, user);
                            }
                        }
                        else
                        {
                            // User has disconnected.
                            _ = logger_.Log($"User #{user.Id}|{user.Name} has left the game");
                            HandleUserLeft(client, false);
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        _ = logger_.Log($"User #{user.Id}|{user.Name} had an error: {e.Message}");
                        HandleUserLeft(client, true);
                    }
                }
            }
        }
    }
}
