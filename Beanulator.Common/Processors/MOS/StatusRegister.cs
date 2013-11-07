namespace Beanulator.Common.Processors.MOS
{
    public struct StatusRegister
    {
        public bool N;
        public bool V;
        public bool D;
        public bool I;
        public bool Z;
        public bool C;
        /// <summary>
        /// Get or set the value of status register
        /// </summary>
        public byte VAL
        {
            get
            {
                return (byte)(
                    (N ? 0x80 : 0) |
                    (V ? 0x40 : 0) |
                    (D ? 0x08 : 0) |
                    (I ? 0x04 : 0) |
                    (Z ? 0x02 : 0) |
                    (C ? 0x01 : 0) | 0x20);
            }
            set
            {
                N = (value & 0x80) != 0;
                V = (value & 0x40) != 0;
                D = (value & 0x08) != 0;
                I = (value & 0x04) != 0;
                Z = (value & 0x02) != 0;
                C = (value & 0x01) != 0;
            }
        }
        /// <summary>
        /// Get the value with B flag set
        /// </summary>
        public byte VALB()
        { 
            return (byte)(
                    (N ? 0x80 : 0) |
                    (V ? 0x40 : 0) |
                    (D ? 0x08 : 0) |
                    (I ? 0x04 : 0) |
                    (Z ? 0x02 : 0) |
                    (C ? 0x01 : 0) | 0x30);
        }
    }
}
