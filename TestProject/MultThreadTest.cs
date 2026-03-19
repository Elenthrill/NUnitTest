using System;
using System.Threading;
using System.Threading.Tasks;
using MyTestFramework;

namespace TestProject
{
    public class MultTest
    {
        [Test]
        [Timeout(2000)] 
        public void FastTest()
        {
            Thread.Sleep(500);
            Assert.IsTrue(true);
        }

        [Test]
        [Timeout(1000)]
        public void SlowTest()
        {
            Thread.Sleep(1500); //превышен timeout
            Assert.IsTrue(true);
        }

        [Test]
        [Timeout(500)]
        public void VeryFastTest()
        {
            Assert.IsNotNull("test");
        }

        [Test]
        public async Task AsyncTest()
        {
            await Task.Delay(100);
            Assert.IsTrue(true);
        }

        [Test]
        [Timeout(1000)]
        public async Task AsyncWithTimeout()
        {
            await Task.Delay(200);
            Assert.IsEqual(1, 1);
        }
    }

    public class PerformanceTests
    {
        [Test]
        [Timeout(3000)]
        public void PerformanceTest1()
        {
            Thread.Sleep(500);
            Assert.IsTrue(true);
        }

        [Test]
        [Timeout(3000)]
        public void PerformanceTest2()
        {
            Thread.Sleep(600);
            Assert.IsTrue(true);
        }

        [Test]
        [Timeout(3000)]
        public void PerformanceTest3()
        {
            Thread.Sleep(700);
            Assert.IsTrue(true);
        }

        [Test]
        [Timeout(3000)]
        public void PerformanceTest4()
        {
            Thread.Sleep(400);
            Assert.IsTrue(true);
        }
    }
}