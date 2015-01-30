using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RSG.Promise
{
    public static class LinqExts
    {
        /// <summary>
        /// Return an empty enumerable.
        /// </summary>
        public static IEnumerable<T> Empty<T>()
        {
            return new T[0];
        }

        /// <summary>
        /// Convert a variable length argument list of items to an enumerable.
        /// </summary>
        public static IEnumerable<T> FromItems<T>(params T[] items)
        {
            foreach (var item in items)
            {
                yield return item;
            }
        }

        /// <summary>
        /// 
        /// </summary>
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
