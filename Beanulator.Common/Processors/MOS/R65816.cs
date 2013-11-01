using System.Runtime.InteropServices;

namespace Beanulator.Common.Processors.MOS
{
    public class R65816
    {
        private Bus bus;
        private Registers registers;

        public struct Bus
        {
            public ushort address;
            public byte data;
            public bool read;
        }
        [StructLayout(LayoutKind.Explicit)]
        public struct Registers
        {
            [FieldOffset(0)]
            public byte al;
            [FieldOffset(1)]
            public byte ah;

            [FieldOffset(2)]
            public byte xl;
            [FieldOffset(3)]
            public byte xh;

            [FieldOffset(4)]
            public byte yl;
            [FieldOffset(5)]
            public byte yh;
        }
    }
}