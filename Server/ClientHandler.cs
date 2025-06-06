using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class ClientHandler
    {
        static int counter_ = 0;
        private Server server_;
        private Logger logger_;

        private UserData user_;
        public UserData User { get => user_; }

        public int Id { get => User.Id; }

        public ClientHandler(TcpClient client, Server server)
        {
            user_ = new UserData(client, Interlocked.Increment(ref counter_));

            logger_ = new Logger($"Server Client{User}");
            log($"In ClientHandler constructor");

            server_ = server;

            log($"Writer created with AES key:{user_.Writer.SessionAesKey}");
        }

        private void log(string text)
        {
            logger_.Log(text);
        }

        public void Start()
        {
            log($"ClientHandler for user <{User}> is starting");
            new Thread(() => ClientLoop()) { IsBackground = true }.Start();
        }
        
        void ClientLoop()
        {
            try
            {
                log($"Entering message loop for user {User}");

                CommMessage? message;
                while (User.Reader.ReadMessage(out message))
                {
                    log($"Received message from user {User}: {message!.Text}");

                    if (message is not null)
                    {
                        HandleParsedMessage(message);
                    }

                    // If the user enterd a lobby it no longer needs this thread to communicate with the server
                    if (User.InLobby)
                    {
                        log($"Exiting message loop for user {User}");
                        break;
                    }
                }

                log($"User {User} has entered lobby");
            }
            catch (Exception ex)
            {
                log($"User {User} had error: {ex.Message}. Disconnecting!");
                Disconnect();
            }
        }

        void Disconnect()
        {
            server_.DisconnectUser(this);
        }

        private void HandleParsedMessage(CommMessage msg)
        {
            log($"ClientHandler: Received message {msg.Text}");

            switch (msg.Type)
            {
                case CommMessage.MessageType.PublicKey:
                    if (msg is PublicKeyMessage keyMsg)
                    {
                        User.PublicKey = keyMsg.Key;
                        log($"Public key {User.PublicKey} received from client #{User.Id}");

                        User.Writer.sendAesKey(keyMsg.Key);
                        log($"AES encryption key sent to user #{User.Id}");
                    }
                    break;

                case CommMessage.MessageType.LoginRequest:
                    if (msg is LoginRequestServerMessage loginMsg)
                    {
                        if (!User.Writer.EncryptionEstablished)
                        {
                            var response = LoginResponseMessage.Create(false, "Encryption has not been established. Please send public key");
                            User.Writer.WriteMessage(response);
                            log($"User {User} has not established encryption");
                        }
                        else if (server_.IsClientLoggedIn(loginMsg.UserName))
                        {
                            var response = LoginResponseMessage.Create(false, "This user is already logged in");
                            User.Writer.WriteMessage(response);
                            log($"User {User} has already logged in");
                        }
                        else
                        {
                            Shared.User? user = null;
                            try
                            {
                                user = Jsn.getUser(loginMsg.UserName);
                            }
                            catch (Exception ex)
                            {
                                var reply = LoginResponseMessage.Create(false, $"Login failed with error - {ex.Message}");
                                User.Writer.WriteMessage(reply);
                                log($"User {User} failed to logged in with error: {ex.Message}");
                                return;
                            }

                            if (user != null && user.HashedPassword == Encryption.ComputeHash(loginMsg.Password, user.Salt))
                            {
                                User.Name = user.Name;

                                var reply = LoginResponseMessage.Create(true, null);
                                User.Writer.WriteMessage(reply);

                                log($"User {User}|{user.Name} has logged in successfully");
                            }
                            else
                            {
                                string reason = user is null ? "Wrong user name" : "Wrong password";
                                var reply = LoginResponseMessage.Create(false, reason);
                                User.Writer.WriteMessage(reply);
                                log($"User {User} failed to log in: {reason}");
                            }
                        }
                    }
                    break;

                case CommMessage.MessageType.RegisterRequest:
                    if (msg is RegisterRequestServerMessage registerMsg)
                    {
                        bool registered = false;
                        try
                        {
                            registered = Jsn.RegisterUser(registerMsg.UserName, registerMsg.Password, registerMsg.Email);
                        }
                        catch (Exception ex)
                        {
                            var reply = RegisterResponseMessage.Create(false, $"Registration has failed - {ex.Message}");
                            User.Writer.WriteMessage(reply);
                            log($"User {registerMsg.UserName} failed to register: {ex.Message}");
                            return;
                        }

                        if (registered)
                        {
                            var reply = RegisterResponseMessage.Create(true, null);
                            User.Writer.WriteMessage(reply);
                            log($"User {registerMsg.UserName} has registered successfully");
                        }
                        else
                        {
                            var reply = RegisterResponseMessage.Create(false, "Something wrong with user data or user already exists");
                            User.Writer.WriteMessage(reply);
                            log($"User {registerMsg.UserName} failed to register: wrong user data or user already exists - {registerMsg.Text}");
                        }
                    }
                    break;

                case CommMessage.MessageType.JoinLobbyRequest:
                    if (msg is JoinLobbyRequestServerMessage joinLobbyMsg)
                    {
                        Shared.Lobby? lobby = null;
                        try
                        {
                            lobby = Jsn.getLobby(joinLobbyMsg.Name);
                        }
                        catch (Exception ex)
                        {
                            var reply = JoinLobbyResponseMessage.Create($"Failed to join lobby with error - {ex.Message}");
                            User.Writer.WriteMessage(reply);
                            log($"User {User} failed to join lobby {joinLobbyMsg.Name} with error: {ex.Message}");
                            return;
                        }

                        if (lobby != null && lobby.HashedEntryCode == Encryption.ComputeHash(joinLobbyMsg.EntryCode, lobby.Salt))
                        {
                            var success = true;
                            string? reason = null;
                            var isHost = lobby.Host == User.Name;

                            if (isHost)
                                server_.OpenLobby(lobby.Name, this);
                            else
                                success = server_.JoinLobby(lobby.Name, this, out reason);

                            var reply = success ? JoinLobbyResponseMessage.Create(isHost) : JoinLobbyResponseMessage.Create(reason!);
                            User.Writer.WriteMessage(reply);

                            if (success)
                                log($"User <{User}> has joined lobby <{lobby.Name}> successfully");
                            else
                                log($"User <{User}> failed to join lobby <{lobby}>: <{reason}>");
                        }
                        else
                        {
                            string reason = lobby is null ? "Unknown lobby name" : "Wrong password";
                            var reply = JoinLobbyResponseMessage.Create(reason);
                            User.Writer.WriteMessage(reply);
                            log($"User {User} failed to join lobby {joinLobbyMsg.Name}: {reason}");
                        }
                    }
                    break;

                case CommMessage.MessageType.CreateLobbyRequest:
                    if (msg is CreateLobbyRequestServerMessage createLobbyMsg)
                    {
                        bool created = false;
                        try
                        {
                            created = Jsn.RegisterLobby(createLobbyMsg.Name, createLobbyMsg.EntryCode, User.Name);
                        }
                        catch (Exception ex)
                        {
                            var reply = CreateLobbyResponseMessage.Create(false, $"Creation has failed - {ex.Message}");
                            User.Writer.WriteMessage(reply);
                            log($"Lobby {createLobbyMsg.Name} failed to create: {ex.Message}");
                            return;
                        }

                        if (created)
                        {
                            var reply = CreateLobbyResponseMessage.Create(true, null);
                            User.Writer.WriteMessage(reply);
                            log($"Lobby {createLobbyMsg.Name} was created successfully");
                        }
                        else
                        {
                            var reply = CreateLobbyResponseMessage.Create(false, "Something wrong with lobby data or lobby already exists");
                            User.Writer.WriteMessage(reply);
                            log($"Lobby {createLobbyMsg.Name} failed to create: wrong lobby data or lobby already exists - {createLobbyMsg.Text}");
                        }
                    }
                    break;

                case CommMessage.MessageType.Logout:
                    if (msg is LogoutServerMessage logoutMsg)
                    {
                        log($"User asked to logout");

                        User.ResetName();
                    }
                    break;

                case CommMessage.MessageType.CommunicationError:
                    if (msg is CommunicationErrorMessage commErrorMsg)
                    {
                        var error = $"Communication error: {commErrorMsg.Error}";
                        log(error);
                        throw new Exception(error);
                    }
                    break;
            }
        }
        internal void EnterLobby(Lobby lobby)
        {
            lock (User)
            {
                User.Lobby = lobby;
            }
        }
    }
}
