namespace Beanulator.Common.Processors
{
    public abstract class Processor
    {
        public CooperativeThread Thread;
        public int Cycles;

        public Processor()
        {
            this.Thread = new CooperativeThread(Main);
        }

        protected virtual void Main() { }
        protected virtual void Tick(int cycles)
        {
            this.Cycles -= cycles;

            if (this.Cycles <= 0)
            {
                this.Thread.Leave();
            }
        }
    }
}