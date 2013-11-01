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

        protected abstract void main();
        protected abstract void tick();
    }
}