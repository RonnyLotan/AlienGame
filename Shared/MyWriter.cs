using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class MyWriter
    {
        private StreamWriter? writer_;
        private string sessionAesKey_;
        public string SessionAesKey {  get { return sessionAesKey_; } }

        public MyWriter(string sessionAesKey, NetworkStream nws)
        {
            sessionAesKey_ = sessionAesKey;
            
            writer_ = new StreamWriter(nws, Encoding.UTF8) { AutoFlush = true };            
        }

        public MyWriter()
        {
            sessionAesKey_ = "";
            writer_ = null; 
        }

        public void WriteMessage(CommMessage message, bool encrypt = true)
        {
            if (writer_ is null)
                throw new Exception("MyWriter was called before it was initialized");

            var line = encrypt ? message.EncryptedText(sessionAesKey_) : message.Text;
            writer_.WriteLine(line);
        }

        public void Close()
        {
            if (writer_ is not null)
                writer_.Close();
        }
    }
}
