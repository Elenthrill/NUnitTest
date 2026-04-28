using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MyTestFramework;

class Program
{
    static async Task Main(string[] args)
    {
        // 1. Фильтрация: только тесты категории "Add User" и приоритет 1
        Console.WriteLine("=== ФИЛЬТРАЦИЯ: категория 'Add User' или приоритет 1 ===");
        var filterConfig = new TestRunnerConfiguration { EnableParallelism = false, Verbose = true };
        filterConfig.TestFilter = (method, type) =>
        {
            var catAttr = method.GetCustomAttribute<CategoryAttribute>() ?? type.GetCustomAttribute<CategoryAttribute>();
            var priAttr = method.GetCustomAttribute<PriorityAttribute>();
            return (catAttr != null && catAttr.Category == "Add User") || (priAttr != null && priAttr.Priority == 1);
        };
        new TestRunner(null, filterConfig).RunTests();

        // 2. Параметризованные тесты с TestSource
        Console.WriteLine("\n=== PARAMETRIZED TESTS (TestSource + yield return) ===");
        var sourceConfig = new TestRunnerConfiguration { EnableParallelism = false, Verbose = true };
        new TestRunner(null, sourceConfig).RunTests();

        // 3. События пула потоков
        Console.WriteLine("\n=== THREAD POOL EVENTS DEMO ===");
        using var pool = new CustomThreadPool(minThreads: 1, maxThreads: 4, idleTimeout: TimeSpan.FromSeconds(3));
        pool.ThreadCreated += id => Console.WriteLine($"[POOL EVENT] Created thread {id}");
        pool.ThreadDestroyed += id => Console.WriteLine($"[POOL EVENT] Destroyed thread {id}");
        pool.TaskStarted += desc => Console.WriteLine($"[POOL EVENT] Started '{desc}'");
        pool.TaskCompleted += desc => Console.WriteLine($"[POOL EVENT] Completed '{desc}'");

        var poolConfig = new TestRunnerConfiguration
        {
            EnableParallelism = true,
            UseCustomThreadPool = true,
            ExternalThreadPool = pool,
            Verbose = false
        };

        using var countdown = new CountdownEvent(3);
        for (int i = 1; i <= 3; i++)
        {
            int runNum = i;
            pool.QueueUserWorkItem(token =>
            {
                Console.WriteLine($">>> Test run #{runNum} started");
                new TestRunner(null, poolConfig).RunTests();
                Console.WriteLine($">>> Test run #{runNum} completed");
                countdown.Signal();
            }, $"Run #{runNum}");
        }
        countdown.Wait();
        Console.WriteLine("\nAll demonstrations finished.");
    }
}