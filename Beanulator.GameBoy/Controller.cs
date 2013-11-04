using Beanulator.Common;

namespace Beanulator.GameBoy
{
    public class Controller
    {
        //          COL_1 COL_2     
        //            |     |       
        // COL_1.0 -> o-----o <- COL_2.0
        //            |     |       
        // COL_1.1 -> o-----o <- COL_2.1
        //            |     |       
        // COL_1.2 -> o-----o <- COL_2.2
        //            |     |       
        // COL_1.3 -> o-----o <- COL_2.3

        public IInputProvider input;
        public bool column1State;
        public bool column2State;
        public byte column1;
        public byte column2;

        public byte read()
        {
            int data = 0;

            if (column1State) { data |= column1; }
            if (column2State) { data |= column2; }

            return (byte)(~data);
        }
        public void write(byte data)
        {
            column1State = (data & 0x10) != 0;
            column2State = (data & 0x20) != 0;
        }

        public void update()
        {
            input.Update();

            column1 = 0x10;
            column2 = 0x20;

            if (input.GetButtonState(0)) { column1 |= 1; }
            if (input.GetButtonState(1)) { column1 |= 2; }
            if (input.GetButtonState(2)) { column1 |= 4; }
            if (input.GetButtonState(3)) { column1 |= 8; }
            if (input.GetButtonState(4)) { column2 |= 1; }
            if (input.GetButtonState(5)) { column2 |= 2; }
            if (input.GetButtonState(6)) { column2 |= 4; }
            if (input.GetButtonState(7)) { column2 |= 8; }
        }
    }
}
