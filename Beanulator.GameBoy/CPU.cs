using Beanulator.Common.Processors.SHARP;

namespace Beanulator.GameBoy
{
    public class CPU : LR35902
    {
        private byte[] hram = new byte[128]; // $ff80 - $fffe
        private byte[] vram = new byte[8192]; // $8000 - $9fff
        private byte[] oram = new byte[160]; // $fe00 - $fea0
        private byte[] wram = new byte[8192]; // $c000 - $dfff
        private dynamic cart; // cartridge connector

        protected override void transfer()
        {
            ushort address = pins.address;

            if (pins.read)
            {
                pins.data = cart.read(pins.address);

                /**/ if (address >= 0x8000 && address <= 0x9fff) { pins.data = vram[address & 0x1fff]; }
                else if (address >= 0xc000 && address <= 0xfdff) { pins.data = wram[address & 0x1fff]; }
                else if (address >= 0xfe00 && address <= 0xfe9f) { pins.data = oram[address & 0x00ff]; }
                else if (address >= 0xff00 && address <= 0xff7f) { /* i/o */ }
                else if (address >= 0xff80 && address <= 0xfffe) { pins.data = hram[address & 0x007f]; }
                else if (address == 0xffff) { pins.data = interrupts.rf; }
            }
            else
            {
                cart.write(pins.address, pins.data);

                /**/ if (address >= 0x8000 && address <= 0x9fff) { vram[address & 0x1fff] = pins.data; }
                else if (address >= 0xc000 && address <= 0xfdff) { wram[address & 0x1fff] = pins.data; }
                else if (address >= 0xfe00 && address <= 0xfe9f) { oram[address & 0x00ff] = pins.data; }
                else if (address >= 0xff00 && address <= 0xff7f) { /* i/o */ }
                else if (address >= 0xff80 && address <= 0xfffe) { hram[address & 0x007f] = pins.data; }
                else if (address == 0xffff) { interrupts.rf = pins.data; }
            }
        }
    }
}