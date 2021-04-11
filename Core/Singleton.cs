using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    /// <summary>Provides singleton instances of a given type.</summary>
    /// <typeparam name="T">The instance type.</typeparam>
    /// <remarks>Stolen from SMAPI</remarks>
    public static class Singleton<T> where T : new()
    {
        /// <summary>The singleton instance.</summary>
        public static T Instance { get; } = new T();
    }
}