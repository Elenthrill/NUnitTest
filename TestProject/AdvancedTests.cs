using System.Collections.Generic;
using System.Threading;
using MyTestFramework;

namespace TestProject
{
    [Category("Math")]
    public class AdvancedTests
    {
        // Источник данных с использованием yield return
        public static IEnumerable<object[]> AdditionCases()
        {
            yield return new object[] { 1, 1, 2 };
            yield return new object[] { 2, 3, 5 };
            yield return new object[] { 5, 5, 10 };
        }

        [TestSource(nameof(AdditionCases))]
        [Priority(1)]
        public void TestAddition(int a, int b, int expected)
        {
            Assert.IsEqual(expected, a + b);
        }

        [Test]
        [Category("Fast")]
        [Priority(2)]
        public void FastTest()
        {
            Assert.IsTrue(true);
        }

        [Test]
        [Category("Slow")]
        [Priority(1)]
        public void SlowTest()
        {
            Thread.Sleep(100);
            Assert.IsTrue(true);
        }

        [Test]
        [Author("John Doe")]
        public void ByJohn()
        {
            Assert.IsNotNull(this);
        }
    }
}