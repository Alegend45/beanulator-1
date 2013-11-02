using System.Runtime.InteropServices;

namespace Beanulator.Common.Processors.SHARP
{
    /// <summary>
    /// Z80-esque CISC processor
    /// </summary>
    public abstract class LR35902 : Processor
    {
        private Bus bus;
        private Registers registers;

        protected abstract void transfer();

        protected override void main()
        {
            byte code;

            while (true)
            {
                code = read(registers.pc++);
            }
        }

        protected byte read(ushort address)
        {
            bus.address = address;
            bus.read = true;

            transfer();

            return bus.data;
        }
        protected void write(ushort address, byte data)
        {
            bus.address = address;
            bus.data = data;
            bus.read = false;

            transfer();
        }

        #region standard instruction set

        #endregion
        #region extended instruction set

        #endregion

        public struct Bus
        {
            public ushort address;
            public byte data;
            public bool read;
        }
        [StructLayout(LayoutKind.Explicit)]
        public struct Registers
        {
            [FieldOffset(0x1)] public byte a;
            [FieldOffset(0x0)] public byte f;
            [FieldOffset(0x3)] public byte b;
            [FieldOffset(0x2)] public byte c;
            [FieldOffset(0x5)] public byte d;
            [FieldOffset(0x4)] public byte e;
            [FieldOffset(0x7)] public byte h;
            [FieldOffset(0x6)] public byte l;
            [FieldOffset(0x8)] public byte pcl;
            [FieldOffset(0x9)] public byte pch;
            [FieldOffset(0xa)] public byte spl;
            [FieldOffset(0xb)] public byte sph;

            [FieldOffset(0x0)] public ushort af;
            [FieldOffset(0x2)] public ushort bc;
            [FieldOffset(0x4)] public ushort de;
            [FieldOffset(0x6)] public ushort hl;
            [FieldOffset(0x8)] public ushort pc;
            [FieldOffset(0xa)] public ushort sp;
        }
    }
}