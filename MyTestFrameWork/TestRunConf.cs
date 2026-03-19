using System;
using System.Reflection;
using System.Text;

namespace MyTestFramework
{
    public class TestRunnerConfiguration
    {
        
        public int MaxDegreeOfParallelism { get; set; } = 10;
        public bool EnableParallelism { get; set; } = true;
        public string? LogFilePath { get; set; }
        public bool Verbose { get; set; } = true;
        public bool MeasureExecutionTime { get; set; } = true;
    }

    internal class TestInfo
    {
        public Type TestClass { get; set; }
        public MethodInfo TestMethod { get; set; }
        public MethodInfo? SetUpMethod { get; set; }
        public MethodInfo? TearDownMethod { get; set; }
        public object[]? Parameters { get; set; }
        public int? TimeoutMs { get; set; }

        public string FullName => $"{TestClass.Name}.{TestMethod.Name}" +
            (Parameters != null ? $"({string.Join(", ", Parameters)})" : "");

        public TestInfo(Type testClass, MethodInfo testMethod, MethodInfo? setUpMethod,
            MethodInfo? tearDownMethod, object[]? parameters = null, int? timeoutMs = null)
        {
            TestClass = testClass;
            TestMethod = testMethod;
            SetUpMethod = setUpMethod;
            TearDownMethod = tearDownMethod;
            Parameters = parameters;
            TimeoutMs = timeoutMs;
        }
    }

    public class TestResult
    {
        public string TestName { get; set; }
        public bool IsSuccess { get; set; }
        public long ElapsedMilliseconds { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime ExecutedAt { get; set; }

        public TestResult(string testName, bool isSuccess, long elapsedMs,
            string? errorMessage = null, DateTime? executedAt = null)
        {
            TestName = testName;
            IsSuccess = isSuccess;
            ElapsedMilliseconds = elapsedMs;
            ErrorMessage = errorMessage;
            ExecutedAt = executedAt ?? DateTime.Now;
        }
    }
}