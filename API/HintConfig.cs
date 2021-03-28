using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalDoom.StardewValley.InterdimensionalShed.API
{
    /// <summary>
    /// Enum that can be used in configuration to allow users to customize the level of hints they want.
    /// None by default is how the base mod is handled, as it provides a level of mystery. This just determines
    /// general behavior, and does not mean every extension mod must adhere to the configured value or even use it.
    /// See <see cref="DimensionImplementation.HintAllowed(HintConfig)"/> for the default usage.
    /// </summary>
    public enum HintConfig
    {
        /// <summary>
        /// No hints should be given
        /// </summary>
        None,
        /// <summary>
        /// Hints are given randomly based on some deterministic function, changing daily or by some other meter.
        /// </summary>
        Random,
        /// <summary>
        /// Hints are cycled in and out, losing and gaining one hint per day.
        /// </summary>
        Daily,
        /// <summary>
        /// All dimensions should be hinted.
        /// </summary>
        All
    }
}
