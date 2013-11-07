using System.Runtime.InteropServices;
namespace Beanulator.Common.Processors.MOS
{
    /// <summary>
    /// The MOS Technology 6502
    /// </summary>
    public class R6502
    {
        // Registers
        private StatusRegister P;// Processor status
        private Registers registers;
        private byte M;// Used by read addressing modes
        private byte opcode;
        private Pins pins;
        // Internal
        protected abstract void idle();
        protected abstract void read();
        protected abstract void write();

        public void HardReset()
        {
            //registers
            registers = new Registers();
            registers.a = 0x00;
            registers.x = 0x00;
            registers.y = 0x00;

            registers.spl = 0xFD;
            registers.sph = 0x01;

            //registers.pcl = NesCore.CPUMemory[0xFFFC];
            //registers.pch = NesCore.CPUMemory[0xFFFD];
            P.VAL = 0;
            P.I = true;
            registers.ea = 0;
            //interrupts
            pins = new Pins();
            pins.nmi_current = false;
            pins.irq_flags = 0;
            pins.interrupt_suspend = false;
            //others
            opcode = 0;
        }
        public void SoftReset()
        {
            P.I = true;
            registers.sp -= 3;

            registers.pcl = Read(0xFFFC);
            registers.pch = Read(0xFFFD);
        }
        public void Clock()
        {
            // First clock is to fetch opcode
            opcode = Read(registers.pc);
            registers.pc++;
            // Decode the opcode !!
            // Opcode pattern is aaab bbcc
            // cc bits determine instructions group. We have 4 groups of instructions.
            switch (opcode & 3)
            {
                case 0: DecodeInstructionCollection00(opcode); break;
                case 1: DecodeInstructionCollection01(opcode); break;
                case 2: DecodeInstructionCollection10(opcode); break;
                case 3: DecodeInstructionCollection11(opcode); break;
            }

            // Handle interrupts...
            if (pins.nmi_detected)
            {
                Interrupt();

                pins.nmi_detected = false;// NMI handled !
            }
            else if (pins.irq_detected)
            {
                Interrupt();
            }
        }
        /*This should be called at phi2 of cycle*/
        public void PollInterruptStatus()
        {
            if (!pins.interrupt_suspend)
            {
                // The edge detector, see if nmi occurred. 
                if (pins.nmi_current & !pins.nmi_old) // Raising edge, set nmi request
                    pins.nmi_detected = true;
                pins.nmi_old = pins.nmi_current = false;// NMI detected or not, low both lines for this form ___|-|__
                // irq level detector
                pins.irq_detected = (!P.I && pins.irq_flags != 0);
                // Update interrupt vector !
                pins.interrupt_vector = pins.nmi_detected ? 0xFFFA : 0xFFFE;
            }
        }
        private void PollRDY_Read(int address)
        {
            if (pins.rdy_cycles > 0)
            {
                int _RDY_Cycles = pins.rdy_cycles;
                pins.rdy_cycles = 0;
                if ((address == 0x4016) || (address == 0x4017))
                {
                    // The 2A03 DMC gets fetch by pulling RDY low internally. 
                    // This causes the CPU to pause during the next read cycle, until RDY goes high again.
                    // The DMC unit holds RDY low for 4 cycles.

                    // Consecutive controller port reads from this are treated as one
                    if (_RDY_Cycles-- > 0)
                    { Read(address); }
                    while (--_RDY_Cycles > 0)
                    {
                        // Tick();
                    }
                }
                else
                {
                    // but other addresses see multiple reads as expected
                    while (--_RDY_Cycles > 0)
                    { Read(address); }
                }

                // Pending DMA should occur here ...
                // e.g. DMC DMA
            }
        }
        private void PollRDY_Write()
        {
            if (pins.rdy_cycles > 0) pins.rdy_cycles--;
        }
        public void AssertInterrupt(InterruptType type, bool assert)
        {
            switch (type)
            {
                case InterruptType.NMI: pins.nmi_current = assert; break;
                case InterruptType.APU: if (assert) pins.irq_flags |= 0x1; else pins.irq_flags &= ~0x1; break;
                case InterruptType.DMC: if (assert) pins.irq_flags |= 0x2; else pins.irq_flags &= ~0x2; break;
                case InterruptType.BOARD: if (assert) pins.irq_flags |= 0x4; else pins.irq_flags &= ~0x4; break;
            }
        }
        private void Interrupt()
        {
            Read(registers.pc);
            Read(registers.pc);

            Push(registers.pch);
            Push(registers.pcl);

            Push(P.VAL);
            // the vector is detected during φ2 of previous cycle (before push about 2 ppu cycles)
            int v = pins.interrupt_vector;

            pins.interrupt_suspend = true;
            registers.pcl = Read(v++); P.I = true;
            registers.pch = Read(v);
            pins.interrupt_suspend = false;
        }

        public byte Read(int address)
        {
            PollRDY_Read(address);
            pins.address = (ushort)address;
            read();
            // Tick()
            return pins.data;
        }
        public void Write(int address, byte value)
        {
            PollRDY_Write();
            pins.address = (ushort)address;
            pins.data = value;
            write();
            // Tick()
        }

