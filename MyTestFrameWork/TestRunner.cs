using System;
using System.Collections;
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

        public TestRunner(int? maxThread, TestRunnerConfiguration? config = null, bool ThreadAavaileble = false)
        {
            _config = config ?? new TestRunnerConfiguration();
            if (maxThread != null)
                _config.MaxDegreeOfParallelism = (int)maxThread;
            _config.EnableParallelism = ThreadAavaileble;

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
            {
                if (_config.UseCustomThreadPool)
                    _logger.Log($"Используется кастомный пул потоков (min={_config.MinThreads}, max={_config.MaxThreads})");
                else
                    _logger.Log($"MaxDegreeOfParallelism: {_config.MaxDegreeOfParallelism}");
            }
            _logger.Log("========================================");

            int totalSuccess = 0, totalFail = 0;
            var allResults = new List<TestResult>();

            Type[] types = assembly.GetTypes();

            foreach (Type type in types)
            {
                if (!type.IsClass || type.IsAbstract)
                    continue;

                MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                       BindingFlags.Instance | BindingFlags.Static);

                MethodInfo? setUpMethod = methods.FirstOrDefault(m => m.GetCustomAttribute<SetUpAttribute>() != null);
                MethodInfo? tearDownMethod = methods.FirstOrDefault(m => m.GetCustomAttribute<TearDownAttribute>() != null);

                List<MethodInfo> testMethods = methods
                    .Where(m => m.GetCustomAttributes<TestAttribute>().Any() ||
                               m.GetCustomAttributes<TestCaseAttribute>().Any() ||
                               m.GetCustomAttribute<TestSourceAttribute>() != null)
                    .ToList();

                if (testMethods.Count == 0)
                    continue;

                _logger.Log($"Найден тестовый класс: {type.Name}");

                int classSuccess = 0, classFail = 0;
                var testsToRun = new List<TestInfo>();

                foreach (MethodInfo testMethod in testMethods)
                {
                    // Фильтрация тестов
                    if (!ShouldRunTest(testMethod, type))
                        continue;

                    int? timeoutMs = testMethod.GetCustomAttribute<TimeoutAttribute>()?.Milliseconds;

                    // Обработка TestSource (имеет приоритет, отдельная ветка)
                    var testSourceAttr = testMethod.GetCustomAttribute<TestSourceAttribute>();
                    if (testSourceAttr != null)
                    {
                        var sourceMethod = type.GetMethod(testSourceAttr.SourceMethodName,
                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        if (sourceMethod == null)
                        {
                            _logger.Log($"! Ошибка: метод-источник '{testSourceAttr.SourceMethodName}' не найден в {type.Name}");
                            continue;
                        }

                        var sourceData = sourceMethod.Invoke(null, null) as IEnumerable;
                        if (sourceData == null)
                        {
                            _logger.Log($"! Ошибка: метод '{testSourceAttr.SourceMethodName}' должен возвращать IEnumerable<object[]>");
                            continue;
                        }

                        foreach (var item in sourceData)
                        {
                            if (item is object[] parameters)
                            {
                                testsToRun.Add(new TestInfo(type, testMethod, setUpMethod, tearDownMethod, parameters, timeoutMs));
                            }
                            else
                            {
                                _logger.Log($"! Ошибка: элемент источника должен быть object[], получено {item?.GetType()}");
                            }
                        }
                        continue; // переходим к следующему методу
                    }

                    // Обработка TestCase
                    var testCases = testMethod.GetCustomAttributes<TestCaseAttribute>().ToList();
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
                        // Обычный тест
                        testsToRun.Add(new TestInfo(type, testMethod, setUpMethod, tearDownMethod,
                            null, timeoutMs));
                    }
                }

                List<TestResult> classResults;
                if (!_config.EnableParallelism)
                {
                    classResults = RunTestsSequential(testsToRun);
                }
                else if (_config.UseCustomThreadPool)
                {
                    classResults = RunTestsWithCustomPool(testsToRun);
                }
                else
                {
                    classResults = RunTestsParallel(testsToRun);
                }

                allResults.AddRange(classResults);
                classSuccess = classResults.Count(r => r.IsSuccess);
                classFail = classResults.Count(r => !r.IsSuccess);
                _logger.Log($"Класс {type.Name}: успешно {classSuccess}, провалено {classFail}");
                _logger.Log("");
                totalSuccess += classSuccess;
                totalFail += classFail;
            }

            stopwatch.Stop();
            PrintSummary(totalSuccess, totalFail, stopwatch.ElapsedMilliseconds, allResults);
        }

        private bool ShouldRunTest(MethodInfo method, Type testClass)
        {
            if (_config.TestFilter == null) return true;
            return _config.TestFilter(method, testClass);
        }

        // Остальные методы (RunTestsSequential, RunTestsParallel, RunTestsWithCustomPool,
        // ExecuteTest, ExecuteTestMethod, InvokeTestMethod, PrintSummary) остаются без изменений.
        // ... (приведены для полноты, но в ответе можно сократить, указав, что они не менялись)
        // Здесь я приведу их все для целостности файла.

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
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = _config.MaxDegreeOfParallelism,
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

        private List<TestResult> RunTestsWithCustomPool(List<TestInfo> tests)
        {
            var results = new List<TestResult>();
            var resultLock = new object();
            using var countdown = new CountdownEvent(tests.Count);
            bool ownPool = _config.ExternalThreadPool == null;
            CustomThreadPool pool = ownPool
                ? new CustomThreadPool(
                    minThreads: _config.MinThreads,
                    maxThreads: _config.MaxThreads,
                    idleTimeout: _config.IdleThreadTimeout,
                    queueWaitThreshold: _config.QueueWaitThreshold)
                : _config.ExternalThreadPool;
            try
            {
                foreach (var test in tests)
                {
                    var testCopy = test;
                    pool.QueueUserWorkItem(token =>
                    {
                        if (token.IsCancellationRequested) return;
                        var result = ExecuteTest(testCopy);
                        lock (resultLock)
                        {
                            results.Add(result);
                            _logger.LogTestResult(result.TestName, result.IsSuccess, result.ElapsedMilliseconds,
                                result.ErrorMessage);
                        }
                        countdown.Signal();
                    }, testCopy.FullName);
                }
                countdown.Wait();
            }
            finally
            {
                if (ownPool) pool.Dispose();
            }
            return results;
        }

        private TestResult ExecuteTest(TestInfo testInfo)
        {
            var stopwatch = Stopwatch.StartNew();
            object? instance = null;
            try
            {
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
                testInfo.SetUpMethod?.Invoke(instance, null);
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
                try
                {
                    testInfo.TearDownMethod?.Invoke(instance, null);
                }
                catch (Exception ex)
                {
                    _logger.Log($"!  Ошибка в TearDown: {ex.InnerException?.Message ?? ex.Message}");
                }
            }
        }

        private void ExecuteTestMethod(TestInfo testInfo, object? instance)
        {
            if (testInfo.TimeoutMs.HasValue)
            {
                var task = Task.Run(() => InvokeTestMethod(testInfo, instance));
                bool completed = task.Wait(TimeSpan.FromMilliseconds(testInfo.TimeoutMs.Value));
                if (!completed)
                    throw new TimeoutException($"Тест не завершился за {testInfo.TimeoutMs.Value}ms");
                if (task.IsFaulted)
                    throw task.Exception ?? new Exception("Неизвестная ошибка в тесте");
            }
            else
            {
                InvokeTestMethod(testInfo, instance);
            }
        }

        private void InvokeTestMethod(TestInfo testInfo, object? instance)
        {
            if (testInfo.TestMethod.ReturnType == typeof(Task) ||
                (testInfo.TestMethod.ReturnType.IsGenericType &&
                 testInfo.TestMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)))
            {
                var task = (Task)testInfo.TestMethod.Invoke(instance, testInfo.Parameters)
                    ?? throw new InvalidOperationException("Асинхронный метод вернул null");
                task.Wait();
            }
            else
            {
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