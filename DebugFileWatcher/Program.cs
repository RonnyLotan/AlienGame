using Shared;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static string filePath = Logger.LogFilePath;
    static object consoleLock = new();

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
        watcher.NotifyFilter = NotifyFilters.LastWrite;
        watcher.EnableRaisingEvents = true;

        watcher.Changed += (s, e) =>
        {
            lock (consoleLock)
            {
                Console.WriteLine($"\n[File updated at {DateTime.Now:T}]");
                DisplayFileContent();
            }
        };

        // Initial display
        lock (consoleLock)
        {
            DisplayFileContent();
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
                    DisplayFileContent();
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