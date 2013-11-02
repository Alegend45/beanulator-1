using System;

namespace Beanulator.Common.Processors.ARM
{
    public class ARM7
    {
        private Mode abt = new Mode(2);
        private Mode fiq = new Mode(7);
        private Mode irq = new Mode(2);
        private Mode svc = new Mode(2);
        private Mode und = new Mode(2);
        private Mode usr = new Mode(7);

        #region Active State

        private Flags cpsr;
        private Flags spsr;
        private Register[] registers;

        #endregion

        public ARM7()
        {
            cpsr = new Flags();
            spsr = null;
            registers = new Register[16];
            registers.Initialize<Register>();
        }

        private void ChangeMode(uint mode)
        {
            var bank = (mode == Mode.FIQ) ? fiq : usr;

            registers[ 8] = bank.registers[2];
            registers[ 9] = bank.registers[3];
            registers[10] = bank.registers[4];
            registers[11] = bank.registers[5];
            registers[12] = bank.registers[6];

            switch (mode)
            {
            case Mode.USR: registers[13] = usr.registers[0]; registers[14] = usr.registers[1]; spsr = null; break;
            case Mode.FIQ: registers[13] = fiq.registers[0]; registers[14] = fiq.registers[1]; spsr = fiq.spsr; break;
            case Mode.IRQ: registers[13] = irq.registers[0]; registers[14] = irq.registers[1]; spsr = irq.spsr; break;
            case Mode.SVC: registers[13] = svc.registers[0]; registers[14] = svc.registers[1]; spsr = svc.spsr; break;
            case Mode.ABT: registers[13] = abt.registers[0]; registers[14] = abt.registers[1]; spsr = abt.spsr; break;
            case Mode.UND: registers[13] = und.registers[0]; registers[14] = und.registers[1]; spsr = und.spsr; break;
            case Mode.SYS: registers[13] = usr.registers[0]; registers[14] = usr.registers[1]; spsr = null; break;
            }
        }

        public class Flags
        {
            public uint n, z, c, v;
            public uint r;
            public uint i, f, t, m;

            public void load(uint value)
            {
                n = (value >> 31) & 1;
                z = (value >> 30) & 1;
                c = (value >> 29) & 1;
                v = (value >> 28) & 1;
                r = (value >>  8) & 0xfffff;
                i = (value >>  7) & 1;
                f = (value >>  6) & 1;
                t = (value >>  5) & 1;
                m = (value >>  0) & 31;
            }
            public uint save()
            {
                return
                    (n << 31) |
                    (z << 30) |
                    (c << 29) |
                    (v << 28) |
                    (r <<  8) |
                    (i <<  7) |
                    (f <<  6) |
                    (t <<  5) |
                    (m <<  0);
            }
        }
        public class Mode
        {
            public const uint USR = 0x10;
            public const uint FIQ = 0x11;
            public const uint IRQ = 0x12;
            public const uint SVC = 0x13;
            public const uint ABT = 0x17;
            public const uint UND = 0x1b;
            public const uint SYS = 0x1f;

            public Flags spsr;
            public Register[] registers;

            public Mode(int index)
            {
                this.spsr = new Flags();
                this.registers = new Register[index];
            }
        }
        public class Register
        {
            public event Action modified;

            public uint value;
        }
    }
}
