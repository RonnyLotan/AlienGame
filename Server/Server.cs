using Shared;
using System.Net;
using System.Net.Sockets;

namespace Server
{
    public partial class Server : Form
    {
        private TcpListener listener;
        
        private CancellationTokenSource cts_;

        private List<ClientHandler> clients_;
        public List<ClientHandler> Clients { get => clients_; }

        private Dictionary<string, Lobby> lobbies_;
        public Dictionary<string, Lobby> Lobbies {  get => lobbies_; }

        public string PrivateKey;
        public string PublicKey;      
        
        Logger Logger;

        public Server()
        {
            listener = new TcpListener(IPAddress.Any, Global.SERVER_TCPPORT);

            (PrivateKey, PublicKey) = Encryption.GenerateRsaKeyPair();

            Logger = new Logger("Server");

            clients_ = new List<ClientHandler>();
            lobbies_ = new Dictionary<string, Lobby>();

            cts_ = new CancellationTokenSource();

            InitializeComponent();
        }

        public void DisconnectUser(ClientHandler client)
        {
            client.User.Close();
            lock (Clients)
            {
                Clients.Remove(client);                
            }
        }

        public bool JoinLobby(string name, ClientHandler guest)
        {
            lock (Lobbies)
            {
                if (Lobbies.TryGetValue(name, out var lobby))
                {
                    if (!lobby.GameInProgress)
                    {
                        lobby.AddGuest(guest);

                        guest.EnterLobby(lobby);

                        return true;
                    }
                }
            }

            return false;
        }

        public void OpenLobby(string name, ClientHandler guest)
        {
            lock (Lobbies)
            {
                var lobby = new Lobby(name, guest, this);
                guest.EnterLobby(lobby);
            }
        }

        public void AddClient(ClientHandler client)
        {
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
            listener.Start();

            var listenerThread = new Thread(() => ThreadAction(cts_.Token));

            listenerThread.IsBackground = true;
            listenerThread.Start();
        }

        async void ThreadAction(CancellationToken token)
        {
            await Logger.Log($"Server started on port {Global.SERVER_IP}");

            while (!token.IsCancellationRequested)
            {
                // Build socket list for Select
                List<Socket> readSockets = new List<Socket>();

                // Add listener socket
                readSockets.Add(listener.Server);

                // Use Select to wait for any socket ready to read
                Socket.Select(readSockets, null, null, 1000 * 1000); // Timeout in microseconds

                foreach (Socket socket in readSockets)
                {
                    if (socket == listener.Server)
                    {
                        // Accept new client
                        var handler = new ClientHandler(listener.AcceptTcpClient(), this);
                        lock (clients_) {  clients_.Add(handler); }
                        
                        _ = Logger.Log($"New client connected: #{handler.Id}");

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
