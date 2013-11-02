using System.Runtime.InteropServices;

namespace Beanulator.Common.Processors.SHARP
{
    /// <summary>
    /// Z80-esque CISC processor, SISD ISA
    /// </summary>
    public abstract class LR35902 : Processor
    {
        protected Flags flags;
        protected Interrupts interrupts;
        protected Pins pins;
        protected Registers registers;

        private byte operand(byte code)
        {
            switch ((code >> 0) & 7)
            {
            default:
            case 0: return registers.b;
            case 1: return registers.c;
            case 2: return registers.d;
            case 3: return registers.e;
            case 4: return registers.h;
            case 5: return registers.l;
            case 6: return read(registers.hl);
            case 7: return registers.a;
            }
        }
        private void operand(byte code, byte data)
        {
            switch ((code >> 3) & 7)
            {
            default:
            case 0: registers.b = data; break;
            case 1: registers.c = data; break;
            case 2: registers.d = data; break;
            case 3: registers.e = data; break;
            case 4: registers.h = data; break;
            case 5: registers.l = data; break;
            case 6: write(registers.hl, data); break;
            case 7: registers.a = data; break;
            }
        }

        protected abstract void transfer();

        protected void step()
        {
            byte code = read(registers.pc++);

            if (code == 0xcb) // extended opcode
            {
                if ((code & 0xf8) == 0x00) { op_shl (code, 0); } // [0000 0---] rlc
                if ((code & 0xf8) == 0x08) { op_shr (code, 0); } // [0000 1---] rrc
                if ((code & 0xf8) == 0x10) { op_shl (code, 0); } // [0001 0---] rl
                if ((code & 0xf8) == 0x18) { op_shr (code, 0); } // [0001 1---] rr
                if ((code & 0xf8) == 0x20) { op_shl (code, 0); } // [0010 0---] sla
                if ((code & 0xf8) == 0x28) { op_shr (code, 0); } // [0010 1---] sra
                if ((code & 0xf8) == 0x30) { op_swap(code   ); } // [0011 0---] swap
                if ((code & 0xf8) == 0x38) { op_shr (code, 0); } // [0011 1---] srl
                if ((code & 0xc0) == 0x40) { op_bit (code   ); } // [01-- ----] bit
                if ((code & 0xc0) == 0x80) { op_res (code   ); } // [10-- ----] res
                if ((code & 0xc0) == 0xc0) { op_set (code   ); } // [11-- ----] set
            }
            else // standard opcode
            {
            }

            if (interrupts.poll())
            {
                interrupts.iff1 = false;
                interrupts.iff2 = false;

                // do interrupt processing here
                byte mask = 0x01;

                for (int i = 0; i < 6; i++)
                {
                    if ((interrupts.rf & mask) != 0)
                    {
                        interrupts.rf ^= mask;

                        read(registers.pc); // ?observe 2 wait states
                        read(registers.pc); // ?
                        read(registers.pc); // ?opcode read
                        read(registers.pc); // ?address low byte
                        read(registers.pc); // ?address high byte

                        write(registers.sp, registers.pch); registers.sp--;
                        write(registers.sp, registers.pcl); registers.sp--;

                        registers.pc = 0x0040;
                        registers.pcl |= mask;
                        break;
                    }

                    mask <<= 1;
                }
            }
        }

        protected byte read(ushort address)
        {
            pins.address = address;
            pins.read = true;

            transfer();

            return pins.data;
        }
        protected void write(ushort address, byte data)
        {
            pins.address = address;
            pins.data = data;
            pins.read = false;

            transfer();
        }

        #region standard instruction set

        #endregion
        #region extended instruction set

        private void op_bit(byte code)
        {
            int b = (code >> 3) & 7;
            int m = (1 << b);

            flags.z = (operand(code) & m) == 0 ? 1 : 0;
            flags.n = 0;
            flags.h = 1;
        }
        private void op_res(byte code)
        {
            int b = (code >> 3) & 7;
            int m = (1 << b);

            operand(code, (byte)(operand(code) & ~m));
        }
        private void op_set(byte code)
        {
            int b = (code >> 3) & 7;
            int m = (1 << b);

            operand(code, (byte)(operand(code) | m));
        }
        private void op_shl(byte code, int carry = 0) { throw new System.NotImplementedException(); }
        private void op_shr(byte code, int carry = 0) { throw new System.NotImplementedException(); }
        private void op_swap(byte code) { throw new System.NotImplementedException(); }

        #endregion

        public struct Flags
        {
            public int z; // 0 = not equal, 1 = equal
            public int n; // 0 = addition, 1 = subtraction
            public int h; // 0 = no half carry, 1 = half carry
            public int c; // 0 = no carry, 1 = carry

            public void load(byte value)
            {
                z = (value >> 7) & 1;
                n = (value >> 6) & 1;
                h = (value >> 5) & 1;
                c = (value >> 4) & 1;
            }
            public byte save()
            {
                return (byte)(
                    (z << 7) |
                    (n << 6) |
                    (h << 5) |
                    (c << 4));
            }
        }
        public struct Interrupts
        {
            public bool iff1; // master enable
            public bool iff2; // master enable temporary (only used for nmi, so not used for gb/cgb)
            public byte ef; // enable flags
            public byte rf; // request flags

            public bool poll()
            {
                return iff1 && (ef & rf) != 0;
            }
        }
        public struct Pins
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
            [FieldOffset(0xc)] public byte eal;
            [FieldOffset(0xd)] public byte eah;

            [FieldOffset(0x0)] public ushort af;
            [FieldOffset(0x2)] public ushort bc;
            [FieldOffset(0x4)] public ushort de;
            [FieldOffset(0x6)] public ushort hl;
            [FieldOffset(0x8)] public ushort pc;
            [FieldOffset(0xa)] public ushort sp;
            [FieldOffset(0xc)] public ushort ea;
        }
    }
}