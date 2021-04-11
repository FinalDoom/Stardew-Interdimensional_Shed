using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    /// <summary>
    /// Implementing types will be called by the InterdimensionalShed core's <c>SaveManager</c> to handle initializing mod data on game launch.
    /// </summary>
    public interface ILaunchHandler
    {
        /// <summary>
        /// Contains logic for preparing an object or collection of objects after launch.
        /// </summary>
        void InitializeAfterLaunch();
    }
}
