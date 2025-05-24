using Microsoft.VisualBasic.ApplicationServices;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Server
{
    public class UserData
    {
        public readonly int Id;
        public TcpClient Socket { get; init; }
        private NetworkStream nws_;
        public MyReader Reader { get; init; }
        public MyWriter Writer { get; set; }

        private string? name_ = null;
        public string Name {
            get
            {
                if (name_ == null)
                {
                    throw new InvalidOperationException("Accessing Name before it was initialized");
                }

                return name_;
            }
            set
            {
                name_ = value;
            }
        }
        public string? HashedPassword { get; set; }
        public string? PublicKey { get; set; }

        public bool LoggedIn { get => name_ is not null; }

        public Lobby? Lobby { get; set; }
        public bool InLobby { get => Lobby is not null;  }

        public UserData(TcpClient socket, int id)
        {
            Socket = socket;
            var sessionAesKey = Encryption.GenerateAesKey();

            nws_ = Socket.GetStream();
            Reader = new MyReader(sessionAesKey, nws_);
            Writer = new MyWriter(sessionAesKey, nws_) ;

            HashedPassword = null;
            PublicKey = null;
            Lobby = null;

            Id = id;
        }        

        public override string ToString()
        {
            return $"#{Id}|{name_ ?? ""}";
        }

        public void Close()
        {
            Socket.Close();
            Reader.Close(); 
            Writer.Close();
        }
    }
}
