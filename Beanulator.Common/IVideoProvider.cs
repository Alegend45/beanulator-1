using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Beanulator.Common
{
    public interface IVideoProvider
    {
        void Update(int[][] screen);
    }
}
