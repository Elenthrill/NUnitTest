using System;
using System.Collections;

namespace MyTestFramework
{
    /// <summary>
    /// Атрибут для тестовых методов
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TestAttribute : Attribute { }

    /// <summary>
    /// Атрибут для инициализации
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SetUpAttribute : Attribute { }

    /// <summary>
    /// Атрибут для финализации
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TearDownAttribute : Attribute { }

    /// <summary>
    /// Атрибут для нескольких тестов
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

    // === Новые атрибуты ===

    /// <summary>
    /// Атрибут, указывающий метод-источник данных (yield return) для параметризованного теста.
    /// Метод должен быть статическим и возвращать IEnumerable<object[]>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TestSourceAttribute : Attribute
    {
        public string SourceMethodName { get; }
        public TestSourceAttribute(string sourceMethodName) => SourceMethodName = sourceMethodName;
    }

    /// <summary>
    /// Категория теста (может применяться к методу или классу).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class CategoryAttribute : Attribute
    {
        public string Category { get; }
        public CategoryAttribute(string category) => Category = category;
    }

    /// <summary>
    /// Приоритет теста.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class PriorityAttribute : Attribute
    {
        public int Priority { get; }
        public PriorityAttribute(int priority) => Priority = priority;
    }

    /// <summary>
    /// Автор теста.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class AuthorAttribute : Attribute
    {
        public string Author { get; }
        public AuthorAttribute(string author) => Author = author;
    }
}