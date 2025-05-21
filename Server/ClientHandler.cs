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
        static int sockCount = 0;
        private Server server_;
        private Logger logger_;

        private UserData user_;
        public UserData User { get => user_; }

        public int Id { get => User.Id; }

        public ClientHandler(TcpClient socket, Server server)
        {
            user_ = new UserData(socket, Interlocked.Increment(ref sockCount));
            server_ = server;

            logger_ = new Logger($"Client#{User.Id}");
        }

        public void Start()
        {
            User.Lobby = null;
            new Thread(() => client_trd_loop()) { IsBackground = true }.Start();
        }
       
        void client_trd_loop()
        {
            try
            {
                _ = logger_.Log($"Client #{User.Id} connected, awaiting authentication...");
                server_.AddClient(this);

                if (server_.PublicKey is not null)
                {
                    var msg = PublicKeyMessage.Create(server_.PublicKey, User.Id);
                    User.Writer.WriteMessage(msg, false);
                }

                CommMessage? message;
                while (User.Reader.ReadMessage(out message))
                {
                    _ = logger_.Log($"Received message from user #{User.Id}: {message}");

                    if (message is not null)
                    {
                        HandleParsedMessage(message);
                    }

                    // If the user enterd a lobby it no longer needs this thread to communicate with the server
                    if (User.InLobby)
                        break;
                }

                _ = logger_.Log($"User #{User.Id}|{User.Name} has entered lobby");
            }
            catch (Exception ex)
            {
                _ = logger_.Log($"User #{User.Id}|{User.Name} had error: {ex.Message}. Disconnecting!");
                Disconnect();
            }
            finally
            {
                _ = logger_.Log($"User #{User.Id}|{User.Name} had unknown error. Disconnecting!");
                Disconnect();
            }
        }

        void Disconnect()
        {
            server_.DisconnectUser(this);
        }

        private async void HandleParsedMessage(CommMessage msg)
        {
            await logger_.Log($"ClientHandler: Received message {msg}");

            switch (msg.Type)
            {
                case CommMessage.MessageType.PublicKey:
                    if (msg is PublicKeyMessage keyMsg)
                    {
                        User.PublicKey = keyMsg.Key;
                        _ = logger_.Log($"Public key {keyMsg.Key} received from client #{User.Id}");

                        User.Writer.sendAesKey(keyMsg.Key);
                        _ = logger_.Log($"AES encryption key sent to user #{User.Id}");
                    }
                    break;

                case CommMessage.MessageType.LoginRequest:
                    if (msg is LoginRequestServerMessage loginMsg)
                    {
                        if (!User.Writer.EncryptionEstablished)
                        {
                            var response = LoginResponseMessage.Create(false, "Encryption has not been established. Please send public key");
                            User.Writer.WriteMessage(response);
                            _ = logger_.Log($"User #{User.Id}|{User.Name} has not established encryption");
                        }
                        else if (server_.IsClientLoggedIn(loginMsg.UserName))
                        {
                            var response = LoginResponseMessage.Create(false, "This user is already logged in");
                            User.Writer.WriteMessage(response);
                            _ = logger_.Log($"User #{User.Id}|{User.Name} has already logged in");
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
                                _ = logger_.Log($"User #{User.Id} failed to logged in with error: {ex.Message}");
                                return;
                            }

                            if (user != null && user.HashedPassword == loginMsg.Password)
                            {
                                User.Name = user.Name;

                                var reply = LoginResponseMessage.Create(true, null);
                                User.Writer.WriteMessage(reply);

                                await logger_.Log($"User #{User.Id}|{user.Name} has logged in successfully");
                            }
                            else
                            {
                                string reason = user is null ? "Wrong user name" : "Wrong password";
                                var reply = LoginResponseMessage.Create(false, reason);
                                User.Writer.WriteMessage(reply);
                                await logger_.Log($"User #{User.Id} failed to log in: {reason}");
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
                            await logger_.Log($"User {registerMsg.UserName} failed to register: {ex.Message}");
                            return;
                        }

                        if (registered)
                        {
                            var reply = RegisterResponseMessage.Create(true, null);
                            User.Writer.WriteMessage(reply);
                            await logger_.Log($"User {registerMsg.UserName} has registered successfully");
                        }
                        else
                        {
                            var reply = RegisterResponseMessage.Create(false, "Something wrong with user data or user already exists");
                            User.Writer.WriteMessage(reply);
                            await logger_.Log($"User {registerMsg.UserName} failed to register: wrong user data or user already exists - {registerMsg.Text}");
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
                            var reply = JoinLobbyResponseMessage.Create(false, $"Failed to join lobby with error - {ex.Message}");
                            User.Writer.WriteMessage(reply);
                            await logger_.Log($"User #{User.Id} failed to join lobby {joinLobbyMsg.Name} with error: {ex.Message}");
                            return;
                        }

                        if (lobby != null && lobby.HashedPassword == joinLobbyMsg.Password)
                        {
                            if (lobby.Host == User.Name)
                                server_.OpenLobby(lobby.Name, this);
                            else
                                server_.JoinLobby(lobby.Name, this);

                            var reply = JoinLobbyResponseMessage.Create(true, null);
                            User.Writer.WriteMessage(reply);

                            _ = logger_.Log($"User #{User.Id}|{User.Name} has joined lobby {lobby.Name} successfully");
                        }
                        else
                        {
                            string reason = lobby is null ? "Unknown lobby name" : "Wrong password";
                            var reply = JoinLobbyResponseMessage.Create(false, reason);
                            User.Writer.WriteMessage(reply);
                            await logger_.Log($"User #{User.Id}|{User.Name} failed to join lobby {joinLobbyMsg.Name}: {reason}");
                        }                        
                    }
                    break;

                case CommMessage.MessageType.CreateLobbyRequest:
                    if (msg is CreateLobbyRequestServerMessage createLobbyMsg)
                    {
                        bool created = false;
                        try
                        {
                            created = Jsn.RegisterLobby(createLobbyMsg.Name, createLobbyMsg.Password, User.Name);
                        }
                        catch (Exception ex)
                        {
                            var reply = CreateLobbyResponseMessage.Create(false, $"Creation has failed - {ex.Message}");
                            User.Writer.WriteMessage(reply);
                            await logger_.Log($"Lobby {createLobbyMsg.Name} failed to create: {ex.Message}");
                            return;
                        }

                        if (created)
                        {
                            var reply = CreateLobbyResponseMessage.Create(true, null);
                            User.Writer.WriteMessage(reply);
                            await logger_.Log($"Lobby {createLobbyMsg.Name} was created successfully");
                        }
                        else
                        {
                            var reply = CreateLobbyResponseMessage.Create(false, "Something wrong with lobby data or lobby already exists");
                            User.Writer.WriteMessage(reply);
                            await logger_.Log($"Lobby {createLobbyMsg.Name} failed to create: wrong lobby data or lobby already exists - {createLobbyMsg.Text}");
                        }
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
