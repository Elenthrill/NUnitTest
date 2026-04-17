using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MyTestFramework;

class Program
{
    private const int TotalRuns = 50;
    private static readonly object ConsoleLock = new object();

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Демонстрация динамического пула потоков ===");
        Console.WriteLine($"Будет выполнено {TotalRuns} параллельных прогонов тестов.\n");

        // Создаём ОДИН пул на всё приложение
        using var pool = new CustomThreadPool(
            minThreads: 2,
            maxThreads: 10,
            idleTimeout: TimeSpan.FromSeconds(3),
            queueWaitThreshold: TimeSpan.FromMilliseconds(300)
        );

        // Подписываемся на события логирования пула
        pool.LogMessage += message => LogToConsole(message, ConsoleColor.Gray);
        pool.LogError += error => LogToConsole(error, ConsoleColor.Red);
        pool.StateChanged += state =>
        {
            LogToConsole($"[СОСТОЯНИЕ] Потоков: {state.ActiveThreads}/{state.MaxThreads} (мин={state.MinThreads}), " +
                         $"Очередь: {state.QueueSize}, Выполнено: {state.TasksProcessed}, " +
                         $"Создано/Уничтожено: {state.ThreadsCreated}/{state.ThreadsDestroyed}", ConsoleColor.Yellow);
        };

        // Конфигурация для TestRunner: используем внешний пул, тесты внутри прогона выполняются последовательно
        var config = new TestRunnerConfiguration
        {
            EnableParallelism = true,          // включает использование пула
            UseCustomThreadPool = true,
            ExternalThreadPool = pool,         // передаём общий пул
            Verbose = false,                   // отключаем детальное логирование тестов, чтобы не засорять консоль
            LogFilePath = null
        };

        using var countdown = new CountdownEvent(TotalRuns);
        var overallStopwatch = Stopwatch.StartNew();

        Console.WriteLine("Добавление задач в пул...\n");

        for (int i = 1; i <= TotalRuns; i++)
        {
            int runNumber = i;

            // Имитация неравномерной подачи задач
            int delayMs = CalculateDelay(runNumber);
            if (delayMs > 0)
                await Task.Delay(delayMs);

            pool.QueueUserWorkItem(token =>
            {
                try
                {
                    LogToConsole($"  >>> Прогон #{runNumber} начат", ConsoleColor.Green);

                    using (var runner = new TestRunner(maxThread: null, config: config))
                    {
                        runner.RunTests();
                    }

                    LogToConsole($"  >>> Прогон #{runNumber} завершён", ConsoleColor.Green);
                }
                catch (Exception ex)
                {
                    LogToConsole($"  >>> Прогон #{runNumber} ошибка: {ex.Message}", ConsoleColor.Red);
                }
                finally
                {
                    countdown.Signal();
                }
            }, $"Прогон тестов #{runNumber}");
        }

        Console.WriteLine("\nВсе задачи добавлены. Ожидание завершения...\n");
        countdown.Wait();
        overallStopwatch.Stop();

        Console.WriteLine("\n========================================");
        Console.WriteLine($"Все {TotalRuns} прогонов завершены.");
        Console.WriteLine($"Общее время программы: {overallStopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine("========================================");
    }

    private static int CalculateDelay(int runNumber)
    {
        if (runNumber <= 10) return 50;
        else if (runNumber <= 20) return 200;
        else if (runNumber <= 25) return 4000;
        else if (runNumber <= 35) return 30;
        else if (runNumber <= 45) return 500;
        else return 1000;
        //return 0;
    }

    private static void LogToConsole(string message, ConsoleColor color)
    {
        lock (ConsoleLock)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
            Console.ForegroundColor = prev;
        }
    }
}