using MyTestFramework;
using TestProject;

using System;
using MyTestFramework;
using TestProject;

class Program
{
    static void Main(string[] args)
    {
        var testRunner = new TestRunner();
        Console.WriteLine("=== Запуск тестов ===");
        testRunner.RunTests();
    }
}