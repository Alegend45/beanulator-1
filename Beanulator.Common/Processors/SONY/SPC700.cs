using System.Runtime.InteropServices;

namespace Beanulator.Common.Processors.SONY
{
    public class Spc700
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
            [FieldOffset(0x0)] public byte a;
            [FieldOffset(0x1)] public byte y;
            [FieldOffset(0x2)] public byte x;

            [FieldOffset(0x4)] public byte pcl;
            [FieldOffset(0x5)] public byte pch;
            [FieldOffset(0x6)] public byte spl;
            [FieldOffset(0x7)] public byte sph;
            [FieldOffset(0x8)] public byte eal;
            [FieldOffset(0x8)] public byte eah;
            [FieldOffset(0x8)] public byte idl;
            [FieldOffset(0x8)] public byte idh;

            [FieldOffset(0x0)] public ushort ya;
            [FieldOffset(0x4)] public ushort pc;
            [FieldOffset(0x6)] public ushort sp;
            [FieldOffset(0x8)] public ushort ea;
            [FieldOffset(0xa)] public ushort id;
        }
    }
}
