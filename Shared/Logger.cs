using System.IO;
using System.Windows.Forms;

namespace Shared
{
    public class Logger
    {
        private static readonly string _logFilePath = "..\\..\\..\\..\\..\\shared_log.txt";

        private static readonly Mutex _mutex = new Mutex(false, "Global\\MyLoggerMutex");

        private string id_;

        public Logger(string id)
        {
            this.id_ = id;
        }

        public async Task Log(string message)
        {
            _mutex.WaitOne();
            try
            {
                await File.AppendAllTextAsync(_logFilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [[{id_}]]- {message}{Environment.NewLine}");
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }
    }
}
