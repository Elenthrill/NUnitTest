using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MyTestFramework
{
    public static class CollectionAssert
    {
        
        public static void AreEqual(ICollection expected, ICollection actual)
        {
            if (expected.Count != actual.Count)
            {
                throw new Exception($"Ожидался массив размером {expected.Count}, но получен размер {actual.Count}.");
            }

            var expectedEnumerator = expected.GetEnumerator();
            var actualEnumerator = actual.GetEnumerator();

            int index = 0;
            while (expectedEnumerator.MoveNext() && actualEnumerator.MoveNext())
            {
                if (!expectedEnumerator.Current?.Equals(actualEnumerator.Current) ?? actualEnumerator.Current != null)
                {
                    throw new Exception($"Элемент коллекции отличается на индексе {index}. Ожидалось: {expectedEnumerator.Current}, Получено: {actualEnumerator.Current}.");
                }
                index++;
            }
        }

        
        public static void AllItemsAreUnique(ICollection collection)
        {
            var hashSet = new HashSet<object>();
            foreach (var item in collection)
            {
                if (!hashSet.Add(item))
                {
                    throw new Exception($"Элемент '{item}' встречается более одного раза в коллекции.");
                }
            }
        }

        
        public static void Contains(ICollection collection, object item)
        {
            if (!collection.Cast<object>().Contains(item))
            {
                throw new Exception($"Элемент '{item}' не найден в коллекции.");
            }
        }

        
        public static void IsSubsetOf(ICollection subset, ICollection superset)
        {
            foreach (var item in subset)
            {
                if (!superset.Cast<object>().Contains(item))
                {
                    throw new Exception($"Элемент '{item}', из подмножества {subset.ToString}, не найден в надмножестве {superset.ToString}.");
                }
            }
        }

        
        public static void DoesNotContain(ICollection collection, object item)
        {
            if (collection.Cast<object>().Contains(item))
            {
                throw new Exception($"Элемент '{item}' не должен содержаться в коллекции.");
            }
        }
    }
}