namespace Beanulator.Common.Processors
{
    public abstract class Processor
    {
        public CooperativeThread thread;
        public int cycles;

        public Processor()
        {
            this.thread = new CooperativeThread(main);
        }

        protected virtual void main() { }
        protected virtual void tick(int cycles)
        {
            this.cycles -= cycles;

            if (this.cycles <= 0)
            {
                this.thread.Leave();
            }
        }
    }
}