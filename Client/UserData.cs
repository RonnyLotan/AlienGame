using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class UserData
    {
        public readonly int Id;
        public TcpClient Socket { get; init; }
        private NetworkStream nws_;
        public MyReader Reader { get; init; }
        public MyWriter Writer { get; init; }

        public Logger Logger { get; init; }

        public bool IsConnected = false;
        public bool IsLoggedIn = false;
        public bool IsInLobby = false;
        public bool IsInGame = false;

        public string? Name { get; set; }

        internal UserData(int id, TcpClient socket, string aesKey, Logger logger)
        {
            Id = id;
            Socket = socket;

            nws_ = Socket.GetStream();
            Reader = new MyReader(aesKey, nws_);
            Writer = new MyWriter(aesKey, nws_); 
            Logger = logger;
        }       
    }
}
