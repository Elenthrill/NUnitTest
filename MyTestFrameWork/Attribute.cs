using System;

namespace MyTestFramework
{
    // Атрибут для тестовых методов
    [AttributeUsage(AttributeTargets.Method)]
    public class TestAttribute : Attribute { }

    // Атрибут для инициализации
    [AttributeUsage(AttributeTargets.Method)]
    public class SetUpAttribute : Attribute { }

    // Атрибут для финализации
    [AttributeUsage(AttributeTargets.Method)]
    public class TearDownAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TestCaseAttribute : Attribute
    {
        public object[] Parameters { get; }
        public TestCaseAttribute(params object[] parameters)
        {
            Parameters = parameters;
        }
    }
}