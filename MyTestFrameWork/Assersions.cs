using System;

namespace MyTestFramework
{
    public static class Assert
    {
        public static void IsEqual(object expected, object actual)
        {
            if (!object.Equals(expected, actual))
            {
                throw new Exception($"Ожидаемое значение: {expected}, получено: {actual}");
            }
        }

        public static void IsNotEqual(object expected, object actual)
        {
            if (object.Equals(expected, actual))
            {
                throw new Exception($"Ожидались разные значения");
            }
        }

        public static void IsTrue(bool condition)
        {
            if (!condition)
            {
                throw new Exception("Ожидалось истинное значение (true).");
            }
        }

        public static void IsFalse(bool condition)
        {
            if (condition)
            {
                throw new Exception("Ожидалось ложное значение (false).");
            }
        }

        public static void IsNull(object value)
        {
            if (value != null)
            {
                throw new Exception($"Ожидалось значение null, получено {value}");
            }
        }
        public static void IsNotNull(object value)
        {
            if (value == null)
            {
                throw new Exception($"Ожидалось значение не null, получено {value}");
            }
        }

        public static void IsLinkSame(object expected, object actual)
        {
            if (!ReferenceEquals(expected, actual))
            {
                throw new Exception($"Ожидалось, что ссылки будут указывать на один и тот же объект. Ожидалось: {expected}, Получено: {actual}");
            }
        }

        public static void IsLinkNotSame(object expected, object actual)
        {
            if (!ReferenceEquals(expected, actual))
            {
                throw new Exception($"Ожидалось, что ссылки будут указывать на разные обьекты");
            }

        }

        public static void InRange<T>(T actual, T min, T max) where T : IComparable<T>
        {
            if(actual.CompareTo(min) < 0 || actual.CompareTo(max) > 0)
            {
                throw new Exception($"Ожидалось значение в диапазоне [{min}, {max}]. Получено: {actual}.");
            }
        }
        public static void OutRange<T>(T actual, T min, T max) where T : IComparable<T>
        {
            if (actual.CompareTo(min) > 0 || actual.CompareTo(max) < 0)
            {
                throw new Exception($"Ожидалось значение вне диапазона [{min}, {max}]. Получено: {actual}.");
            }
        }
    }

}