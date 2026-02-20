using MyTestFramework;
using System;
using System.Threading.Tasks;

namespace TestProject
{
    public class AsyncTest
    {
        [SetUp]
        public void Init()
        {
            Console.WriteLine("  > Инициализация перед тестом...");
        }

        [TearDown]
        public void Cleanup()
        {
            Console.WriteLine("  > Очистка после теста...");
        }

        [Test]
        public void TestSyncMethod()
        {
            Assert.IsEqual(4, 2 + 2);
        }

        [Test]
        public async Task TestAsyncMethod()
        {
            await Task.Delay(100); // Имитация какой-либо асинхронной обработки
            Assert.IsEqual(5, 5);
        }

        [Test]
        public async Task TestAsyncWithFailure()
        {
            await Task.Delay(100); // Имитация задержки выполнения
            Assert.IsEqual(5, 4); // Этот тест упадёт
        }

        [Test]
        public async Task TestLongOperation()
        {
            await Task.Delay(5000); // Имитация очень долгой асинхронной задачи
            Assert.IsEqual(10, 5 + 5);
        }
    }
}