using Shared;
using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;

namespace Server
{
    public partial class Server : Form
    {
        private TcpListener listener_;
        
        private CancellationTokenSource cts_;

        private List<ClientHandler> clients_;
        public List<ClientHandler> Clients { get => clients_; }

        private Dictionary<string, Lobby> lobbies_;
        public Dictionary<string, Lobby> Lobbies {  get => lobbies_; }

        public string PrivateKey;
        public string PublicKey;      
        
        Logger logger_;

        public Server()
        {
            listener_ = new TcpListener(IPAddress.Any, Global.SERVER_TCPPORT);

            (PrivateKey, PublicKey) = Encryption.GenerateRsaKeyPair();

            logger_ = new Logger("Server");

            clients_ = new List<ClientHandler>();
            lobbies_ = new Dictionary<string, Lobby>();

            cts_ = new CancellationTokenSource();

            _ = logger_.Log($"Server is constructed");

            InitializeComponent();
        }

        public void DisconnectUser(ClientHandler client)
        {
            _ = logger_.Log($"Disconnecting user: {client.User}");
            client.User.Close();
            lock (Clients)
            {
                Clients.Remove(client);                
            }
        }

        public bool JoinLobby(string name, ClientHandler guest, out string? reason)
        {
            _ = logger_.Log($"Trying to add user: {guest.User} to lobby: {name}");
            lock (Lobbies)
            {
                if (Lobbies.TryGetValue(name, out var lobby))
                {
                    if (!lobby.GameInProgress)
                    {
                        lobby.AddGuest(guest);

                        guest.EnterLobby(lobby);

                        _ = logger_.Log($"User: {guest.User} added to lobby: {name}");

                        reason = null;
                        return true;
                    }
                    else
                    {
                        reason = "game in progress";
                        _ = logger_.Log($"Failed to add user: {guest.User} to lobby: {lobby} - {reason}");
                    }
                }
                else
                {
                    reason = "lobby is not open";
                    _ = logger_.Log($"Failed to add user: {guest.User} to lobby: {lobby} - {reason}");
                }
            }

            return false;
        }

        public void OpenLobby(string name, ClientHandler guest)
        {
            _ = logger_.Log($"Create a new lobby: {name} and add user: {guest.User} to lobby");
            Lobby lobby;
            lock (Lobbies)
            {
                lobby = new Lobby(name, guest, this);
                Lobbies.Add(name, lobby); 
                lobby.Start();
            }

            guest.EnterLobby(lobby);
        }

        public void AddClient(ClientHandler client)
        {
            _ = logger_.Log($"New client added to client list: {client}");
            lock (Clients)
            { Clients.Add(client); }
        }

        public bool IsClientLoggedIn(string username)
        {
            lock (Clients)
            {
                return Clients.Find(c => c.User.LoggedIn && c.User.Name == username) is not null;
            }
        }

        private void LoadAsync(object sender, EventArgs e)
        {
            listener_.Start();

            var listenerThread = new Thread(() => ListenerAction(cts_.Token));

            _ = logger_.Log($"Launching listener thread");

            listenerThread.IsBackground = true;
            listenerThread.Start();
        }

        void ListenerAction(CancellationToken token)
        {
            _ = logger_.Log($"Server started on port {Global.SERVER_IP}");

            while (!token.IsCancellationRequested)
            {
                // Build socket list for Select
                List<Socket> readSockets = new List<Socket>();

                // Add listener socket
                readSockets.Add(listener_.Server);

                // Use Select to wait for any socket ready to read
                Socket.Select(readSockets, null, null, 1000 * 1000); // Timeout in microseconds

                foreach (Socket socket in readSockets)
                {
                    if (socket == listener_.Server)
                    {
                        _ = logger_.Log($"A new client has connected");

                        // Accept new client
                        var handler = new ClientHandler(listener_.AcceptTcpClient(), this);
                        
                        _ = logger_.Log($"New client connected: #{handler.Id}");

                        handler.Start();
                    }
                    
                }
            }
        }

        internal void PurgeLobby(Lobby lobby)
        {
            lock (lobbies_)
            {
                lobbies_.Remove(lobby.Name);
            }
        }
    }
}
