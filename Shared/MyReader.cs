using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class MyReader
    {
        private StreamReader reader_;
        private string sessionAesKey_;

        public MyReader(string sessionAesKey, NetworkStream nws)
        {  
            sessionAesKey_ = sessionAesKey;

            reader_ = new StreamReader(nws, Encoding.UTF8);
        }  

        public bool ReadMessage(out CommMessage? message)
        {
            string? line = reader_.ReadLine();
            if (line is not null)
            {
                message = CommMessage.FromText(line, sessionAesKey_);
                return true;
            }

            message = null;
            return false;
        }

        public void Close()
        {
            reader_.Close();
        }
    }
}
