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
            [FieldOffset(0x0)] public byte al;
            [FieldOffset(0x1)] public byte ah;
            [FieldOffset(0x2)] public byte xl;
            [FieldOffset(0x3)] public byte xh;
            [FieldOffset(0x4)] public byte yl;
            [FieldOffset(0x5)] public byte yh;
            [FieldOffset(0x6)] public byte pcl;
            [FieldOffset(0x7)] public byte pch;
            [FieldOffset(0x8)] public byte spl;
            [FieldOffset(0x9)] public byte sph;
            [FieldOffset(0xa)] public byte eal;
            [FieldOffset(0xb)] public byte eah;
            [FieldOffset(0xc)] public byte idl;
            [FieldOffset(0xd)] public byte idh;

            [FieldOffset(0x0)] public ushort a;    // accumulator
            [FieldOffset(0x2)] public ushort x;    // x-index register
            [FieldOffset(0x4)] public ushort y;    // y-index register
            [FieldOffset(0x6)] public ushort pc;   // program cursor
            [FieldOffset(0x8)] public ushort sp;   // stack pointer
            [FieldOffset(0xa)] public ushort ea;   // effective address temporary register
            [FieldOffset(0xc)] public ushort id;   // indirect address temporary register
        }
    }
}