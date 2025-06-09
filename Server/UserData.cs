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
        public TcpClient Client { get; init; }
        private NetworkStream nws_;

        public bool EstablishedEncryption = false;
        public MyReader Reader { get; private set; }
        public MyWriter Writer { get; private set; }

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

        public bool LoggedIn { get => name_ is not null; }

        private Lobby? lobby_;
        public Lobby Lobby {
            get
            {
                if (lobby_ == null)
                {
                    throw new InvalidOperationException("Accessing Name before it was initialized");
                }

                return lobby_;
            }
            set
            {
                lobby_ = value;
            }
        }
        public bool InLobby { get => lobby_ is not null;  }

        public UserData(TcpClient client, int id)
        {
            Client = client;            

            nws_ = Client.GetStream();            

            name_ = null;
            lobby_ = null;

            Id = id;
        }     
        
        public void SetSessionAesKey(string sessionAesKey)
        {
            Reader = new MyReader(sessionAesKey, nws_);
            Writer = new MyWriter(sessionAesKey, nws_);

            EstablishedEncryption = true;
        }

        public void ResetLobby()
        {
            lobby_ = null;
        }

        public void ResetName()
        {
            name_ = null;
        }

        public override string ToString()
        {
            return $"#{Id}|{name_ ?? ""}";
        }

        public void Close()
        {
            Client.Close();
            nws_.Close();
            Reader.Close(); 
            Writer.Close();
        }
    }
}
