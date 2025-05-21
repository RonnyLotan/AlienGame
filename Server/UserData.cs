using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class UserData
    {
        public readonly int Id;
        public TcpClient Socket { get; init; }
        public MyReader Reader { get; init; }
        public MyWriter Writer { get; set; }

        public string? Name { get; set; }
        public string? HashedPassword { get; set; }
        public string? PublicKey { get; set; }

        public bool LoggedIn { get => Name is not null; }

        public Lobby? Lobby { get; set; }
        public bool InLobby { get => Lobby is not null;  }

        public UserData(TcpClient socket, int id)
        {
            Socket = socket;
            var sessionAesKey = Encryption.GenerateAesKey();

            var nws = Socket.GetStream();
            Reader = new MyReader(sessionAesKey, nws);
            Writer = new MyWriter(sessionAesKey, nws) ;

            Id = id;
        }        

        public override string ToString()
        {
            return $"#{Id}|{Name}";
        }

        public void Close()
        {
            Socket.Close();
            Reader.Close(); 
            Writer.Close();
        }
    }
}
