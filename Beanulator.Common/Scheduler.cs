using System;
using System.Runtime.InteropServices;

namespace Beanulator.Common
{
    public delegate void SchedulerProc(Action lpParameter);

    public sealed class CooperativeThread
    {
        private static CooperativeThread mainThread;

        internal Action action;
        internal uint id;

        public CooperativeThread(uint id)
        {
            this.action = null;
            this.id = id;
        }
        public CooperativeThread(Action action)
        {
            this.action = action;
            this.id = CreateFiber(0, EntryPoint, action);
        }

        public void Enter()
        {
            SwitchToFiber(id);
        }
        public void Leave()
        {
            SwitchToFiber(mainThread.id);
        }

        private static void EntryPoint(Action action)
        {
            action();
        }

        public static void Abort()
        {
            DeleteFiber(mainThread.id);
            mainThread = null;
        }
        public static void Start()
        {
            mainThread = new CooperativeThread(ConvertThreadToFiber());
            SwitchToFiber(mainThread.id);
        }

        #region P/Invoke

        [DllImport("Kernel32.dll")]
        internal static extern uint CreateFiber(uint dwStackSize, SchedulerProc lpStartAddress, object lpParameter = null);
        [DllImport("Kernel32.dll")]
        internal static extern uint ConvertThreadToFiber(object lpParameter = null);
        [DllImport("Kernel32.dll")]
        internal static extern void DeleteFiber(uint id);
        [DllImport("Kernel32.dll")]
        internal static extern void SwitchToFiber(uint id);

        #endregion
    }
}