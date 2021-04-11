using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    /// <summary>
    /// Defines a blueprint and how to add it to the game's <see cref="CarpenterMenu"/>. See the core mod's
    /// <see cref="CarpenterMenuCustomizer"/> for how it's used.
    /// </summary>
    public interface ICustomBluePrintProvider
    {
        /// <summary>
        /// Returns true if this building should be displayed in the Wizard's <see cref="CarpenterMenu"/>,
        /// false otherwise (It should be displayed in Robin's menu).
        /// </summary>
        bool IsMagical { get; }
        /// <summary>
        /// Returns the BluePrint to display for this relevant <see cref="Building"/>
        /// </summary>
        BluePrint BluePrint { get; }
        /// <summary>
        /// This will be called as part of the <see cref="IGameLoopEvents.UpdateTicked"/> event when a custom building has been added
        /// to a <see cref="CarpenterMenu"/>. It should check for the menu still being open and remove itself from
        /// the event if it is not. It should modify any just placed/upgraded building as necessary, then remove itself
        /// from the event.
        /// </summary>
        /// <seealso cref="IModHelper.Events"/>
        /// <seealso cref="IModEvents.GameLoop"/>
        void InterceptBuildAction(object sender, UpdateTickedEventArgs e);
    }
}
