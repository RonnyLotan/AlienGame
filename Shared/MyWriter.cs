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
        private StreamWriter writer_;
        private string sessionAesKey_;

        public bool EncryptionEstablished = !Global.USE_ENCRYPTION;
        
        public MyWriter(string sessionAesKey, NetworkStream nws)
        {
            sessionAesKey_ = sessionAesKey;
            
            writer_ = new StreamWriter(nws, Encoding.UTF8) { AutoFlush = true };            
        }

        public void WriteMessage(CommMessage message, bool encrypt = true)
        {
            var line = encrypt ? message.EncryptedText(sessionAesKey_) : message.Text;
            writer_.WriteLine(line);
        }

        public void sendAesKey(string rsaPublicKey)
        {
            string encryptedAesKey = Encryption.RsaEncrypt(sessionAesKey_, rsaPublicKey);
            var reply = AesKeyMessage.Create(encryptedAesKey);
            WriteMessage(reply);

            EncryptionEstablished = true;
        }

        public void Close()
        {
            writer_.Close();
        }
    }
}
