using System.Runtime.InteropServices;

namespace Beanulator.Common.Processors.MOS
{
    /// <summary>
    /// Represents 16-bit register 
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct Register16
    {
        [FieldOffset(0)]
        public byte LOW;
        [FieldOffset(1)]
        public byte Hi;

        [FieldOffset(0)]
        public ushort VAL;
    }
}
