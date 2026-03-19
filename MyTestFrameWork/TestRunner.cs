using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyTestFramework
{
    public class TestRunner : IDisposable
    {
        private readonly TestRunnerConfiguration _config;
        private readonly TestLogger _logger;

        public TestRunner(TestRunnerConfiguration? config = null)
        {
            _config = config ?? new TestRunnerConfiguration();
            _logger = new TestLogger(_config.LogFilePath, _config.Verbose);
        }

        public void RunTests()
        {
            RunTests(Assembly.GetCallingAssembly());
        }

        public void RunTests(Assembly assembly)
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.Log("========================================");
            _logger.Log($"Начало тестирования сборки: {assembly.GetName().Name}");
            _logger.Log($"Параллелизм: {(_config.EnableParallelism ? "ДА" : "НЕТ")}");
            if (_config.EnableParallelism)
                _logger.Log($"MaxDegreeOfParallelism: {_config.MaxDegreeOfParallelism}");
            _logger.Log("========================================");

            int totalSuccess = 0, totalFail = 0;
            var allResults = new List<TestResult>();

            // Получаем все типы из сборки
            Type[] types = assembly.GetTypes();

            foreach (Type type in types)
            {
                // Пропускаем типы, которые не являются классами
                if (!type.IsClass || type.IsAbstract)
                    continue;

                // Получаем все методы типа
                MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                       BindingFlags.Instance | BindingFlags.Static);

                // Ищем SetUp и TearDown методы
                MethodInfo? setUpMethod = methods.FirstOrDefault(m => m.GetCustomAttribute<SetUpAttribute>() != null);
                MethodInfo? tearDownMethod = methods.FirstOrDefault(m => m.GetCustomAttribute<TearDownAttribute>() != null);

                // Собираем тестовые методы
                List<MethodInfo> testMethods = methods
                    .Where(m => m.GetCustomAttributes<TestAttribute>().Any() ||
                               m.GetCustomAttributes<TestCaseAttribute>().Any())
                    .ToList();

                if (testMethods.Count == 0)
                    continue;

                _logger.Log($"Найден тестовый класс: {type.Name}");

                int classSuccess = 0, classFail = 0;

                // Подготавливаем список тестов для параллельного запуска
                var testsToRun = new List<TestInfo>();

                foreach (MethodInfo testMethod in testMethods)
                {
                    var testCases = testMethod.GetCustomAttributes<TestCaseAttribute>().ToList();
                    int? timeoutMs = testMethod.GetCustomAttribute<TimeoutAttribute>()?.Milliseconds;

                    if (testCases.Any())
                    {
                        foreach (var testCase in testCases)
                        {
                            testsToRun.Add(new TestInfo(type, testMethod, setUpMethod, tearDownMethod,
                                testCase.Parameters, timeoutMs));
                        }
                    }
                    else
                    {
                        testsToRun.Add(new TestInfo(type, testMethod, setUpMethod, tearDownMethod,
                            null, timeoutMs));
                    }
                }

                // Запускаем тесты параллельно или последовательно
                var classResults = _config.EnableParallelism
                    ? RunTestsParallel(testsToRun)
                    : RunTestsSequential(testsToRun);

                allResults.AddRange(classResults);

                classSuccess = classResults.Count(r => r.IsSuccess);
                classFail = classResults.Count(r => !r.IsSuccess);

                _logger.Log($"Класс {type.Name}: успешно {classSuccess}, провалено {classFail}");
                _logger.Log("");

                totalSuccess += classSuccess;
                totalFail += classFail;
            }

            stopwatch.Stop();

            // Вывод итогов
            PrintSummary(totalSuccess, totalFail, stopwatch.ElapsedMilliseconds, allResults);
        }

        private List<TestResult> RunTestsSequential(List<TestInfo> tests)
        {
            var results = new List<TestResult>();

            foreach (var test in tests)
            {
                var result = ExecuteTest(test);
                results.Add(result);
                _logger.LogTestResult(result.TestName, result.IsSuccess, result.ElapsedMilliseconds,
                    result.ErrorMessage);
            }

            return results;
        }

        private List<TestResult> RunTestsParallel(List<TestInfo> tests)
        {
            var results = new List<TestResult>();
            var resultLock = new object();
            var cts = new CancellationTokenSource();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = _config.MaxDegreeOfParallelism,
                CancellationToken = cts.Token
            };

            try
            {
                Parallel.ForEach(tests, parallelOptions, (test) =>
                {
                    var result = ExecuteTest(test);

                    lock (resultLock)
                    {
                        results.Add(result);
                        _logger.LogTestResult(result.TestName, result.IsSuccess, result.ElapsedMilliseconds,
                            result.ErrorMessage);
                    }
                });
            }
            catch (OperationCanceledException)
            {
                _logger.Log("⚠️  Выполнение тестов отменено");
            }

            return results;
        }

        private TestResult ExecuteTest(TestInfo testInfo)
        {
            var stopwatch = Stopwatch.StartNew();
            object? instance = null;

            try
            {
                // Создаём экземпляр класса, если метод нестатический
                if (!testInfo.TestMethod.IsStatic)
                {
                    try
                    {
                        instance = Activator.CreateInstance(testInfo.TestClass);
                    }
                    catch (MissingMethodException)
                    {
                        stopwatch.Stop();
                        return new TestResult(testInfo.FullName, false, stopwatch.ElapsedMilliseconds,
                            "Невозможно создать экземпляр класса – нет конструктора без параметров");
                    }
                }

                // Вызов SetUp
                testInfo.SetUpMethod?.Invoke(instance, null);

                // Выполнение теста с учётом timeout
                ExecuteTestMethod(testInfo, instance);

                stopwatch.Stop();
                return new TestResult(testInfo.FullName, true, stopwatch.ElapsedMilliseconds);
            }
            catch (TargetInvocationException ex)
            {
                stopwatch.Stop();
                return new TestResult(testInfo.FullName, false, stopwatch.ElapsedMilliseconds,
                    ex.InnerException?.Message ?? ex.Message);
            }
            catch (TimeoutException ex)
            {
                stopwatch.Stop();
                return new TestResult(testInfo.FullName, false, stopwatch.ElapsedMilliseconds,
                    $"TIMEOUT: Тест превысил время выполнения ({testInfo.TimeoutMs}ms). {ex.Message}");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new TestResult(testInfo.FullName, false, stopwatch.ElapsedMilliseconds,
                    $"ОШИБКА ВЫЗОВА: {ex.Message}");
            }
            finally
            {
                // Вызов TearDown
                try
                {
                    testInfo.TearDownMethod?.Invoke(instance, null);
                }
                catch (Exception ex)
                {
                    _logger.Log($"⚠️  Ошибка в TearDown: {ex.InnerException?.Message ?? ex.Message}");
                }
            }
        }

        private void ExecuteTestMethod(TestInfo testInfo, object? instance)
        {
            // Проверяем, есть ли timeout
            if (testInfo.TimeoutMs.HasValue)
            {
                var task = Task.Run(() => InvokeTestMethod(testInfo, instance));
                bool completed = task.Wait(TimeSpan.FromMilliseconds(testInfo.TimeoutMs.Value));

                if (!completed)
                {
                    throw new TimeoutException(
                        $"Тест не завершился за {testInfo.TimeoutMs.Value}ms");
                }

                // Проверяем, не было ли исключений в асинхронной задаче
                if (task.IsFaulted)
                {
                    throw task.Exception ?? new Exception("Неизвестная ошибка в тесте");
                }
            }
            else
            {
                // Выполняем без ограничения по времени
                InvokeTestMethod(testInfo, instance);
            }
        }

        private void InvokeTestMethod(TestInfo testInfo, object? instance)
        {
            // Если метод асинхронный, дожидаемся выполнения
            if (testInfo.TestMethod.ReturnType == typeof(Task) ||
                (testInfo.TestMethod.ReturnType.IsGenericType &&
                 testInfo.TestMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)))
            {
                var task = (Task)testInfo.TestMethod.Invoke(instance, testInfo.Parameters)
                    ?? throw new InvalidOperationException("Асинхронный метод вернул null");
                task.GetAwaiter().GetResult();
            }
            else
            {
                // Синхронный метод
                testInfo.TestMethod.Invoke(instance, testInfo.Parameters);
            }
        }

        private void PrintSummary(int totalSuccess, int totalFail, long totalElapsedMs,
            List<TestResult> allResults)
        {
            _logger.Log("");
            _logger.Log("========================================");
            _logger.Log($"ИТОГО: успешно {totalSuccess}, провалено {totalFail}");
            _logger.Log($"Общее время выполнения: {totalElapsedMs}ms");

            if (allResults.Any())
            {
                long avgTime = (long)allResults.Average(r => r.ElapsedMilliseconds);
                long maxTime = allResults.Max(r => r.ElapsedMilliseconds);
                long minTime = allResults.Min(r => r.ElapsedMilliseconds);

                _logger.Log($"Среднее время теста: {avgTime}ms");
                _logger.Log($"Минимальное время: {minTime}ms");
                _logger.Log($"Максимальное время: {maxTime}ms");
            }

            _logger.Log("========================================");
        }

        public void Dispose()
        {
            _logger?.Dispose();
        }
    }
}