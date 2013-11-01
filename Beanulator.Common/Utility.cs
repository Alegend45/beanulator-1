namespace Beanulator.Common
{
    using half = System.UInt16;
    using word = System.UInt32;

    public class Utility
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
    }
}