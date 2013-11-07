namespace Beanulator.Common.Processors.SANYO
{
    /// <summary>
    /// Sanyo LC8670 "Potato", Sega Dreamcast VMU Processor
    /// Hardware Information: http://mc.pp.se/dc/vms/
    /// </summary>
    public class LC8760
    {
        private Flags flags;
        private byte[] ram; // memory, 256  bytes
        private byte[] sfr; // special function registers

        public struct Flags
        {
            public int cy;      // carry (carry from bit 7)
            public int ac;      // auxiliary carry (carry from bit 3)
            public int irbk1;   // indirect register bank 1
            public int irbk0;   // indirect register bank 0
            public int ov;      // arithmetic overflow
            public int rambk0;  // ram bank
            public int p;       // parity
        }
    }
}