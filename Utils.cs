using System;
using System.Net.Sockets;

namespace AlienGame
{
    class ReadWrite
    {
        static public string? read(TcpClient client)
        {
            var stream = client.GetStream();
            var reader = new StreamReader(stream, Encoding.UTF8);
            
            return reader.ReadLine();
        }

        static public void write(TcpClient client, string response)
        {
            var stream = client.GetStream();
            var writer = new StreamWriter(stream, Encoding.UTF8);
            writer.WriteLine(response);
            writer.AutoFlush = true;
        }
    }


}
