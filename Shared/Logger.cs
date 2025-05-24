using System.IO;
using System.Windows.Forms;

namespace Shared
{
    public class Logger
    {
        public static readonly string LogFilePath = "..\\..\\..\\..\\..\\shared_log.txt";

        private static readonly Mutex _mutex = new Mutex(false, "Global\\MyLoggerMutex");

        private string id_;

        public Logger(string id)
        {
            this.id_ = id;

            if (id == "Server")
                File.Delete(LogFilePath);
        }

        public bool Log(string message)
        {
            _mutex.WaitOne();
            try
            {
                File.AppendAllText(LogFilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [[{id_}]]- {message}{Environment.NewLine}");
                return true;
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }
    }
}
