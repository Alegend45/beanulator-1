using System;

namespace Beanulator.Common
{
    using half = System.UInt16;
    using word = System.UInt32;

    public static class Utility
    {
        public static int BitsSet(byte value)
        {
            int count = 0;

            while (value != 0)
            {
                value &= (byte)(value - 1);
                count++;
            }

            return count;
        }
        public static int BitsSet(half value)
        {
            int count = 0;

            while (value != 0)
            {
                value &= (half)(value - 1);
                count++;
            }

            return count;
        }
        public static int BitsSet(word value)
        {
            int count = 0;

            while (value != 0)
            {
                value &= (word)(value - 1);
                count++;
            }

            return count;
        }

        public static void Initialize<T>(this T[] array)
            where T : new()
        {
            array.Initialize(() => new T());
        }
        public static void Initialize<T>(this T[] array, Func<T> factory)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = factory();
            }
        }
    }
}