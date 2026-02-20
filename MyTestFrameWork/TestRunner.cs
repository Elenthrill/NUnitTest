using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MyTestFramework
{
    public class TestRunner
    {
        public void RunTests()
        {
            RunTests(Assembly.GetCallingAssembly());
        }

        public void RunTests(Assembly assembly)
        {
            int totalSuccess = 0, totalFail = 0;

            // Получаем все типы из сборки
            Type[] types = assembly.GetTypes();

            foreach (Type type in types)
            {
                // Пропускаем типы, которые не являются классами (интерфейсы, перечисления и т.д.)
                if (!type.IsClass || type.IsAbstract)
                    continue;

                // Получаем все методы типа (public, non-public, instance, static)
                MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                        BindingFlags.Instance | BindingFlags.Static);

                // Ищем единственный SetUp и TearDown для этого класса (если есть)
                MethodInfo setUpMethod = methods.FirstOrDefault(m => m.GetCustomAttribute<SetUpAttribute>() != null);
                MethodInfo tearDownMethod = methods.FirstOrDefault(m => m.GetCustomAttribute<TearDownAttribute>() != null);

                // Собираем тестовые методы (помеченные Test или TestCase)
                List<MethodInfo> testMethods = methods
                    .Where(m => m.GetCustomAttributes<TestAttribute>().Any() ||
                                m.GetCustomAttributes<TestCaseAttribute>().Any())
                    .ToList();

                if (testMethods.Count == 0)
                    continue; // в текущем классе нет тестов – переходим к следующему

                Console.WriteLine($"Найден тестовый класс: {type.Name}");

                int classSuccess = 0, classFail = 0;

                // Запускаем каждый тестовый метод класса
                foreach (MethodInfo testMethod in testMethods)
                {
                    (int success, int fail) = RunTestMethodWithLifecycle(type, testMethod, setUpMethod, tearDownMethod).GetAwaiter().GetResult();
                    classSuccess += success;
                    classFail += fail;
                }

                // Выводим итоги по классу
                Console.WriteLine($"Класс {type.Name}: успешно {classSuccess}, провалено {classFail}");
                Console.WriteLine();

                totalSuccess += classSuccess;
                totalFail += classFail;
            }

            // Общие итоги
            Console.WriteLine("========================================");
            Console.WriteLine($"ИТОГО по сборке: успешно {totalSuccess}, провалено {totalFail}");
        }

        private async Task<(int success, int fail)> RunTestMethodWithLifecycle(Type testClass, MethodInfo testMethod,
                                                                              MethodInfo? setUpMethod, MethodInfo? tearDownMethod)
        {
            int successCount = 0, failCount = 0;

            // Получаем все атрибуты TestCase для метода (если есть)
            var testCases = testMethod.GetCustomAttributes<TestCaseAttribute>().ToList();

            if (testCases.Any())
            {
                // Параметризованный тест – запускаем для каждого набора параметров
                foreach (var testCase in testCases)
                {
                    (int success, int fail) = await ExecuteTestWithLifecycle(testClass, testMethod, setUpMethod,
                                                                            tearDownMethod, testCase.Parameters);
                    successCount += success;
                    failCount += fail;
                }
            }
            else
            {
                // Обычный тест без параметров
                (int success, int fail) = await ExecuteTestWithLifecycle(testClass, testMethod, setUpMethod, tearDownMethod, null);
                successCount += success;
                failCount += fail;
            }

            return (successCount, failCount);
        }

        private async Task<(int success, int fail)> ExecuteTestWithLifecycle(Type testClass, MethodInfo testMethod,
                                                                             MethodInfo? setUpMethod, MethodInfo? tearDownMethod,
                                                                             object[]? parameters)
        {
            object? instance = null;
            int successCount = 0, failCount = 0;

            // Создаём экземпляр класса, если метод нестатический и есть конструктор по умолчанию
            if (!testMethod.IsStatic)
            {
                try
                {
                    instance = Activator.CreateInstance(testClass);
                }
                catch (MissingMethodException)
                {
                    Console.WriteLine($"Невозможно создать экземпляр класса {testClass.Name} – нет конструктора без параметров. Тест {testMethod.Name} пропущен.");
                    failCount++;
                    return (successCount, failCount);
                }
            }

            try
            {
                // Вызов SetUp (если есть)
                setUpMethod?.Invoke(instance, null);

                string paramInfo = parameters != null ? $"{string.Join(", ", parameters)}" : "без параметров";
                Console.WriteLine($"  Запуск теста: {testClass.Name}.{testMethod.Name} ({paramInfo})");

                // Если метод асинхронный, дожидаемся выполнения
                if (testMethod.ReturnType == typeof(Task) || (testMethod.ReturnType.IsGenericType &&
                                                              testMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)))
                {
                    var task = (Task)testMethod.Invoke(instance, parameters);
                    await task.ConfigureAwait(false); // Дожидаемся завершения задачи
                }
                else
                {
                    // Син��ронный метод
                    testMethod.Invoke(instance, parameters);
                }

                // Если дошли до сюда – тест успешен
                successCount++;
                Console.WriteLine($"  ✅ {testClass.Name}.{testMethod.Name} – УСПЕХ");
            }
            catch (TargetInvocationException ex)
            {
                // Внутреннее исключение, выброшенное самим тестом
                failCount++;
                Console.WriteLine($"  ❌ {testClass.Name}.{testMethod.Name} – ПРОВАЛ: {ex.InnerException?.Message}");
            }
            catch (Exception ex)
            {
                // Другие ошибки (например, неверные параметры)
                failCount++;
                Console.WriteLine($"  ❌ {testClass.Name}.{testMethod.Name} – ОШИБКА ВЫЗОВА: {ex.Message}");
            }
            finally
            {
                // Вызов TearDown (если есть) на том же экземпляре
                if (tearDownMethod != null)
                {
                    try
                    {
                        tearDownMethod.Invoke(instance, null);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  ⚠️ Ошибка в TearDown метода {testClass.Name}.{tearDownMethod.Name}: {ex.InnerException?.Message}");
                    }
                }
            }

            return (successCount, failCount);
        }
    }
}