        private void DecodeInstructionCollection00(byte opcode)
        {
            /*
             * This group is the hardest one. Each instruction should treated differently.
             * Opcode pattern is aaab bb00
             * aaa bits determine the instruction (or flag to use).
             * bbb bits determine the addressing mode.
             * 
             * bbb=5 and aaa < 4 or aaa > 5 is DOP instruction with zpg addressing mode.
             * bbb=7 and aaa < 4 or aaa > 5 is TOP instruction with abs addressing mode.
             * SHY is the only illegal instruction that works here.
             * 
             */
            int aaa = ((opcode >> 5) & 7);
            int bbb = (opcode >> 2) & 7;
            switch (bbb)
            {
                case 0:// Special instructions ...
                    {
                        switch (aaa)
                        {
                            case 0: BRK(); break;
                            case 1: JSR(); break;// addressing mode is done in instruction itself
                            case 2: ImpliedAccumulator(); RTI(); break;
                            case 3: ImpliedAccumulator(); RTS(); break;
                            case 4: Immediate(); break;// ILLEGAL ! set DOP
                            case 5: Immediate(); LDY(); break;
                            case 6: Immediate(); CPY(); break;
                            case 7: Immediate(); CPX(); break;
                        }
                        break;
                    }
                case 1:// Zero page instructions
                    {
                        switch (aaa)
                        {
                            case 0: ZeroPage_R(); break;// ILLEGAL ! set DOP
                            case 1: ZeroPage_R(); BIT(); break;
                            case 2: ZeroPage_R(); break;// ILLEGAL ! set DOP
                            case 3: ZeroPage_R(); break;// ILLEGAL ! set DOP
                            case 4: ZeroPage_W(); STY(); break;
                            case 5: ZeroPage_R(); LDY(); break;
                            case 6: ZeroPage_R(); CPY(); break;
                            case 7: ZeroPage_R(); CPX(); break;
                        }
                        break;
                    }
                case 2: // PHP, PLA ...etc
                    {
                        ImpliedAccumulator();// All implied addressing
                        switch (aaa)
                        {
                            case 0: PHP(); break;
                            case 1: PLP(); break;
                            case 2: PHA(); break;
                            case 3: PLA(); break;
                            case 4: DEY(); break;
                            case 5: TAY(); break;
                            case 6: INY(); break;
                            case 7: INX(); break;
                        }
                        break;
                    }
                case 3:
                    {
                        switch (aaa)
                        {
                            case 0: Absolute_R(); break;// ILLEGAL ! set TOP
                            case 1: Absolute_R(); BIT(); break;
                            case 2: Absolute_W(); registers.pc = registers.ea;/*JMP*/ break;
                            case 3: JMP_I(); break;
                            case 4: Absolute_W(); STY(); break;
                            case 5: Absolute_R(); LDY(); break;
                            case 6: Absolute_R(); CPY(); break;
                            case 7: Absolute_R(); CPX(); break;
                        }
                        break;
                    }
                case 4:// Branches (Relative addressing mode, here, addressing mode is done in instruction itself)
                    {
                        switch (aaa)
                        {
                            case 0: Branch(!P.N); break;
                            case 1: Branch(P.N); break;
                            case 2: Branch(!P.V); break;
                            case 3: Branch(P.V); break;
                            case 4: Branch(!P.C); break;
                            case 5: Branch(P.C); break;
                            case 6: Branch(!P.Z); break;
                            case 7: Branch(P.Z); break;
                        }
                        break;
                    }
                case 5:
                    {
                        switch (aaa)
                        {
                            case 4: ZeroPageX_W(); STY(); break;
                            case 5: ZeroPageX_R(); LDY(); break;
                            default: ZeroPageX_R(); break;// ILLEGAL ! set DOP
                        }
                        break;
                    }
                case 6: // CLC, SEC ...etc
                    {
                        ImpliedAccumulator();// All implied addressing
                        switch (aaa)
                        {
                            case 0: P.C = false; break;
                            case 1: P.C = true; break;
                            case 2: P.I = false; break;
                            case 3: P.I = true; break;
                            case 4: TYA(); break;
                            case 5: P.V = false; break;
                            case 6: P.D = false; break;
                            case 7: P.D = true; break;
                        }
                        break;
                    }
                case 7:
                    {
                        switch (aaa)
                        {
                            case 4: Absolute_W(); SHY(); break;// ILLEGAL ! SHY fits here.
                            case 5: AbsoluteX_R(); LDY(); break;
                            default: AbsoluteX_R(); break;// ILLEGAL ! set TOP
                        }
                        break;
                    }
            }
        }
        private void DecodeInstructionCollection01(byte opcode)
        {
            /*
             * Opcode pattern is aaab bb01
             * aaa bits determine the instruction.
             * bbb bits determine the addressing mode.
             * 
             * We have 9 instructions involved here (1 illegal)
             * 
             */
            int aaa = ((opcode >> 5) & 7);
            int bbb = (opcode >> 2) & 7;
            // Addressing mode:
            if (aaa != 4)// not STA instruction. All addressing modes then are read
            {
                switch (bbb)
                {
                    case 0: IndirectX_R(); break;
                    case 1: ZeroPage_R(); break;
                    case 2: Immediate(); break;
                    case 3: Absolute_R(); break;
                    case 4: IndirectY_R(); break;
                    case 5: ZeroPageX_R(); break;
                    case 6: AbsoluteY_R(); break;
                    case 7: AbsoluteX_R(); break;
                }
                // Do instruction
                switch (aaa)
                {
                    case 0: ORA(); break;
                    case 1: AND(); break;
                    case 2: EOR(); break;
                    case 3: ADC(); break;
                    case 5: LDA(); break;
                    case 6: CMP(); break;
                    case 7: SBC(); break;
                }
            }
            else// STA ! This instruction is an exception for this group.
            {

                switch (bbb)
                {
                    case 0: IndirectX_W(); STA(); break;
                    case 1: ZeroPage_W(); STA(); break;
                    // STA impossible with immediate addressing mode, so set DOP. (or nop, do nothing !)
                    // This is the only illegal instruction in 01 group.
                    // To me, cpu should jam here !
                    case 2: Immediate(); break;
                    case 3: Absolute_W(); STA(); break;
                    case 4: IndirectY_W(); STA(); break;
                    case 5: ZeroPageX_W(); STA(); break;
                    case 6: AbsoluteY_W(); STA(); break;
                    case 7: AbsoluteX_W(); STA(); break;
                }
            }
        }
        private void DecodeInstructionCollection10(byte opcode)
        {
            /*
             * Opcode pattern is aaab bb10
             * aaa bits determine the instruction.
             * bbb bits determine the addressing mode.
             * 
             * SHX is the only illegal instruction that work here without problems.
             * bbb= 000 (0) and aaa < 4 causes cpu to JAM; aaa= 4 or aaa > 5 set DOP (aaa= 5 is LDX).
             * (Looks like instruction ASL, ROL, LSR and ROR are forbidden with this addressing mode.)
             * bbb= 100 (4) is illegal addressing mode. Calling it causes cpu to JAM.
             * bbb= 110 (6) and aaa < 4 or aaa > 5 turn instructions ASL, ROL, LSR, ROR, DEC and INC into NOP.
             * 
             */
            int aaa = ((opcode >> 5) & 7);
            int bbb = (opcode >> 2) & 7;
            // Addressing mode (CHECK FORBIDDEN OPCODES FIRST)
            if (bbb == 4)// This addressing mode is forbidden in this group !
            {
                Console.WriteLine("JAM !", DebugCode.Error); // ILLEGAL ! set JAM.
                return;
            }
            else if (bbb == 0)
            {
                if (aaa < 4)
                {
                    // Instruction ASL, ROL, LSR, ROR are forbidden with this addressing mode.
                    ImpliedAccumulator();
                    Console.WriteLine("JAM !", DebugCode.Error);
                    return;// ILLEGAL ! set JAM.
                }
                else if (aaa > 5)
                {
                    // Instruction DEC and INC turned into DOP.
                    Immediate();
                    return;// ILLEGAL ! set DOP.
                }
            }
            else if (bbb == 6)
            {
                if ((aaa < 4) || (aaa > 5))
                {
                    // Instruction ASL, ROL, LSR, ROR, DEC and INC turned into NOP.
                    ImpliedAccumulator();
                    return;// LEGAL ! set NOP. (is NOP a legal instruction ?)
                }
            }
            // Decode
            switch (aaa)
            {
                case 4:// STX, TXA, TXS
                    {
                        switch (bbb)
                        {
                            case 0: Immediate(); break;// ILLEGAL ! set DOP.
                            case 1: ZeroPage_W(); STX(); break;
                            case 2: ImpliedAccumulator(); TXA(); break;
                            case 3: Absolute_W(); STX(); break;
                            case 5: ZeroPageY_W(); STX(); break;
                            case 6: ImpliedAccumulator(); TXS(); break;
                            case 7: Absolute_W(); SHX(); break;// ILLEGAL ! SHX fits here.
                        }
                        break;
                    }
                case 5:// LDX, TAX, TSX
                    {
                        switch (bbb)
                        {
                            case 0: Immediate(); LDX(); break;
                            case 1: ZeroPage_R(); LDX(); break;
                            case 2: ImpliedAccumulator(); TAX(); break;
                            case 3: Absolute_R(); LDX(); break;
                            case 5: ZeroPageY_R(); LDX(); break;
                            case 6: ImpliedAccumulator(); TSX(); break;
                            case 7: AbsoluteY_R(); LDX(); break;
                        }
                        break;
                    }
                default:
                    {
                        switch (bbb)
                        {
                            case 0: Immediate(); break;
                            case 1: ZeroPage_RW(); break;
                            case 2: ImpliedAccumulator(); break;
                            case 3: Absolute_RW(); break;
                            case 5: ZeroPageX_RW(); break;
                            case 7: AbsoluteX_RW(); break;
                        }
                        // Do instruction
                        switch (aaa)
                        {
                            case 0:
                                {
                                    if (bbb == 2)// Implied or Accumulator
                                        ASL_A();
                                    else
                                        ASL_M();
                                    break;
                                }
                            case 1:
                                {
                                    if (bbb == 2)
                                        ROL_A();
                                    else
                                        ROL_M();
                                    break;
                                }
                            case 2:
                                {
                                    if (bbb == 2)
                                        LSR_A();
                                    else
                                        LSR_M();
                                    break;
                                }
                            case 3:
                                {
                                    if (bbb == 2)
                                        ROR_A();
                                    else
                                        ROR_M();
                                    break;
                                }
                            case 6:
                                {
                                    if (bbb == 2)
                                        DEX();
                                    else
                                        DEC();
                                    break;
                                }
                            case 7:
                                {
                                    if (bbb != 2)
                                        INC();
                                    /* else NOP();*/
                                    break;
                                }
                        }
                        break;
                    }
            }
        }
        private void DecodeInstructionCollection11(byte opcode)
        {
            /* 
             * Illegal opcodes group !
             * Combined of group cc=01 and group cc=10 (11)
             * 
             * Opcode pattern is aaab bb11
             * aaa bits determine the instruction (or flag to use).
             * bbb bits determine the addressing mode.
             * 
             */
            int aaa = ((opcode >> 5) & 7);
            int bbb = (opcode >> 2) & 7;


            switch (bbb)
            {
                case 0: if (aaa == 5 || aaa == 6) IndirectX_R(); else IndirectX_W(); IllegalInstrucitonGroup1(aaa); break;
                case 1: if (aaa == 5 || aaa == 6) ZeroPage_R(); else ZeroPage_W(); IllegalInstrucitonGroup1(aaa); break;
                case 3: if (aaa == 5 || aaa == 6) Absolute_R(); else Absolute_W(); IllegalInstrucitonGroup1(aaa); break;
                case 5:
                    {
                        if (aaa == 4)
                            ZeroPageY_W();
                        else if (aaa == 5)
                            ZeroPageY_R();
                        else if (aaa == 6)
                            ZeroPageX_RW();
                        else
                            ZeroPageX_W();
                        IllegalInstrucitonGroup1(aaa);
                        break;
                    }
                case 2: Immediate(); IllegalInstrucitonGroup2(aaa); break;

                case 4:
                    {
                        if (aaa == 5)
                            IndirectY_R();
                        else if (aaa == 6)
                            IndirectY_RW();
                        else
                            IndirectY_W();
                        IllegalInstrucitonGroup3(aaa); break;
                    }
                case 7:
                    {
                        if (aaa == 4)
                            AbsoluteY_W();
                        else if (aaa == 5)
                            AbsoluteY_R();
                        else if (aaa == 6)
                            AbsoluteX_RW();
                        else
                            AbsoluteX_W();
                        IllegalInstrucitonGroup3(aaa);
                        break;
                    }

                case 6: IllegalInstrucitonGroup4(aaa); break;
            }
        }
        private void IllegalInstrucitonGroup1(int aaa)
        {
            switch (aaa)
            {
                case 0: SLO(); break;
                case 1: RLA(); break;
                case 2: SRE(); break;
                case 3: RRA(); break;
                case 4: SAX(); break;
                case 5: LAX(); break;
                case 6: DCP(); break;
                case 7: ISC(); break;
            }
        }
        private void IllegalInstrucitonGroup2(int aaa)
        {
            switch (aaa)
            {
                case 0: ANC(); break;
                case 1: ANC(); break;
                case 2: ALR(); break;
                case 3: ARR(); break;
                case 4: XAA(); break;
                case 5: LAX(); break;
                case 6: AXS(); break;
                case 7: SBC(); break;
            }
        }
        private void IllegalInstrucitonGroup3(int aaa)
        {
            switch (aaa)
            {
                case 0: SLO(); break;
                case 1: RLA(); break;
                case 2: SRE(); break;
                case 3: RRA(); break;
                case 4: AHX(); break;
                case 5: LAX(); break;
                case 6: DCP(); break;
                case 7: ISC(); break;
            }
        }
        private void IllegalInstrucitonGroup4(int aaa)
        {
            switch (aaa)
            {
                case 0: AbsoluteY_W(); SLO(); break;
                case 1: AbsoluteY_W(); RLA(); break;
                case 2: AbsoluteY_W(); SRE(); break;
                case 3: AbsoluteY_W(); RRA(); break;
                case 4: AbsoluteY_W(); XAS(); break;
                case 5: AbsoluteY_R(); LAR(); break;
                case 6: AbsoluteY_RW(); DCP(); break;
                case 7: AbsoluteY_W(); ISC(); break;
            }
        }

