using System;
using System.Runtime.InteropServices;

namespace Beanulator.Common
{
    public delegate void CooperativeThreadProc(object lpParameter);

    public sealed class CooperativeThread
    {
        private static CooperativeThread mainThread;

        internal Action Action;
        internal uint Id;

        public CooperativeThread(Action action)
        {
            this.Action = action;
            this.Id = CreateFiber(0, EntryPoint, action);
        }
        public CooperativeThread(uint id)
        {
            this.Action = null;
            this.Id = id;
        }

        private static void EntryPoint(object lpParameter)
        {
            ((Action)lpParameter)();
        }

        #region P/Invoke

        [DllImport("Kernel32.dll")]
        internal static extern uint CreateFiber(uint dwStackSize, CooperativeThreadProc lpStartAddress, object lpParameter = null);
        [DllImport("Kernel32.dll")]
        internal static extern uint ConvertThreadToFiber(object lpParameter = null);
        [DllImport("Kernel32.dll")]
        internal static extern void DeleteFiber(uint id);
        [DllImport("Kernel32.dll")]
        internal static extern void SwitchToFiber(uint id);

        #endregion

        public static void Abort()
        {
            DeleteFiber(mainThread.Id);
            mainThread = null;
        }
        public static void Start()
        {
            mainThread = new CooperativeThread(ConvertThreadToFiber());
            SwitchToFiber(mainThread.Id);
        }

        public void Enter()
        {
            SwitchToFiber(Id);
        }
        public void Leave()
        {
            SwitchToFiber(mainThread.Id);
        }
    }
}