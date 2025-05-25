using Shared;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static string filePath = Logger.LogFilePath;
    static object consoleLock = new();
    static long lastPosition = 0;

    static void Main()
    {
        Console.WriteLine("Watching file. Press Ctrl+R to reload. Press Esc to exit.");

        // Start watching the file
        SetupFileWatcher();

        // Start key listener
        Task.Run(() => ListenForHotkey());

        // Keep the main thread alive
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Escape)
                break;
        }
    }

    static void SetupFileWatcher()
    {
        var watcher = new FileSystemWatcher(Path.GetDirectoryName(filePath));
        watcher.Filter = Path.GetFileName(filePath);
        watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
        watcher.EnableRaisingEvents = true;

        watcher.Changed += OnChanged;
    }

    private static void OnChanged(object source, FileSystemEventArgs e)
    {
        doOnChange();
    }

    private static void doOnChange()
    { 
        try
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                stream.Seek(lastPosition, SeekOrigin.Begin);
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        Console.WriteLine($"[New Line] {line}");
                    }

                    lastPosition = stream.Position;
                }
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"[Error reading file] {ex.Message}");
        }
    }

    static void ListenForHotkey()
    {
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.R && key.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                lock (consoleLock)
                {
                    Console.Clear();
                    Console.WriteLine("[Manual reload with Ctrl+R]");
                    lastPosition = 0;
                    doOnChange();
                }
            }
        }
    }

    static void DisplayFileContent()
    {
        if (File.Exists(filePath))
        {
            string content = File.ReadAllText(filePath);
            Console.WriteLine(content);
        }
        else
        {
            Console.WriteLine("[File not found]");
        }
    }
}