        #region Addressing modes
        /*
         * _R: read access instructions, set the M value. Some addressing modes will execute 1 extra cycle when page crossed.
         * _W: write access instructions, doesn't set the M value. The instruction should handle write at effective address (EF).
         * _RW: Read-Modify-Write instructions, set the M value and the instruction should handle write at effective address (EF).
         */
        private void IndirectX_R()
        {
            byte temp = Read(registers.pc); registers.pc++;// CLock 1
            Read(temp);// Clock 2
            temp += registers.x;

            registers.eal = Read(temp);// Clock 3
            temp++;

            registers.eah = Read(temp);// Clock 4

            M = Read(registers.ea);
        }
        private void IndirectX_W()
        {
            byte temp = Read(registers.pc); registers.pc++;// CLock 1
            Read(temp);// Clock 2
            temp += registers.x;

            registers.eal = Read(temp);// Clock 3
            temp++;

            registers.eah = Read(temp);// Clock 4
        }
        private void IndirectX_RW()
        {
            byte temp = Read(registers.pc); registers.pc++;// CLock 1
            Read(temp);// Clock 2
            temp += registers.x;

            registers.eal = Read(temp);// Clock 3
            temp++;

            registers.eah = Read(temp);// Clock 4

            M = Read(registers.ea);
        }

