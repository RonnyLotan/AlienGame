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

        private string? name_;
        public string Name {
            get
            {
                if (name_ == null)
                {
                    throw new InvalidOperationException("Accessing Name before it was initialized");
                }

                return name_;
            }
            private set
            {
                name_ = value;
            }
        }

        internal UserData(int id, TcpClient socket, NetworkStream nws, string aesKey, Logger logger)
        {
            Id = id;
            Socket = socket;

            logger.Log("Constructing UserData");
            nws_ = nws;

            Reader = new MyReader(aesKey, nws_);
            Writer = new MyWriter(aesKey, nws_);            

            logger.Log($"Constructing UserData - creating Reader and writer with AesKey: {aesKey}");

            Logger = logger;
        }

        public override string ToString()
        {
            return $"#{Id}|{name_ ?? ""}";
        }
    }
}
