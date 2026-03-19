using System;
using System.IO;
using System.Text;

namespace MyTestFramework
{
    
    public class TestLogger : IDisposable
    {
        private readonly object _lock = new object();
        private readonly StreamWriter? _fileWriter;
        private readonly bool _verbose;

        public TestLogger(string? logFilePath = null, bool verbose = true)
        {
            _verbose = verbose;

            if (!string.IsNullOrWhiteSpace(logFilePath))
            {
                try
                {
                    _fileWriter = new StreamWriter(logFilePath, append: true, Encoding.UTF8)
                    {
                        AutoFlush = true
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"!!!  Не удалось открыть файл логов: {ex.Message}");
                }
            }
        }

        public void Log(string message)
        {
            lock (_lock)
            {
                if (_verbose)
                {
                    Console.WriteLine(message);
                }

                _fileWriter?.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");
            }
        }

        public void LogTestResult(string testName, bool success, long elapsedMs, string? errorMessage = null)
        {
            string status = success ? "+ УСПЕХ" : "- ПРОВАЛ";
            string timeInfo = $" ({elapsedMs}ms)";
            string message = $"{testName} – {status}{timeInfo}";

            if (errorMessage != null)
            {
                message += $": {errorMessage}";
            }

            Log(message);
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _fileWriter?.Dispose();
            }
        }
    }
}