        private void IndirectY_R()
        {
            byte temp = Read(registers.pc); registers.pc++;// CLock 1
            registers.eal = Read(temp); temp++;// Clock 2
            registers.eah = Read(temp);// Clock 2

            registers.eal += registers.y;

            M = Read(registers.ea);// Clock 3
            if (registers.eal < registers.y)
            {
                registers.eah++;
                M = Read(registers.ea);// Clock 4
            }
        }
        private void IndirectY_W()
        {
            byte temp = Read(registers.pc); registers.pc++;// CLock 1
            registers.eal = Read(temp); temp++;// Clock 2
            registers.eah = Read(temp);// Clock 2

            registers.eal += registers.y;

            M = Read(registers.ea);// Clock 3
            if (registers.eal < registers.y)
                registers.eah++;
        }
        private void IndirectY_RW()
        {
            byte temp = Read(registers.pc); registers.pc++;// CLock 1
            registers.eal = Read(temp); temp++;// Clock 2
            registers.eah = Read(temp);// Clock 2

            registers.eal += registers.y;

            Read(registers.ea);// Clock 3
            if (registers.eal < registers.y)
                registers.eah++;

            M = Read(registers.ea);// Clock 4
        }

        private void ZeroPage_R()
        {
            registers.ea = Read(registers.pc); registers.pc++;// Clock 1
            M = Read(registers.ea);// Clock 2
        }
        private void ZeroPage_W()
        {
            registers.ea = Read(registers.pc); registers.pc++;// Clock 1
        }
        private void ZeroPage_RW()
        {
            registers.ea = Read(registers.pc); registers.pc++;// Clock 1
            M = Read(registers.ea);// Clock 2
        }

