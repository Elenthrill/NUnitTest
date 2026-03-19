using System;

namespace MyTestFramework
{
    /// <summary>
    /// Атрибут для тестовых методов
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TestAttribute : Attribute { }

    /// <summary>
    /// Атрибут для инициализации
    ///  <summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SetUpAttribute : Attribute { }

    /// <summary>
    /// Атрибут для финализации
    /// <summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TearDownAttribute : Attribute { }

    /// <summary>
    /// атрибут для нескольких тестов
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TestCaseAttribute : Attribute
    {
        public object[] Parameters { get; }
        public TestCaseAttribute(params object[] parameters)
        {
            Parameters = parameters;
        }
    }

    /// <summary>
    /// Атрибут для ограничения времени
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TimeoutAttribute : Attribute
    {
        public int Milliseconds { get; }

        public TimeoutAttribute(int milliseconds)
        {
            if (milliseconds <= 0)
                throw new ArgumentException("Timeout должен быть больше 0", nameof(milliseconds));
            Milliseconds = milliseconds;
        }
    }
}