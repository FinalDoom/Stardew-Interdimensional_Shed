using StardewValley;
using StardewValley.Buildings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalDoom.StardewValley.InterdimensionalShed.API
{
    /// <summary>
    /// Event arguments for <see cref="IDimensionImplementation.StageChanged"/>
    /// </summary>
    public class DimensionStageChangedArgs
    {
        public int OldStage { get; }
        public int NewStage { get; }
        public DimensionStageChangedArgs(int oldStage, int newStage)
        {
            OldStage = oldStage;
            NewStage = newStage;
        }
    }
    public delegate void StageChangedEventHandler(object sender, DimensionStageChangedArgs e);

    /// <summary>
    /// Defines the dynamic logic for a dimension.
    /// </summary>
    public interface IDimensionImplementation
    {
        /// <summary>
        /// Event that will be fired when the dimension stage is changed. Useful for buildings to self-update.
        /// </summary>
        public event StageChangedEventHandler StageChanged;
        /// <summary>
        /// The current size stage the dimension item enables.
        /// </summary>
        /// <returns>-1 for undiscovered dimensions,
        /// 0 for discovered dimensions without enough items for any other stage,
        /// and a numeric stage otherwise, generally 1-6.</returns>
        int CurrentStage();
        /// <summary>
        /// A description to display for the current dimension, either a hint or a stage-based note.
        /// </summary>
        string CurrentDescription();
        /// <summary>
        /// Returns true if this dimension can be revealed as a hint, before being discovered.
        /// </summary>
        bool HintAllowed();
        /// <summary>
        /// Returns true if the passed item can be consumed to add to this dimension.
        /// </summary>
        bool CanAdd(Item item);
        /// <summary>
        /// Adds the passed item to the dimension's item pool. Passed items are consumed
        /// if they match, up to the maximum defined by the last ulock stage of this dimension.
        /// </summary>
        /// <returns>Any unconsumed item</returns>
        Item Add(Item item);
        /// <summary>The item instance that this dimension is founded on</summary>
        Item Item { get; }

        /// <summary>
        /// Finds the gamelocation associated with this DimensionInfo.
        /// </summary>
        /// <remarks>
        /// This should depend on <c><see cref="Game1.getFarm()"/>.buildings</c> as the save/load process
        /// will operate on that collection and may change the actual <see cref="Building"/> and
        /// <see cref="GameLocation"/> objects. A separately managed collection may not continue to
        /// reference the actual ingame locations.
        /// </remarks>
        GameLocation getDimensionLocation();

        /// <summary>
        /// Adds dimension containing buildings if there were none. Should be called when first Interdimensional Shed is built.
        /// </summary>
        void InitializeDimensionBuilding();
    }
}