        private void ZeroPageX_R()
        {
            registers.ea = Read(registers.pc); registers.pc++;// Clock 1
            Read(registers.ea);// Clock 2
            registers.eal += registers.x;
            M = Read(registers.ea);// Clock 3
        }
        private void ZeroPageX_W()
        {
            registers.ea = Read(registers.pc); registers.pc++;// Clock 1
            Read(registers.ea);// Clock 2
            registers.eal += registers.x;
        }
        private void ZeroPageX_RW()
        {
            registers.ea = Read(registers.pc); registers.pc++;// Clock 1
            Read(registers.ea);// Clock 2
            registers.eal += registers.x;
            M = Read(registers.ea);// Clock 3
        }

        private void ZeroPageY_R()
        {
            registers.ea = Read(registers.pc); registers.pc++;// Clock 1
            Read(registers.ea);// Clock 2
            registers.eal += registers.y;
            M = Read(registers.ea);// Clock 3
        }
        private void ZeroPageY_W()
        {
            registers.ea = Read(registers.pc); registers.pc++;// Clock 1
            Read(registers.ea);// Clock 2
            registers.eal += registers.y;
        }
        private void ZeroPageY_RW()
        {
            registers.ea = Read(registers.pc); registers.pc++;// Clock 1
            Read(registers.ea);// Clock 2
            registers.eal += registers.y;
            M = Read(registers.ea);// Clock 3
        }

        private void Immediate()
        {
            M = Read(registers.pc); registers.pc++;// Clock 1
        }

        private void ImpliedAccumulator()
        {
            byte dummy = Read(registers.pc);
        }

        private void Absolute_R()
        {
            registers.eal = Read(registers.pc); registers.pc++;// Clock 1
            registers.eah = Read(registers.pc); registers.pc++;// Clock 2
            M = Read(registers.ea);// Clock 3
        }
        private void Absolute_W()
        {
            registers.eal = Read(registers.pc); registers.pc++;// Clock 1
            registers.eah = Read(registers.pc); registers.pc++;// Clock 2
        }
        private void Absolute_RW()
        {
            registers.eal = Read(registers.pc); registers.pc++;// Clock 1
            registers.eah = Read(registers.pc); registers.pc++;// Clock 2
            M = Read(registers.ea);// Clock 3
        }

        private void AbsoluteX_R()
        {
            registers.eal = Read(registers.pc); registers.pc++;// Clock 1
            registers.eah = Read(registers.pc); registers.pc++;// Clock 2

            registers.eal += registers.x;

            M = Read(registers.ea);// Clock 3
            if (registers.eal < registers.x)
            {
                registers.eah++;
                M = Read(registers.ea);// Clock 4
            }
        }
        private void AbsoluteX_W()
        {
            registers.eal = Read(registers.pc); registers.pc++;// Clock 1
            registers.eah = Read(registers.pc); registers.pc++;// Clock 2

            registers.eal += registers.x;

            M = Read(registers.ea);// Clock 3
            if (registers.eal < registers.x)
                registers.eah++;
        }
        private void AbsoluteX_RW()
        {
            registers.eal = Read(registers.pc); registers.pc++;// Clock 1
            registers.eah = Read(registers.pc); registers.pc++;// Clock 2

            registers.eal += registers.x;

            Read(registers.ea);// Clock 3
            if (registers.eal < registers.x)
                registers.eah++;

            M = Read(registers.ea);// Clock 4
        }

        private void AbsoluteY_R()
        {
            registers.eal = Read(registers.pc); registers.pc++;// Clock 1
            registers.eah = Read(registers.pc); registers.pc++;// Clock 2

            registers.eal += registers.y;

            M = Read(registers.ea);// Clock 3
            if (registers.eal < registers.y)
            {
                registers.eah++;
                M = Read(registers.ea);// Clock 4
            }
        }
        private void AbsoluteY_W()
        {
            registers.eal = Read(registers.pc); registers.pc++;// Clock 1
            registers.eah = Read(registers.pc); registers.pc++;// Clock 2

            registers.eal += registers.y;

            M = Read(registers.ea);// Clock 3
            if (registers.eal < registers.y)
                registers.eah++;
        }
        private void AbsoluteY_RW()
        {
            registers.eal = Read(registers.pc); registers.pc++;// Clock 1
            registers.eah = Read(registers.pc); registers.pc++;// Clock 2

            registers.eal += registers.y;

            M = Read(registers.ea);// Clock 3
            if (registers.eal < registers.y)
                registers.eah++;

            M = Read(registers.ea);// Clock 4
        }
        #endregion
        #region Instructions
        private void Branch(bool condition)
        {
            byte data = Read(registers.pc); registers.pc++;

            if (condition)
            {
                // Suspend interrupt polling at this cycle ...
                // Fixes the branches delay interrupt !
                // This implements the strange behavior of branches with interrupts, actually this tell us that
                // 6502 never poll interrupts at last cycle of instruction concedering this is the last cycle
                // if no page crossed.
                // This work for all instruction too including BRK
                pins.interrupt_suspend = true;

                Read(registers.pc);
                registers.pcl += data;

                pins.interrupt_suspend = false;
                if (data >= 0x80)
                {
                    if (registers.pcl >= data)
                    {
                        Read(registers.pc);
                        registers.pch--;
                    }
                }
                else
                {
                    if (registers.pcl < data)
                    {
                        Read(registers.pc);
                        registers.pch++;
                    }
                }
            }

        }
        private void Push(byte val)
        {
            Write(registers.sp, val);
            registers.spl--;
        }
        private byte Pull()
        {
            registers.spl++;
            return Read(registers.sp);
        }

