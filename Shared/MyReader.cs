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
        private StreamReader? reader_;
        private string sessionAesKey_;

        public MyReader(string sessionAesKey, NetworkStream nws)
        {  
            sessionAesKey_ = sessionAesKey;

            reader_ = new StreamReader(nws, Encoding.UTF8);
        }

        public MyReader()
        {
            sessionAesKey_ = "";

            reader_ = null;
        }

        public bool ReadMessage(out CommMessage? message)
        {
            try
            {
                if (reader_ is null)
                    throw new Exception($"Calling MyReader before it was initialized");

                string? line = reader_.ReadLine();
                if (line is not null)
                {
                    message = CommMessage.FromText(line, sessionAesKey_);
                    return true;
                }
            }
            catch (Exception ex)
            {
                message = CommunicationErrorMessage.Create(ex.Message);
                return true;
            }

            message = null;
            return false;
        }

        public void Close()
        {
            if (reader_ is not  null)
                reader_.Close();
        }
    }
}
