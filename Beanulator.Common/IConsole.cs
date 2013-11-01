using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Beanulator.Common
{
    public interface IConsole
    {
        IAudioProvider AudioProvider
        {
            get;
            set;
        }

        IVideoProvider VideoProvider
        {
            get;
            set;
        }

        IInputProvider InputProvider
        {
            get;
            set;
        }
    }
}