        private void ADC()
        {
            int t = (registers.a + M + (P.C ? 1 : 0));

            P.V = ((t ^ registers.a) & (t ^ M) & 0x80) != 0;
            P.N = (t & 0x80) != 0;
            P.Z = (t & 0xFF) == 0;
            P.C = (t >> 0x8) != 0;

            registers.a = (byte)(t & 0xFF);
        }
        private void AHX()
        {
            byte data = (byte)((registers.a & registers.x) & 7);
            Write(registers.ea, data);
        }
        private void ALR()
        {
            registers.a &= M;

            P.C = (registers.a & 0x01) != 0;

            registers.a >>= 1;

            P.N = (registers.a & 0x80) != 0;
            P.Z = registers.a == 0;
        }
        private void ANC()
        {
            registers.a &= M;
            P.N = (registers.a & 0x80) != 0;
            P.Z = registers.a == 0;
            P.C = (registers.a & 0x80) != 0;
        }
        private void AND()
        {
            registers.a &= M;
            P.N = (registers.a & 0x80) == 0x80;
            P.Z = (registers.a == 0);
        }
        private void ARR()
        {
            registers.a = (byte)(((M & registers.a) >> 1) | (P.C ? 0x80 : 0x00));

            P.Z = (registers.a & 0xFF) == 0;
            P.N = (registers.a & 0x80) != 0;
            P.C = (registers.a & 0x40) != 0;
            P.V = ((registers.a << 1 ^ registers.a) & 0x40) != 0;
        }
        private void AXS()
        {
            int temp = (registers.a & registers.x) - M;

            P.N = (temp & 0x80) != 0;
            P.Z = (temp & 0xFF) == 0;
            P.C = (~temp >> 8) != 0;

            registers.x = (byte)(temp);
        }
        private void ASL_M()
        {
            P.C = (M & 0x80) == 0x80;
            Write(registers.ea, M);

            M = (byte)((M << 1) & 0xFE);

            Write(registers.ea, M);

            P.N = (M & 0x80) == 0x80;
            P.Z = (M == 0);
        }
        private void ASL_A()
        {
            P.C = (registers.a & 0x80) == 0x80;

            registers.a = (byte)((registers.a << 1) & 0xFE);

            P.N = (registers.a & 0x80) == 0x80;
            P.Z = (registers.a == 0);
        }
        private void BIT()
        {
            P.N = (M & 0x80) != 0;
            P.V = (M & 0x40) != 0;
            P.Z = (M & registers.a) == 0;
        }
        private void BRK()
        {
            Read(registers.pc);
            registers.pc++;

            Push(registers.pch);
            Push(registers.pcl);

            Push(P.VALB());
            // the vector is detected during φ2 of previous cycle (before push about 2 ppu cycles)
            int v = interrupt_vector;

            pins.interrupt_suspend = true;
            registers.pcl = Read(v++); P.I = true;
            registers.pch = Read(v);
            pins.interrupt_suspend = false;
        }
        private void CMP()
        {
            int t = registers.a - M;
            P.N = (t & 0x80) == 0x80;
            P.C = (registers.a >= M);
            P.Z = (t == 0);
        }
        private void CPX()
        {
            int t = registers.x - M;
            P.N = (t & 0x80) == 0x80;
            P.C = (registers.x >= M);
            P.Z = (t == 0);
        }
        private void CPY()
        {
            int t = registers.y - M;
            P.N = (t & 0x80) == 0x80;
            P.C = (registers.y >= M);
            P.Z = (t == 0);
        }
        private void DCP()
        {
            Write(registers.ea, M);

            M--;
            Write(registers.ea, M);

            int data1 = registers.a - M;

            P.N = (data1 & 0x80) != 0;
            P.Z = data1 == 0;
            P.C = (~data1 >> 8) != 0;
        }
        private void DEC()
        {
            Write(registers.ea, M);
            M--;
            Write(registers.ea, M);
            P.N = (M & 0x80) == 0x80;
            P.Z = (M == 0);
        }
        private void DEY()
        {
            registers.y--;
            P.Z = (registers.y == 0);
            P.N = (registers.y & 0x80) == 0x80;
        }
        private void DEX()
        {
            registers.x--;
            P.Z = (registers.x == 0);
            P.N = (registers.x & 0x80) == 0x80;
        }
        private void EOR()
        {
            registers.a ^= M;
            P.N = (registers.a & 0x80) == 0x80;
            P.Z = (registers.a == 0);
        }
        private void INC()
        {
            Write(registers.ea, M);
            M++;
            Write(registers.ea, M);
            P.N = (M & 0x80) == 0x80;
            P.Z = (M == 0);
        }
        private void INX()
        {
            registers.x++;
            P.Z = (registers.x == 0);
            P.N = (registers.x & 0x80) == 0x80;
        }
        private void INY()
        {
            registers.y++;
            P.N = (registers.y & 0x80) == 0x80;
            P.Z = (registers.y == 0);
        }
        private void ISC()
        {
            byte data = Read(registers.ea);

            Write(registers.ea, data);

            data++;

            Write(registers.ea, data);

            int data1 = data ^ 0xFF;
            int temp = (registers.a + data1 + (P.C ? 1 : 0));

            P.N = (temp & 0x80) != 0;
            P.V = ((temp ^ registers.a) & (temp ^ data1) & 0x80) != 0;
            P.Z = (temp & 0xFF) == 0;
            P.C = (temp >> 0x8) != 0;
            registers.a = (byte)(temp);
        }
        private void JMP_I()
        {
            registers.eal = Read(registers.pc++);
            registers.eah = Read(registers.pc++);

            byte latch = Read(registers.ea);
            registers.eal++; // only increment the low byte, causing the "JMP ($nnnn)" bug
            registers.pch = Read(registers.ea);

            registers.pcl = latch;
        }
        private void JSR()
        {
            /*registers.eal = Peek(registers.pc); registers.pc++;// Clock 1
            registers.eah = Peek(registers.pc); //registers.pc++;

            Push(registers.pch);
            Push(registers.pcl);

            registers.pc = registers.ea;
            Peek(registers.pc);*/
            registers.eal = Read(registers.pc); registers.pc++;
            registers.eah = Read(registers.pc);

            Push(registers.pch);
            Push(registers.pcl);

            registers.eah = Read(registers.pc);
            registers.pc = registers.ea;
        }
        private void LAR()
        {
            registers.spl &= M;
            registers.a = registers.spl;
            registers.x = registers.spl;

            P.N = (registers.spl & 0x80) != 0;
            P.Z = (registers.spl & 0xFF) == 0;
        }
        private void LAX()
        {
            registers.x = registers.a = M;

            P.N = (registers.x & 0x80) != 0;
            P.Z = (registers.x & 0xFF) == 0;
        }
        private void LDA()
        {
            registers.a = M;
            P.N = (registers.a & 0x80) == 0x80;
            P.Z = (registers.a == 0);
        }
        private void LDX()
        {
            registers.x = M;
            P.N = (registers.x & 0x80) == 0x80;
            P.Z = (registers.x == 0);
        }
        private void LDY()
        {
            registers.y = M;
            P.N = (registers.y & 0x80) == 0x80;
            P.Z = (registers.y == 0);
        }
        private void LSR_A()
        {
            P.C = (registers.a & 1) == 1;
            registers.a >>= 1;
            P.Z = (registers.a == 0);
            P.N = (registers.a & 0x80) != 0;
        }
        private void LSR_M()
        {
            P.C = (M & 1) == 1;
            Write(registers.ea, M);
            M >>= 1;

            Write(registers.ea, M);
            P.Z = (M == 0);
            P.N = (M & 0x80) != 0;
        }
        private void ORA()
        {
            registers.a |= M;
            P.N = (registers.a & 0x80) == 0x80;
            P.Z = (registers.a == 0);
        }
        private void PHA()
        {
            Push(registers.a);
        }
        private void PHP()
        {
            Push(P.VALB());
        }
        private void PLA()
        {
            Read(registers.sp);
            registers.a = Pull();
            P.N = (registers.a & 0x80) == 0x80;
            P.Z = (registers.a == 0);
        }
        private void PLP()
        {
            Read(registers.sp);
            P.VAL = Pull();
        }
        private void RLA()
        {
            byte data = Read(registers.ea);

            Write(registers.ea, data);

            byte temp = (byte)((data << 1) | (P.C ? 0x01 : 0x00));

            Write(registers.ea, temp);

            P.N = (temp & 0x80) != 0;
            P.Z = (temp & 0xFF) == 0;
            P.C = (data & 0x80) != 0;

            registers.a &= temp;
            P.N = (registers.a & 0x80) != 0;
            P.Z = (registers.a & 0xFF) == 0;
        }
        private void ROL_A()
        {
            byte temp = (byte)((registers.a << 1) | (P.C ? 0x01 : 0x00));

            P.N = (temp & 0x80) != 0;
            P.Z = (temp & 0xFF) == 0;
            P.C = (registers.a & 0x80) != 0;

            registers.a = temp;
        }
        private void ROL_M()
        {
            Write(registers.ea, M);
            byte temp = (byte)((M << 1) | (P.C ? 0x01 : 0x00));

            Write(registers.ea, temp);
            P.N = (temp & 0x80) != 0;
            P.Z = (temp & 0xFF) == 0;
            P.C = (M & 0x80) != 0;
        }
        private void ROR_A()
        {
            byte temp = (byte)((registers.a >> 1) | (P.C ? 0x80 : 0x00));

            P.N = (temp & 0x80) != 0;
            P.Z = (temp & 0xFF) == 0;
            P.C = (registers.a & 0x01) != 0;

            registers.a = temp;
        }
        private void ROR_M()
        {
            Write(registers.ea, M);

            byte temp = (byte)((M >> 1) | (P.C ? 0x80 : 0x00));
            Write(registers.ea, temp);

            P.N = (temp & 0x80) != 0;
            P.Z = (temp & 0xFF) == 0;
            P.C = (M & 0x01) != 0;
        }
        private void RRA()
        {
            byte data = Read(registers.ea);

            Write(registers.ea, data);

            byte temp = (byte)((data >> 1) | (P.C ? 0x80 : 0x00));

            Write(registers.ea, temp);

            P.N = (temp & 0x80) != 0;
            P.Z = (temp & 0xFF) == 0;
            P.C = (data & 0x01) != 0;

            data = temp;
            int temp1 = (registers.a + data + (P.C ? 1 : 0));

            P.N = (temp1 & 0x80) != 0;
            P.V = ((temp1 ^ registers.a) & (temp1 ^ data) & 0x80) != 0;
            P.Z = (temp1 & 0xFF) == 0;
            P.C = (temp1 >> 0x8) != 0;
            registers.a = (byte)(temp1);
        }
        private void RTI()
        {
            Read(registers.sp);
            P.VAL = Pull();

            registers.pcl = Pull();
            registers.pch = Pull();
        }
        private void RTS()
        {
            Read(registers.sp);
            registers.pcl = Pull();
            registers.pch = Pull();

            registers.pc++;

            Read(registers.pc);
        }
        private void SAX()
        { Write(registers.ea, (byte)(registers.x & registers.a)); }
        private void SBC()
        {
            M ^= 0xFF;
            int temp = (registers.a + M + (P.C ? 1 : 0));

            P.N = (temp & 0x80) != 0;
            P.V = ((temp ^ registers.a) & (temp ^ M) & 0x80) != 0;
            P.Z = (temp & 0xFF) == 0;
            P.C = (temp >> 0x8) != 0;
            registers.a = (byte)(temp);
        }
        private void SHX()
        {
            byte t = (byte)(registers.x & (registers.eah + 1));

            Read(registers.ea);
            registers.eal += registers.y;

            if (registers.eal < registers.y)
                registers.eah = t;

            Write(registers.ea, t);
        }
        private void SHY()
        {
            var t = (byte)(registers.y & (registers.eah + 1));

            Read(registers.ea);
            registers.eal += registers.x;

            if (registers.eal < registers.x)
                registers.eah = t;
            Write(registers.ea, t);
        }
        private void SLO()
        {
            byte data = Read(registers.ea);

            P.C = (data & 0x80) != 0;

            Write(registers.ea, data);

            data <<= 1;

            Write(registers.ea, data);

            P.N = (data & 0x80) != 0;
            P.Z = (data & 0xFF) == 0;

            registers.a |= data;
            P.N = (registers.a & 0x80) != 0;
            P.Z = (registers.a & 0xFF) == 0;
        }
        private void SRE()
        {
            byte data = Read(registers.ea);

            P.C = (data & 0x01) != 0;

            Write(registers.ea, data);

            data >>= 1;

            Write(registers.ea, data);

            P.N = (data & 0x80) != 0;
            P.Z = (data & 0xFF) == 0;

            registers.a ^= data;
            P.N = (registers.a & 0x80) != 0;
            P.Z = (registers.a & 0xFF) == 0;
        }
        private void STA()
        {
            Write(registers.ea, registers.a);
        }
        private void STX()
        {
            Write(registers.ea, registers.x);
        }
        private void STY()
        {
            Write(registers.ea, registers.y);
        }
        private void TAX()
        {
            registers.x = registers.a;
            P.N = (registers.x & 0x80) == 0x80;
            P.Z = (registers.x == 0);
        }
        private void TAY()
        {
            registers.y = registers.a;
            P.N = (registers.y & 0x80) == 0x80;
            P.Z = (registers.y == 0);
        }
        private void TSX()
        {
            registers.x = registers.spl;
            P.N = (registers.x & 0x80) != 0;
            P.Z = registers.x == 0;
        }
        private void TXA()
        {
            registers.a = registers.x;
            P.N = (registers.a & 0x80) == 0x80;
            P.Z = (registers.a == 0);
        }
        private void TXS()
        { registers.spl = registers.x; }
        private void TYA()
        {
            registers.a = registers.y;
            P.N = (registers.a & 0x80) == 0x80;
            P.Z = (registers.a == 0);
        }
        private void XAA()
        {
            registers.a = (byte)(registers.x & M);
            P.N = (registers.a & 0x80) != 0;
            P.Z = (registers.a & 0xFF) == 0;
        }
        private void XAS()
        {
            registers.spl = (byte)(registers.a & registers.x /*& ((dummyVal >> 8) + 1)*/);
            Write(registers.ea, registers.spl);
        }
        #endregion
    }
    public enum InterruptType
    {
        NMI, APU, DMC, BOARD
    }
    public struct Pins
    {
        public bool nmi_current;// Represents the current NMI pin (connected to ppu)
        public bool nmi_old;// Represents the old status if NMI pin, used to generate NMI in raising edge
        public bool nmi_detected;// Determines that NMI is pending (active when NMI pin become true and was false)
        public int irq_flags = 0;// Determines that IRQ flags (pins)
        public bool irq_detected;// Determines that IRQ is pending
        public int interrupt_vector;// This is the interrupt vector to jump in the last 2 cycles of BRK/IRQ/NMI
        // This flag suspend interrupt polling; for testing purpose, not proved yet
        // Tests the behavior that interrupt polling suspends at the last cycle of each instruction.
        // Implemented only in BRK, Branches and interrupts. 
        public bool interrupt_suspend;
        // Others
        public byte rdy_cycles;

        public ushort address;
        public byte bank;
        public byte data;
    }
    /// <summary>
    /// Represents 16-bit register 
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct Registers
    {
        [FieldOffset(0)]
        public byte a;
        [FieldOffset(1)]
        public byte x;
        [FieldOffset(2)]
        public byte y;

        [FieldOffset(4)]
        public byte pcl;
        [FieldOffset(5)]
        public byte pch;

        [FieldOffset(6)]
        public byte eal;
        [FieldOffset(7)]
        public byte eah;

        [FieldOffset(8)]
        public byte spl;
        [FieldOffset(9)]
        public byte sph;

        [FieldOffset(4)]
        public ushort pc;
        [FieldOffset(6)]
        public ushort ea;
        [FieldOffset(8)]
        public ushort sp;
    }
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
