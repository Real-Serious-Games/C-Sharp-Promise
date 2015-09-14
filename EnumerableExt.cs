using System;
using System.Collections.Generic;

namespace RSG.Promises
{
    /// <summary>
    /// General extensions to LINQ.
    /// </summary>
    public static class EnumerableExt
    {
        public static IEnumerable<T> Empty<T>()
        {
            return new T[0];
        }

        public static IEnumerable<T> LazyEach<T>(this IEnumerable<T> source, Action<T> fn)
        {
            foreach (var item in source)
            {
                fn.Invoke(item);

                yield return item;
            }
        }

        public static void Each<T>(this IEnumerable<T> source, Action<T> fn)
        {
            foreach (var item in source)
            {
                fn.Invoke(item);
            }
        }

        public static void Each<T>(this IEnumerable<T> source, Action<T, int> fn)
        {
            int index = 0;

            foreach (T item in source)
            {
                fn.Invoke(item, index);
                index++;
            }
        }
    }
}
